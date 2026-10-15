#!/usr/bin/env python3
"""
Generador determinista de dataset de consumos masivo para medición de 001 NFR-01 (< 30 s).
Genera N filas CSV válidas distribuidas en puntos y productos del catálogo ISTPET.
Garantiza unicidad de clave natural (point_code, sku, date) para inserción limpia.
"""

import argparse
import csv
import datetime
import random
import sys
from pathlib import Path

DEFAULT_POINTS = [
    "HOS-N01", "CLI-N02", "OFI-N03", "EDU-N04", "COM-N05",
    "HOT-N06", "OFI-N07", "HOS-S08", "CLI-S09", "EDU-S10",
    "COM-S11", "OFI-S12", "IND-S13", "GER-S14", "HOS-C15"
]

DEFAULT_SKUS = ["DES-01", "PAP-01", "GUA-01"]

def generate_consumptions(count: int, output_path: Path, seed: int = 42, base_date_str: str = "2026-10-15") -> None:
    random.seed(seed)
    base_date = datetime.date.fromisoformat(base_date_str)
    
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # Para respetar el índice único de BD (ServicePointId, ProductId, OccurredOn),
    # generamos combinaciones únicas distribuidas hacia el pasado.
    points = DEFAULT_POINTS
    skus = DEFAULT_SKUS
    combos_per_day = len(points) * len(skus) # 15 * 3 = 45 combos únicos por día
    
    days_needed = (count // combos_per_day) + 2
    
    rows_written = 0
    with open(output_path, mode="w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["point_code", "sku", "date", "quantity", "period_days"])
        
        day_offset = 1
        while rows_written < count:
            current_date = base_date - datetime.timedelta(days=day_offset)
            # Mezclamos los puntos y skus para distribución realista
            day_combos = [(p, s) for p in points for s in skus]
            random.shuffle(day_combos)
            
            for point, sku in day_combos:
                if rows_written >= count:
                    break
                qty = random.randint(10, 150)
                period = random.choice([1, 7])
                writer.writerow([point, sku, current_date.isoformat(), qty, period])
                rows_written += 1
                
            day_offset += 1

    print(f"Generadas {rows_written} filas de consumo en '{output_path}'")

def main():
    parser = argparse.ArgumentParser(description="Generador de consumos masivos para benchmark NFR-01")
    parser.add_argument("--rows", type=int, default=10000, help="Número de filas a generar (default: 10000)")
    parser.add_argument("--output", type=str, default="data/fixtures/demo-region-caos/consumos_10k.csv", help="Ruta de salida del archivo CSV")
    parser.add_argument("--seed", type=int, default=42, help="Semilla determinista (default: 42)")
    parser.add_argument("--base-date", type=str, default="2026-10-15", help="Fecha base ISO (default: 2026-10-15)")
    
    args = parser.parse_args()
    output_file = Path(args.output)
    generate_consumptions(args.rows, output_file, args.seed, args.base_date)

if __name__ == "__main__":
    main()
