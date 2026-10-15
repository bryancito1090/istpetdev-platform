#!/usr/bin/env python3
"""Generador masivo de consumos para pruebas de estrés y caos (Issue #117)."""

import argparse
import csv
import datetime as dt
import random
import sys
from pathlib import Path

DEFAULT_POINTS = [
    "HOS-N01", "CLI-N02", "OFI-N03", "EDU-N04", "COM-N05",
    "HOT-N06", "OFI-N07", "HOS-S08", "CLI-S09", "EDU-S10",
    "COM-S11", "OFI-S12", "IND-S13", "GER-S14", "HOS-C15",
]

DEFAULT_SKUS = ["DES-01", "PAP-01", "GUA-01"]


def generate_mass_consumos(rows: int, output_path: str | None, seed: int = 42):
    rng = random.Random(seed)
    base_date = dt.date(2026, 10, 1)

    out_file = open(output_path, "w", newline="", encoding="utf-8") if output_path else sys.stdout
    try:
        writer = csv.writer(out_file)
        writer.writerow(["point_code", "sku", "date", "quantity", "period_days"])

        for _ in range(rows):
            point = rng.choice(DEFAULT_POINTS)
            sku = rng.choice(DEFAULT_SKUS)
            day_offset = rng.randint(0, 45)
            record_date = (base_date - dt.timedelta(days=15) + dt.timedelta(days=day_offset)).isoformat()
            quantity = rng.randint(5, 250)
            period_days = 7
            writer.writerow([point, sku, record_date, quantity, period_days])
    finally:
        if output_path and out_file is not sys.stdout:
            out_file.close()


def main():
    parser = argparse.ArgumentParser(description="Genera archivo masivo de consumos válido para demo-region.")
    parser.add_argument("--rows", type=int, default=10000, help="Número de filas a generar (por defecto 10000)")
    parser.add_argument("--out", type=str, default=None, help="Ruta del archivo de salida (por defecto stdout)")
    parser.add_argument("--seed", type=int, default=42, help="Semilla pseudoaleatoria fija para reproducibilidad")
    args = parser.parse_args()

    generate_mass_consumos(args.rows, args.out, args.seed)


if __name__ == "__main__":
    main()
