#!/usr/bin/env python3
"""
Evaluación reproducible del error de pronóstico de la demanda diaria (issue #248).

Replica la lógica de estimación de demanda diaria de RiskCalculator.cs
(src/Application/Logistics/RiskCalculator.cs) sobre consumos.csv
(data/fixtures/demo-quito-planificacion/consumos.csv), comparando el pronóstico
unipaso contra la demanda real observada para ventanas de 7, 14 y 28 días.

Lógica de réplica de RiskCalculator.cs:
1. Ventana: Para estimar la demanda en el día t, se consideran únicamente los consumos
   en el intervalo [t - window_days, t) (windowStart = today - window_days,
   windowEnd = today). El día t tiene solapamiento 0 con la ventana (sin anticipación).
2. period_days: Cada registro de consumo con period_days se prorratea según la fracción
   de días de solapamiento: overlap_days / period_days. La cantidad prorrateada se redondea
   a 3 decimales (MidpointRounding.ToEven), igual que ProrateConsumption en C#.
3. Días sin consumo: RiskCalculator no interpola ni inserta ceros artificiales para días
   de calendario sin registro; totalOverlapDays es la suma de los solapamientos de los
   registros de consumo existentes. Por ello, en puntos con actividad parcial (p. ej.
   lunes a viernes), la media se calcula sobre los días con consumo reportado.
4. Demanda cero: Si totalOverlapDays == 0 o totalProrated == 0, RiskCalculator retorna null.
   Al evaluar el pronóstico para el día t frente a la demanda real, un valor null se trata
   como 0.0 (demanda esperada nula por ausencia de observaciones previas).

Métricas calculadas por punto×producto y globales:
- MAE: Mean Absolute Error (1/N * sum(|y_pred - y_real|))
- Sesgo: Mean Signed Error (1/N * sum(y_pred - y_real)); positivo = sobreestimación.
- WAPE: Weighted Absolute Percentage Error (sum(|y_pred - y_real|) / sum(y_real)).
  Se usa WAPE en lugar de MAPE debido a la existencia de días con demanda cero (fines
  de semana en oficinas, bancos, etc.), donde MAPE generaría división por cero.

Entorno del dataset sintético:
- Semilla del generador: 20261016 (scripts/data/generate-demo-quito-planificacion.py).
- Período total del fixture: 2026-08-17 a 2026-10-15 (60 días).
- Corte de evaluación por defecto: 2026-09-14 a 2026-10-15 (32 días de prueba, tras
  28 días de calentamiento para permitir historial completo a las tres ventanas).
"""

import argparse
import csv
import json
import sys
from collections import defaultdict
from datetime import date, timedelta
from decimal import Decimal, ROUND_HALF_EVEN
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_FIXTURE_PATH = ROOT / "data/fixtures/demo-quito-planificacion/consumos.csv"
DATASET_SEED = 20261016
DEFAULT_WARMUP_DAYS = 28


def quantize_3(value: Decimal) -> Decimal:
    """Redondea un valor Decimal a 3 decimales usando banquero (ROUND_HALF_EVEN)."""
    return value.quantize(Decimal("0.001"), rounding=ROUND_HALF_EVEN)


def prorate_consumption(
    occurred_on: date,
    period_days: int,
    quantity: Decimal,
    window_start: date,
    window_end: date,
) -> Tuple[Decimal, int]:
    """
    Replica exacta de RiskCalculator.ProrateConsumption (C#):
    Calcula la cantidad prorrateada y los días de solapamiento entre el período
    de consumo [occurred_on, occurred_on + period_days) y la ventana [window_start, window_end).
    """
    if period_days <= 0:
        return Decimal("0.0"), 0

    period_end = occurred_on + timedelta(days=period_days)
    overlap_start = max(occurred_on, window_start)
    overlap_end = min(period_end, window_end)

    if overlap_end <= overlap_start:
        return Decimal("0.0"), 0

    overlap_days = (overlap_end - overlap_start).days
    if overlap_days <= 0:
        return Decimal("0.0"), 0

    fraction = Decimal(overlap_days) / Decimal(period_days)
    prorated = quantize_3(quantity * fraction)
    return prorated, overlap_days


