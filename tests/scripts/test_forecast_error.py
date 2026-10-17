#!/usr/bin/env python3
"""Pruebas unitarias para el script de cálculo de error de pronóstico (tests/scripts/forecast_error.py)."""

import importlib.util
import unittest
from datetime import date, timedelta
from decimal import Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("forecast_error", ROOT / "tests/scripts/forecast_error.py")
forecast_error = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(forecast_error)


class ForecastErrorTests(unittest.TestCase):
    def test_prorate_consumption_outside_window(self):
        # Consumo antes del inicio de la ventana
        prorated, overlap = forecast_error.prorate_consumption(
            occurred_on=date(2026, 8, 1),
            period_days=1,
            quantity=Decimal("10.0"),
            window_start=date(2026, 8, 10),
            window_end=date(2026, 8, 20),
        )
        self.assertEqual(prorated, Decimal("0.0"))
        self.assertEqual(overlap, 0)

        # Consumo en o después del fin de ventana (el día t no tiene solapamiento con [window_start, t))
        prorated, overlap = forecast_error.prorate_consumption(
            occurred_on=date(2026, 8, 20),
            period_days=1,
            quantity=Decimal("10.0"),
            window_start=date(2026, 8, 10),
            window_end=date(2026, 8, 20),
        )
        self.assertEqual(prorated, Decimal("0.0"))
        self.assertEqual(overlap, 0)

    def test_prorate_consumption_single_day_inside_window(self):
        prorated, overlap = forecast_error.prorate_consumption(
            occurred_on=date(2026, 8, 15),
            period_days=1,
            quantity=Decimal("12.5"),
            window_start=date(2026, 8, 10),
            window_end=date(2026, 8, 20),
        )
        self.assertEqual(prorated, Decimal("12.500"))
        self.assertEqual(overlap, 1)

    def test_prorate_consumption_multi_day_partial_overlap(self):
        # Consumo que empieza antes de window_start: del 8 al 13 (5 días), ventana del 10 al 20.
        # Solapamiento: 10 al 13 = 3 días. Fracción: 3/5 = 0.600. Cantidad: 10 * 0.6 = 6.000
        prorated, overlap = forecast_error.prorate_consumption(
            occurred_on=date(2026, 8, 8),
            period_days=5,
            quantity=Decimal("10.0"),
            window_start=date(2026, 8, 10),
            window_end=date(2026, 8, 20),
        )
        self.assertEqual(overlap, 3)
        self.assertEqual(prorated, Decimal("6.000"))

        # Consumo que termina después de window_end: del 18 al 23 (5 días), ventana del 10 al 20.
        # Solapamiento: 18 al 20 = 2 días. Fracción: 2/5 = 0.400. Cantidad: 10 * 0.4 = 4.000
        prorated, overlap = forecast_error.prorate_consumption(
            occurred_on=date(2026, 8, 18),
            period_days=5,
            quantity=Decimal("10.0"),
            window_start=date(2026, 8, 10),
            window_end=date(2026, 8, 20),
        )
        self.assertEqual(overlap, 2)
        self.assertEqual(prorated, Decimal("4.000"))

    def test_estimate_daily_demand_replicates_risk_calculator(self):
        records = [
            {"occurred_on": date(2026, 9, 1), "period_days": 1, "quantity": Decimal("10.0")},
            {"occurred_on": date(2026, 9, 2), "period_days": 1, "quantity": Decimal("20.0")},
        ]
        # Evaluando el día 2026-09-03 con ventana de 7 días:
        # ventana [2026-08-27, 2026-09-03). Incluye 9-1 y 9-2.
        # totalProrated = 30.0, totalOverlapDays = 2 (RiskCalculator cuenta overlap de registros existentes).
        # dailyDemand = 30 / 2 = 15.0
        demand = forecast_error.estimate_daily_demand(records, window_days=7, today=date(2026, 9, 3))
        self.assertEqual(demand, Decimal("15.000"))

        # Si hoy es 2026-09-01, los registros del 9-1 y 9-2 son >= today, solapamiento 0 -> None
        demand_early = forecast_error.estimate_daily_demand(records, window_days=7, today=date(2026, 9, 1))
        self.assertIsNone(demand_early)

    def test_compute_metrics_mae_bias_wape(self):
        # y_real = [10, 0, 20], y_pred = [12, 4, 18]
        # errores = [+2, +4, -2]
        # abs_errores = [2, 4, 2] -> sum = 8
        # sum_actual = 30
        # MAE = 8 / 3 = 2.6667
        # Sesgo = (2 + 4 - 2) / 3 = 4 / 3 = 1.3333
        # WAPE = 8 / 30 * 100 = 26.6667%
        errors = [Decimal("2.0"), Decimal("4.0"), Decimal("-2.0")]
        actuals = [Decimal("10.0"), Decimal("0.0"), Decimal("20.0")]
        metrics = forecast_error.compute_metrics(errors, actuals)
        self.assertAlmostEqual(float(metrics["mae"]), 8.0 / 3.0, places=4)
        self.assertAlmostEqual(float(metrics["bias"]), 4.0 / 3.0, places=4)
        self.assertAlmostEqual(float(metrics["wape_pct"]), (8.0 / 30.0) * 100.0, places=4)
        self.assertEqual(metrics["n"], 3)


if __name__ == "__main__":
    unittest.main()
