#!/usr/bin/env python3
"""
Generador canónico de tramos viales para demo-region.
Lee bodegas.csv y puntos.csv, calcula matriz dirigida (240 pares)
y genera tramos.csv.
Usa solo la biblioteca estándar de Python.
"""
import csv
import json
import math
import os
import sys
import time
import urllib.request
import urllib.error

EARTH_RADIUS_KM = 6371.0

def haversine(lat1, lon1, lat2, lon2):
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lon2 - lon1)
    a = math.sin(delta_phi / 2.0)**2 + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda / 2.0)**2
    c = 2.0 * math.atan2(math.sqrt(a), math.sqrt(1.0 - a))
    return EARTH_RADIUS_KM * c

def encode_polyline(points):
    result = []
    prev_lat = 0
    prev_lng = 0
    for lat, lng in points:
        late5 = int(round(lat * 1e5))
        lnge5 = int(round(lng * 1e5))
        dlat = late5 - prev_lat
        dlng = lnge5 - prev_lng
        prev_lat = late5
        prev_lng = lnge5
        for val in (dlat, dlng):
            val = ~(val << 1) if val < 0 else (val << 1)
            while val >= 0x20:
                result.append(chr((0x20 | (val & 0x1f)) + 63))
                val >>= 5
            result.append(chr(val + 63))
    return ''.join(result)

def main():
    fixture_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "data", "fixtures", "demo-region"))
    bodegas_csv = os.path.join(fixture_dir, "bodegas.csv")
    puntos_csv = os.path.join(fixture_dir, "puntos.csv")
    tramos_csv = os.path.join(fixture_dir, "tramos.csv")

    nodes = []
    with open(bodegas_csv, mode="r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for r in reader:
            nodes.append({
                "code": r["code"],
                "kind": "warehouse",
                "lat": float(r["lat"]),
                "lon": float(r["lon"])
            })

    with open(puntos_csv, mode="r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for r in reader:
            nodes.append({
                "code": r["code"],
                "kind": "service_point",
                "lat": float(r["lat"]),
                "lon": float(r["lon"])
            })

    print(f"Loaded {len(nodes)} nodes from fixture.")

    base_url = "https://router.project-osrm.org"
    if "--base-url" in sys.argv:
        idx = sys.argv.index("--base-url")
        if idx + 1 < len(sys.argv):
            base_url = sys.argv[idx + 1]

    use_osrm = False
    # Test OSRM connectivity
    try:
        test_url = f"{base_url}/route/v1/driving/-78.5400,-0.2850;-78.4880,-0.1180?overview=simplified"
        req = urllib.request.Request(test_url, headers={"User-Agent": "IstpetDev-BuildRoadSegments/1.0"})
        with urllib.request.urlopen(req, timeout=3) as resp:
            if resp.status == 200:
                use_osrm = True
                print("OSRM connectivity verified; using live routing.")
    except Exception as e:
        print(f"OSRM not reachable or disabled ({e}); using deterministic Haversine fallback (x1.3, 40 km/h).")

    rows = []
    pair_count = 0
    total_pairs = len(nodes) * (len(nodes) - 1)

    for src in nodes:
        for dst in nodes:
            if src["code"] == dst["code"]:
                continue
            pair_count += 1
            dist_km = 0.0
            dur_min = 0.0
            geom = ""

            if use_osrm:
                try:
                    time.sleep(1.0) # rate limit <= 1 req/s
                    url = f"{base_url}/route/v1/driving/{src['lon']},{src['lat']};{dst['lon']},{dst['lat']}?overview=simplified"
                    req = urllib.request.Request(url, headers={"User-Agent": "IstpetDev-BuildRoadSegments/1.0"})
                    with urllib.request.urlopen(req, timeout=5) as resp:
                        data = json.loads(resp.read().decode("utf-8"))
                        if data.get("code") == "Ok" and data.get("routes"):
                            route = data["routes"][0]
                            dist_km = round(route["distance"] / 1000.0, 3)
                            dur_min = round(route["duration"] / 60.0, 3)
                            geom = route.get("geometry", "")
                except Exception as ex:
                    print(f"Warning: error querying OSRM for {src['code']} -> {dst['code']}: {ex}")

            if dist_km <= 0.0:
                h_dist = haversine(src["lat"], src["lon"], dst["lat"], dst["lon"])
                dist_km = round(h_dist * 1.3, 3)
                dur_min = round((dist_km / 40.0) * 60.0, 3)
                geom = encode_polyline([(src["lat"], src["lon"]), (dst["lat"], dst["lon"])])

            rows.append({
                "from_code": src["code"],
                "to_code": dst["code"],
                "distance_km": f"{dist_km:.3f}",
                "duration_min": f"{dur_min:.3f}",
                "geometry": geom
            })

    with open(tramos_csv, mode="w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=["from_code", "to_code", "distance_km", "duration_min", "geometry"])
        writer.writeheader()
        writer.writerows(rows)

    print(f"Generated {len(rows)} segments in {tramos_csv}.")

if __name__ == "__main__":
    main()