def estimate_daily_demand(
    records: List[Dict[str, Any]],
    window_days: int,
    today: date,
) -> Optional[Decimal]:
    """
    Replica exacta de la estimación de demanda diaria de RiskCalculator.CalculateAsync:
    Filtra consumos en [today - window_days, today), suma cantidades prorrateadas
    y solapamientos, y calcula totalProrated / totalOverlapDays redondeado a 3 decimales.
    Si totalOverlapDays == 0 o totalProrated <= 0, retorna None.
    """
    window_start = today - timedelta(days=window_days)
    total_prorated = Decimal("0.0")
    total_overlap_days = 0

    for c in records:
        prorated, overlap = prorate_consumption(
            occurred_on=c["occurred_on"],
            period_days=c["period_days"],
            quantity=c["quantity"],
            window_start=window_start,
            window_end=today,
        )
        total_prorated += prorated
        total_overlap_days += overlap

    if total_overlap_days > 0 and total_prorated > Decimal("0.0"):
        return quantize_3(total_prorated / Decimal(total_overlap_days))

    return None


def load_consumptions(file_path: Path) -> Tuple[Dict[Tuple[str, str], List[Dict[str, Any]]], date, date]:
    """Carga consumos.csv agrupados por (point_code, sku) y retorna fechas mín/máx."""
    series: Dict[Tuple[str, str], List[Dict[str, Any]]] = defaultdict(list)
    min_date: Optional[date] = None
    max_date: Optional[date] = None

    with open(file_path, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            d = date.fromisoformat(row["date"])
            rec = {
                "occurred_on": d,
                "period_days": int(row["period_days"]),
                "quantity": Decimal(row["quantity"]),
            }
            series[(row["point_code"], row["sku"])].append(rec)
            if min_date is None or d < min_date:
                min_date = d
            if max_date is None or d > max_date:
                max_date = d

    if min_date is None or max_date is None:
        raise ValueError(f"El archivo {file_path} no contiene registros válidos.")

    return series, min_date, max_date


def compute_metrics(errors: List[Decimal], actuals: List[Decimal]) -> Dict[str, Decimal]:
    """Calcula MAE, Sesgo (error medio con signo) y WAPE."""
    n = len(errors)
    if n == 0:
        return {
            "mae": Decimal("0.0"),
            "bias": Decimal("0.0"),
            "wape_pct": Decimal("0.0"),
            "sum_actual": Decimal("0.0"),
            "n": 0,
        }

    sum_abs_err = sum(abs(e) for e in errors)
    sum_err = sum(errors)
    sum_act = sum(actuals)

    mae = sum_abs_err / Decimal(n)
    bias = sum_err / Decimal(n)
    wape_pct = (sum_abs_err / sum_act * Decimal("100")) if sum_act > Decimal("0.0") else Decimal("0.0")

    return {
        "mae": mae,
        "bias": bias,
        "wape_pct": wape_pct,
        "sum_actual": sum_act,
        "n": n,
    }


def evaluate_forecasts(
    series: Dict[Tuple[str, str], List[Dict[str, Any]]],
    windows: List[int],
    eval_start: date,
    eval_end: date,
) -> Dict[int, Dict[str, Any]]:
    """
    Evalúa el error de pronóstico unipaso para cada día t en [eval_start, eval_end]
    comparando las ventanas especificadas.
    """
    eval_dates = [eval_start + timedelta(days=i) for i in range((eval_end - eval_start).days + 1)]
    results: Dict[int, Dict[str, Any]] = {}

    for w in windows:
        all_errors: List[Decimal] = []
        all_actuals: List[Decimal] = []
        series_results: Dict[Tuple[str, str], Dict[str, Decimal]] = {}

        for (point_code, sku), records in sorted(series.items()):
            actual_by_date = {c["occurred_on"]: c["quantity"] for c in records}
            s_errors: List[Decimal] = []
            s_actuals: List[Decimal] = []

            for t in eval_dates:
                actual = actual_by_date.get(t, Decimal("0.0"))
                pred_opt = estimate_daily_demand(records, w, t)
                # Si RiskCalculator devuelve None (sin observaciones previas), se pronostica 0.0
                pred = pred_opt if pred_opt is not None else Decimal("0.0")

                err = pred - actual
                s_errors.append(err)
                s_actuals.append(actual)
                all_errors.append(err)
                all_actuals.append(actual)

            series_results[(point_code, sku)] = compute_metrics(s_errors, s_actuals)

        total_metrics = compute_metrics(all_errors, all_actuals)
        results[w] = {
            "total": total_metrics,
            "by_series": series_results,
            "eval_days": len(eval_dates),
            "series_count": len(series),
        }

    return results


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Medición de error de pronóstico de demanda (MAE, Sesgo, WAPE) para RiskCalculator."
    )
    parser.add_argument(
        "--input",
        type=Path,
        default=DEFAULT_FIXTURE_PATH,
        help="Ruta a consumos.csv (por defecto fixture demo-quito-planificacion)",
    )
    parser.add_argument(
        "--windows",
        type=str,
        default="7,14,28",
        help="Lista de ventanas en días separadas por coma (por defecto: 7,14,28)",
    )
    parser.add_argument(
        "--start-date",
        type=str,
        default=None,
        help="Fecha inicial de evaluación (YYYY-MM-DD). Por defecto: min_date + 28 días (2026-09-14)",
    )
    parser.add_argument(
        "--end-date",
        type=str,
        default=None,
        help="Fecha final de evaluación (YYYY-MM-DD). Por defecto: última fecha del dataset (2026-10-15)",
    )
    parser.add_argument(
        "--export-csv",
        type=Path,
        default=None,
        help="Ruta para exportar métricas detalladas por punto×producto en CSV",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="Imprimir resultados globales en formato JSON",
    )

    args = parser.parse_args()

    if not args.input.is_file():
        print(f"Error: No se encontró el archivo de consumos en {args.input}", file=sys.stderr)
        return 1

    series, min_d, max_d = load_consumptions(args.input)
    windows = [int(w.strip()) for w in args.windows.split(",") if w.strip()]

    eval_start = (
        date.fromisoformat(args.start_date)
        if args.start_date
        else min_d + timedelta(days=DEFAULT_WARMUP_DAYS)
    )
    eval_end = date.fromisoformat(args.end_date) if args.end_date else max_d

    if eval_start > eval_end:
        print(f"Error: start-date ({eval_start}) no puede ser posterior a end-date ({eval_end}).", file=sys.stderr)
        return 1

    results = evaluate_forecasts(series, windows, eval_start, eval_end)

    if args.json:
        payload = {
            "dataset_seed": DATASET_SEED,
            "eval_range": {"start": eval_start.isoformat(), "end": eval_end.isoformat()},
            "series_count": len(series),
            "results": {
                f"window_{w}d": {
                    "mae": float(res["total"]["mae"]),
                    "bias": float(res["total"]["bias"]),
                    "wape_pct": float(res["total"]["wape_pct"]),
                    "observations": res["total"]["n"],
                }
                for w, res in results.items()
            },
        }
        print(json.dumps(payload, indent=2))
        return 0

    print("=" * 76)
    print("REPORTE DE ERROR DE PRONÓSTICO — MODELO DE REFERENCIA (MEDIA MÓVIL)")
    print("=" * 76)
    print(f"Fuente de datos    : {args.input}")
    print(f"Semilla del dataset: {DATASET_SEED} (sintético, generate-demo-quito-planificacion.py)")
    print(f"Historial completo : {min_d} al {max_d} ({(max_d - min_d).days + 1} días)")
    print(f"Corte de evaluación: {eval_start} al {eval_end} ({(eval_end - eval_start).days + 1} días)")
    print(f"Pares punto × SKU  : {len(series)}")
    print(f"Total observaciones: {len(series) * ((eval_end - eval_start).days + 1)} evaluaciones unipaso")
    print("-" * 76)
    print(f"{'Ventana':<10} | {'MAE':<10} | {'Sesgo (Mean Error)':<20} | {'WAPE (%)':<12} | {'Observaciones':<12}")
    print("-" * 76)

    best_w = None
    min_mae = None
    for w in windows:
        tot = results[w]["total"]
        mae = tot["mae"]
        bias = tot["bias"]
        wape = tot["wape_pct"]
        n = tot["n"]
        if min_mae is None or mae < min_mae:
            min_mae = mae
            best_w = w
        print(f"{w:>2} días    | {mae:<10.4f} | {bias:>+10.4f}          | {wape:>9.2f} %  | {n:<12}")

    print("-" * 76)
    print(f"Menor error global en este escenario: Ventana de {best_w} días (MAE {min_mae:.4f})")
    print("Nota: El sesgo positivo refleja la demanda cero en fines de semana no modelada.")
    print("=" * 76)

    if args.export_csv:
        with open(args.export_csv, "w", encoding="utf-8", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(["point_code", "sku", "window_days", "mae", "bias", "wape_pct", "observations", "sum_actual"])
            for w in windows:
                for (point_code, sku), met in sorted(results[w]["by_series"].items()):
                    writer.writerow([
                        point_code,
                        sku,
                        w,
                        f"{met['mae']:.4f}",
                        f"{met['bias']:.4f}",
                        f"{met['wape_pct']:.2f}",
                        met["n"],
                        f"{met['sum_actual']:.3f}",
                    ])
        print(f"Detalle por serie exportado en: {args.export_csv}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
