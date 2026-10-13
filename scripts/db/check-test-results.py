#!/usr/bin/env python3
"""Fail CI if a required test category ran no tests, failed, or skipped cases."""
import sys
from pathlib import Path
from xml.etree import ElementTree

root = Path(sys.argv[1])
namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
for category in ("Unit", "Integration", "E2E"):
    counters = ElementTree.parse(root / f"{category}.trx").find(".//t:Counters", namespace)
    if counters is None or int(counters.get("total", 0)) == 0:
        raise SystemExit(f"{category}: no tests executed")
    total, passed = int(counters.get("total", 0)), int(counters.get("passed", 0))
    if passed != total:
        raise SystemExit(f"{category}: {passed}/{total} tests passed")
    print(f"{category}: {passed}/{total} passed")
