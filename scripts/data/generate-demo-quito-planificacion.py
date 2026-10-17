#!/usr/bin/env python3
"""
Generador del fixture data/fixtures/demo-quito-planificacion (issue #243).

Arma un escenario de Quito con ubicaciones públicas reales (OpenStreetMap),
vehículos de modelos que se venden en Ecuador, catálogo de limpieza con pesos
estimados, 60 días de consumo e incidentes para probar desde un día normal
hasta una avería a mitad de ruta.

Las relaciones comerciales, cantidades, matrículas, kilometrajes, precios y
costos son SINTÉTICOS. Los incidentes y las franjas se basan en hechos publicados
(AMT, Secretaría de Movilidad, prensa), citados en data/fixtures/demo-quito-planificacion/README.md.

Uso:  python scripts/data/generate-demo-quito-planificacion.py [--sin-osrm]
Solo usa la biblioteca estándar. Con OSRM público, distancias y tiempos son por
calles reales; con --sin-osrm (o si OSRM falla) se usa haversine × 1,35 y el
manifiesto lo declara.
"""
import csv
import hashlib
import json
import math
import os
import random
import sys
import time
import urllib.request
from datetime import date, timedelta

SEED = 20261016
END_DATE = date(2026, 10, 15)      # último día con consumo registrado
HISTORY_DAYS = 60
OSRM = "https://router.project-osrm.org"
UA = {"User-Agent": "istpetdev-dataset/1.0 (hackathon IstpetDev)"}
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "data", "fixtures", "demo-quito-planificacion")

# ---------------------------------------------------------------------------
# Bodegas: ubicaciones plausibles en zonas con actividad logística (sintéticas).
WAREHOUSES = [
    ("BOD-0001", "Bodega Central Sur - Turubamba (sintética)", -0.31888, -78.54932),
    ("BOD-0002", "Bodega Norte - Carcelén (sintética)", -0.09350, -78.47400),
    ("BOD-0003", "Bodega Valle - Conocoto (sintética)", -0.29203, -78.47780),
]

# Puntos de servicio.
# fuente = "osm" -> establecimiento público real, coordenada de OpenStreetMap (ODbL);
#          la relación de abastecimiento con IstpetDev es ficticia.
# fuente = "sintetico" -> negocio inventado, coordenada de un barrio de Quito.
# (código, nombre, lat, lon, categoría, criticidad, ventana, servicio_min, lead_time, zona, fuente, referencia)
POINTS = [
    ("PTO-0001", "Hospital de Especialidades Eugenio Espejo", -0.21531, -78.49848, "hospital", "alta", ("07:00", "11:00"), 30, 1, "Centro", "osm", "w1429059854"),
    ("PTO-0002", "Hospital Gineco-Obstétrico Isidro Ayora", -0.21501, -78.49997, "hospital", "alta", ("07:00", "11:00"), 25, 1, "Centro", "osm", "w717440311"),
    ("PTO-0003", "Hospital Pediátrico Baca Ortiz", -0.20308, -78.48633, "hospital", "alta", ("07:00", "11:00"), 25, 1, "Centro-Norte", "osm", "w31135580"),
    ("PTO-0004", "Hospital Carlos Andrade Marín (IESS)", -0.20513, -78.50483, "hospital", "alta", ("06:30", "10:30"), 35, 1, "Centro", "osm", "w1101691224"),
    ("PTO-0005", "Hospital General Enrique Garcés", -0.24444, -78.54119, "hospital", "alta", ("07:00", "11:00"), 30, 1, "Sur", "osm", "w31134989"),
    ("PTO-0006", "Hospital General IESS Quito Sur", -0.25742, -78.52524, "hospital", "alta", ("07:00", "11:00"), 30, 1, "Sur", "osm", "w107938379"),
    ("PTO-0007", "Hospital General Docente de Calderón", -0.09251, -78.43681, "hospital", "alta", ("07:00", "11:00"), 30, 2, "Norte", "osm", "w242102876"),
    ("PTO-0008", "Hospital Pablo Arturo Suárez", -0.12781, -78.49734, "hospital", "alta", ("07:00", "11:00"), 25, 1, "Norte", "osm", "w31135949"),
    ("PTO-0009", "Hospital IESS San Francisco de Quito", -0.08966, -78.47678, "hospital", "alta", ("07:00", "11:00"), 25, 1, "Norte", "osm", "w1357560803"),
    ("PTO-0010", "Hospital Quito N.º 1 de la Policía", -0.18292, -78.50487, "hospital", "alta", ("07:00", "11:00"), 25, 1, "Centro-Norte", "osm", "w31152385"),
    ("PTO-0011", "Centro de Salud Guamaní", -0.32421, -78.54938, "centro_salud", "media", ("08:00", "15:00"), 15, 2, "Sur", "osm", "n992302533"),
    ("PTO-0012", "Centro de Salud Tumbaco", -0.21288, -78.40514, "centro_salud", "media", ("08:00", "15:00"), 15, 2, "Valle Tumbaco", "osm", "w425163215"),
    # --- Sintéticos ---
    ("PTO-0013", "Centro comercial Quicentro Norte (sintético)", -0.17800, -78.48500, "centro_comercial", "media", ("07:00", "09:30"), 20, 2, "Centro-Norte", "sintetico", "Iñaquito"),
    ("PTO-0014", "Oficinas corporativas La Carolina (sintéticas)", -0.19037, -78.48550, "oficina", "normal", ("08:00", "17:00"), 10, 3, "Centro-Norte", "sintetico", "La Carolina"),
    ("PTO-0015", "Restaurante La Mariscal (sintético)", -0.20330, -78.49300, "local_comida", "media", ("06:30", "10:30"), 15, 2, "Centro", "sintetico", "La Mariscal"),
    ("PTO-0016", "Patio de comidas El Batán (sintético)", -0.18000, -78.47200, "local_comida", "media", ("06:30", "10:30"), 15, 2, "Centro-Norte", "sintetico", "El Batán"),
    ("PTO-0017", "Sucursal bancaria El Inca (sintética)", -0.14989, -78.47641, "banco", "normal", ("08:00", "09:00"), 10, 3, "Norte", "sintetico", "El Inca"),
    ("PTO-0018", "Clínica privada Kennedy (sintética)", -0.14095, -78.48136, "clinica", "media", ("07:00", "12:00"), 20, 2, "Norte", "sintetico", "Kennedy"),
    ("PTO-0019", "Unidad educativa Ponceano (sintética)", -0.10856, -78.48774, "colegio", "normal", ("13:30", "16:00"), 15, 3, "Norte", "sintetico", "Ponceano"),
    ("PTO-0020", "Supermercado Cotocollao (sintético)", -0.11596, -78.50011, "comercio", "normal", ("06:00", "09:00"), 20, 2, "Norte", "sintetico", "Cotocollao"),
    ("PTO-0021", "Oficinas El Condado (sintéticas)", -0.10386, -78.52749, "oficina", "normal", ("08:00", "17:00"), 10, 3, "Norte", "sintetico", "El Condado"),
    ("PTO-0022", "Planta de alimentos Carcelén (sintética)", -0.09000, -78.47300, "industria", "media", ("06:00", "14:00"), 25, 2, "Norte", "sintetico", "Carcelén"),
    ("PTO-0023", "Local de comida Pomasqui (sintético)", -0.04779, -78.46081, "local_comida", "normal", ("07:00", "11:00"), 10, 3, "Norte extraurbano", "sintetico", "Pomasqui"),
    ("PTO-0024", "Clínica privada Calderón (sintética)", -0.09600, -78.42200, "clinica", "media", ("07:00", "12:00"), 20, 2, "Norte", "sintetico", "Calderón"),
    ("PTO-0025", "Hotel La Floresta (sintético)", -0.20934, -78.48492, "hotel", "media", ("09:00", "12:00"), 15, 2, "Centro", "sintetico", "La Floresta"),
    ("PTO-0026", "Oficinas Centro Histórico - Plaza Grande (sintéticas)", -0.22010, -78.51240, "oficina", "normal", ("06:00", "08:30"), 15, 3, "Centro Histórico", "sintetico", "Centro Histórico"),
    ("PTO-0027", "Sucursal bancaria Centro Histórico (sintética)", -0.21960, -78.51080, "banco", "normal", ("06:00", "08:30"), 10, 3, "Centro Histórico", "sintetico", "Centro Histórico"),
    ("PTO-0028", "Restaurante San Juan (sintético)", -0.22083, -78.51223, "local_comida", "media", ("06:00", "08:30"), 15, 2, "Centro Histórico", "sintetico", "San Juan"),
    ("PTO-0029", "Unidad educativa La Magdalena (sintética)", -0.24379, -78.52593, "colegio", "normal", ("13:30", "16:00"), 15, 3, "Sur", "sintetico", "La Magdalena"),
    ("PTO-0030", "Centro comercial Villaflora (sintético)", -0.24569, -78.52140, "centro_comercial", "media", ("07:00", "09:30"), 20, 2, "Sur", "sintetico", "Villaflora"),
    ("PTO-0031", "Sucursal bancaria Chimbacalle (sintética)", -0.24328, -78.51225, "banco", "normal", ("08:00", "09:00"), 10, 3, "Sur", "sintetico", "Chimbacalle"),
    ("PTO-0032", "Clínica privada Solanda (sintética)", -0.27505, -78.54126, "clinica", "media", ("07:00", "12:00"), 20, 2, "Sur", "sintetico", "Solanda"),
    ("PTO-0033", "Local de comida Chillogallo (sintético)", -0.27900, -78.55500, "local_comida", "normal", ("07:00", "11:00"), 10, 3, "Sur", "sintetico", "Chillogallo"),
    ("PTO-0034", "Oficinas Quitumbe (sintéticas)", -0.29600, -78.54400, "oficina", "normal", ("08:00", "17:00"), 10, 3, "Sur", "sintetico", "Quitumbe"),
    ("PTO-0035", "Mercado Mayorista - local de abastos (sintético)", -0.27281, -78.53789, "comercio", "normal", ("05:30", "08:00"), 20, 2, "Sur", "sintetico", "Mercado Mayorista"),
    ("PTO-0036", "Unidad educativa La Ecuatoriana (sintética)", -0.30859, -78.56500, "colegio", "normal", ("13:30", "16:00"), 15, 3, "Sur", "sintetico", "La Ecuatoriana"),
    ("PTO-0037", "Oficinas Conocoto (sintéticas)", -0.29000, -78.48000, "oficina", "normal", ("08:00", "17:00"), 10, 3, "Valle Chillos", "sintetico", "Conocoto"),
    ("PTO-0038", "Clínica privada Cumbayá (sintética)", -0.20000, -78.43400, "clinica", "media", ("07:00", "12:00"), 20, 2, "Valle Tumbaco", "sintetico", "Cumbayá"),
    ("PTO-0039", "Hotel Tumbaco (sintético)", -0.21225, -78.40450, "hotel", "media", ("09:00", "12:00"), 15, 2, "Valle Tumbaco", "sintetico", "Tumbaco"),
    ("PTO-0040", "Restaurante Guamaní (sintético)", -0.33000, -78.55000, "local_comida", "normal", ("07:00", "11:00"), 10, 3, "Sur", "sintetico", "Guamaní"),
]

# Catálogo. Peso y volumen por unidad base: estimación física (densidad × contenido
# + envase); el valor unitario es un precio de referencia SINTÉTICO.
# (sku, nombre, unidad_base, peso_kg, volumen_m3, valor_usd, unidades_por_bulto, presentación, vida_util_dias)
PRODUCTS = [
    ("PRD-0001", "Hipoclorito de sodio 5% (galón 3,785 L)", "galon", 4.20, 0.0065, 3.80, 4, "caja de 4 galones", 180),
    ("PRD-0002", "Desinfectante amonio cuaternario (galón)", "galon", 4.00, 0.0065, 7.50, 4, "caja de 4 galones", 540),
    ("PRD-0003", "Detergente industrial en polvo (saco 30 kg)", "saco", 30.30, 0.0450, 42.00, 1, "saco de 30 kg", 730),
    ("PRD-0004", "Jabón líquido antibacterial (galón)", "galon", 4.00, 0.0065, 6.20, 4, "caja de 4 galones", 540),
    ("PRD-0005", "Alcohol gel 70% (frasco 1 L)", "frasco", 0.95, 0.0013, 3.40, 12, "caja de 12 frascos", 720),
    ("PRD-0006", "Papel higiénico jumbo (paquete 4 rollos)", "paquete", 2.80, 0.0300, 9.60, 3, "fardo de 3 paquetes", 1095),
    ("PRD-0007", "Toalla de manos interfoliada (caja 20 paquetes)", "caja", 6.00, 0.0400, 18.50, 1, "caja", 1095),
    ("PRD-0008", "Fundas rojas para desechos infecciosos (paquete 100)", "paquete", 2.00, 0.0040, 8.90, 10, "bulto de 10 paquetes", 1095),
    ("PRD-0009", "Fundas negras para desechos comunes (paquete 100)", "paquete", 2.50, 0.0050, 6.40, 10, "bulto de 10 paquetes", 1095),
    ("PRD-0010", "Guantes de nitrilo (caja 100)", "caja", 0.60, 0.0015, 7.20, 10, "bulto de 10 cajas", 1095),
    ("PRD-0011", "Desengrasante industrial (caneca 20 L)", "caneca", 21.50, 0.0240, 38.00, 1, "caneca de 20 L", 720),
    ("PRD-0012", "Mopa de repuesto (unidad)", "unidad", 0.35, 0.0030, 4.10, 12, "bulto de 12", 1825),
]

# Consumo diario base por categoría (unidad base del SKU). SINTÉTICO.
PROFILE = {
    "hospital": {"PRD-0001": 9, "PRD-0002": 4, "PRD-0003": 0.6, "PRD-0004": 3, "PRD-0005": 14, "PRD-0006": 18,
                 "PRD-0007": 3, "PRD-0008": 7, "PRD-0009": 5, "PRD-0010": 10, "PRD-0012": 2},
    "centro_salud": {"PRD-0001": 2, "PRD-0002": 1, "PRD-0004": 0.8, "PRD-0005": 3, "PRD-0006": 4, "PRD-0007": 0.8,
                     "PRD-0008": 1.5, "PRD-0009": 1, "PRD-0010": 2},
    "clinica": {"PRD-0001": 3, "PRD-0002": 1.5, "PRD-0004": 1, "PRD-0005": 5, "PRD-0006": 6, "PRD-0007": 1,
                "PRD-0008": 2.5, "PRD-0009": 1.5, "PRD-0010": 4},
    "oficina": {"PRD-0002": 0.5, "PRD-0004": 0.6, "PRD-0005": 1.5, "PRD-0006": 5, "PRD-0007": 1, "PRD-0009": 1.2, "PRD-0012": 0.2},
    "banco": {"PRD-0002": 0.3, "PRD-0004": 0.3, "PRD-0005": 1.2, "PRD-0006": 1.5, "PRD-0007": 0.4, "PRD-0009": 0.5},
    "local_comida": {"PRD-0001": 1.2, "PRD-0003": 0.15, "PRD-0004": 0.8, "PRD-0009": 1.5, "PRD-0010": 1.5, "PRD-0011": 0.25},
    "centro_comercial": {"PRD-0001": 2, "PRD-0002": 2, "PRD-0004": 2, "PRD-0005": 4, "PRD-0006": 14, "PRD-0007": 3,
                         "PRD-0009": 4, "PRD-0011": 0.2, "PRD-0012": 1},
    "colegio": {"PRD-0002": 0.8, "PRD-0004": 1, "PRD-0005": 2, "PRD-0006": 8, "PRD-0007": 1.2, "PRD-0009": 1.5, "PRD-0012": 0.4},
    "comercio": {"PRD-0001": 1.5, "PRD-0002": 0.8, "PRD-0004": 0.6, "PRD-0006": 2, "PRD-0009": 2, "PRD-0011": 0.15},
    "industria": {"PRD-0001": 3, "PRD-0002": 2, "PRD-0003": 0.8, "PRD-0004": 2, "PRD-0010": 6, "PRD-0011": 0.6, "PRD-0009": 2},
    "hotel": {"PRD-0001": 1.5, "PRD-0002": 1, "PRD-0003": 0.3, "PRD-0004": 1, "PRD-0005": 1.5, "PRD-0006": 6, "PRD-0007": 1, "PRD-0009": 1.5},
}
WEEKEND = {"hospital": 0.85, "centro_salud": 0.0, "clinica": 0.7, "oficina": 0.0, "banco": 0.0, "colegio": 0.0,
           "local_comida": 1.25, "centro_comercial": 1.3, "comercio": 1.1, "industria": 0.4, "hotel": 1.2}

# Flota. Capacidades de fichas comerciales citadas en el README; volumen útil,
# matrícula, kilometraje, rendimiento y costo/km son SUPUESTOS DE PRUEBA.
# (código, matrícula, marca, modelo, año, combustible, tipo, uso, bodega, kg, m3, km, autonomía_km, rend_km_gal, mant_usd_km, estado)
VEHICLES = [
    ("VEH-0001", "PCB-2214", "Hino", "Dutro City", 2022, "diesel", "camion_liviano", "urbano", "BOD-0001", 3500, 14, 86230, None, 26, 0.09, "operativo"),
    ("VEH-0002", "PCA-1187", "Hino", "Dutro 616", 2021, "diesel", "camion_pesado", "extraurbano", "BOD-0001", 4500, 18, 128450, None, 22, 0.09, "operativo"),
    ("VEH-0003", "PBX-7896", "Chevrolet", "N400 Max Cargo", 2023, "gasolina", "camioneta", "urbano", "BOD-0001", 720, 3.1, 45210, None, 32, 0.06, "operativo"),
    ("VEH-0004", "PCE-3021", "BYD", "T35", 2025, "electrico", "camion_liviano", "urbano", "BOD-0001", 2700, 12, 18320, 270, None, 0.04, "operativo"),
    ("VEH-0005", "PCD-5540", "Hino", "Dutro 716", 2018, "diesel", "camion_pesado", "extraurbano", "BOD-0002", 5500, 22, 214900, None, 20, 0.11, "mantenimiento_proximo"),
    ("VEH-0006", "PBY-4412", "Chevrolet", "N400 Max Cargo", 2020, "gasolina", "camioneta", "urbano", "BOD-0002", 720, 3.1, 132880, None, 30, 0.07, "operativo"),
    ("VEH-0007", "PCC-0915", "Hino", "Dutro City", 2019, "diesel", "camion_liviano", "urbano", "BOD-0002", 3500, 14, 175400, None, 25, 0.10, "operativo"),
    ("VEH-0008", "PBZ-6603", "Chevrolet", "N400 Max Cargo", 2022, "gasolina", "camioneta", "urbano", "BOD-0003", 720, 3.1, 61005, None, 32, 0.06, "operativo"),
    ("VEH-0009", "PCF-1209", "BYD", "T35", 2026, "electrico", "camion_liviano", "urbano", "BOD-0003", 2700, 12, 9870, 270, None, 0.04, "operativo"),
    ("VEH-0010", "PCG-8830", "Hino", "Dutro 816", 2016, "diesel", "camion_pesado", "extraurbano", "BOD-0003", 6500, 26, 301250, None, 18, 0.12, "operativo"),
]
DIESEL_USD_GAL = 3.11      # precio oficial 12-oct-2026 (ver README)
GASOLINA_USD_GAL = 3.17    # Extra/Ecopaís 12-oct-2026
KWH_USD = 0.10             # referencia residencial (fuente comercial, PENDIENTE pliego oficial)
EV_KWH_KM = 0.35           # SUPUESTO de prueba


def cost_per_km(v):
    fuel, rend, mant = v[5], v[13], v[14]
    if fuel == "electrico":
        energy = KWH_USD * EV_KWH_KM
    else:
        energy = (DIESEL_USD_GAL if fuel == "diesel" else GASOLINA_USD_GAL) / rend
    return round(energy + mant, 3)


# Incidentes. Tipos de #246 (Congestion, Manifestacion, Accidente con slowdownFactor),
# BloqueoVial existente y VEHICLE_BREAKDOWN que ya evalúa IncidentEvaluator.
# Cada uno reproduce un hecho publicado (lugar y tipo); el factor de velocidad y la
# instancia dentro de la demo son SINTÉTICOS (isSynthetic: true).
INCIDENTS = {
    "averia-matilde-alvarez": {
        "kind": "VEHICLE_BREAKDOWN", "severity": "ALTA",
        "description": "Hino Dutro City PCB-2214 (VEH-0001, diésel, 86.230 km) se avería por falla de embrague en la Av. Matilde Álvarez (Quitumbe), con 50 km recorridos y paradas pendientes",
        "vehicleCode": "VEH-0001", "plate": "PCB-2214", "kmIntoRoute": 50,
        "lat": -0.28391, "lon": -78.55291, "radiusKm": 0.3, "isSynthetic": True,
        "basadoEn": "Caso planteado por el equipo; la calle existe (OSM w267622146)."},
    "accidente-simon-bolivar-san-martin": {
        "kind": "Accidente", "severity": "ALTA", "slowdownFactor": 0.5,
        "description": "Camioneta se impacta contra un árbol en la Av. Simón Bolívar, sector San Martín (sur); AMT cierra el carril derecho sentido norte-sur",
        "lat": -0.30201, "lon": -78.52142, "radiusKm": 0.8, "isSynthetic": True,
        "basadoEn": "El Diario, 30-mar-2026: https://www.eldiario.ec/seguridad/alerta-vial-en-el-sur-de-quito-choque-contra-un-arbol-provoca-cierres-y-deja-dos-heridos-30032026/"},
    "accidente-redondel-zambiza": {
        "kind": "Accidente", "severity": "MEDIA", "slowdownFactor": 0.4,
        "description": "Choque en el Redondel de Zámbiza (Av. Simón Bolívar), uno de los cinco puntos críticos de siniestros",
        "lat": -0.15036, "lon": -78.45244, "radiusKm": 0.8, "isSynthetic": True,
        "basadoEn": "Primicias, puntos críticos de la Av. Simón Bolívar según la AMT: https://primicias.ec/noticias/quito/avenida-simon-bolivar-problemas-vias"},
    "manifestacion-centro-historico": {
        "kind": "Manifestacion", "severity": "ALTA", "slowdownFactor": 0.3,
        "description": "Marcha desde la Av. 10 de Agosto y Bogotá hacia la Plaza Grande; cierres en Guayaquil, Flores y Montúfar",
        "lat": -0.21951, "lon": -78.51196, "radiusKm": 1.0, "isSynthetic": True,
        "basadoEn": "Extra, movilización del 13-mar-2026: https://www.extra.ec/noticia/quito/marchas-quito-hoy-vias-afectadas-movilizaciones-viernes-148978.html"},
    "inundacion-24-de-mayo": {
        "kind": "BloqueoVial", "severity": "ALTA",
        "description": "Acumulación de agua por lluvia en la Av. Maldonado y Av. 24 de Mayo; la AMT cierra el paso",
        "lat": -0.22319, "lon": -78.51642, "radiusKm": 0.4, "isSynthetic": True,
        "basadoEn": "Extra, lluvias del 6-mar-2026: https://www.extra.ec/noticia/quito/lluvias-tormentas-quito-desvios-cierres-viales-viernes-148474.html"},
    "congestion-hora-pico-maldonado": {
        "kind": "Congestion", "severity": "MEDIA", "slowdownFactor": 0.64,
        "description": "Hora pico en la Av. Pedro Vicente Maldonado, salida de la Bodega Central Sur: 16 km/h frente a 25 km/h fuera de pico",
        "lat": -0.31888, "lon": -78.54932, "radiusKm": 1.2, "isSynthetic": True,
        "basadoEn": "Primicias, Secretaría de Movilidad (16 km/h en hora pico, 25 km/h fuera): https://www.primicias.ec/noticias/sociedad/quito-hoy-no-circula-multas"},
}
PICO_Y_PLACA = {1: "lunes", 2: "lunes", 3: "martes", 4: "martes", 5: "miércoles", 6: "miércoles",
                7: "jueves", 8: "jueves", 9: "viernes", 0: "viernes"}

# ---------------------------------------------------------------------------
# Historia sintética enriquecida (issue #319). Las reglas se fijan antes de
# correr la comparación y se documentan en el README. La ventana y los pares
# punto/SKU usados por la corrida A/B/C del pitch quedan protegidos para no
# alterar sus cifras ya validadas.
ENRICHMENT_VERSION = 2
END_OF_MONTH_FACTOR = 1.18
CAMPAIGN = {
    "start": date(2026, 9, 14),
    "end": date(2026, 9, 18),
    "points": {"PTO-0001", "PTO-0020", "PTO-0030"},
    "skus": {"PRD-0002", "PRD-0006", "PRD-0009"},
    "factor": 1.8,
    "label": "campaña sanitaria sintética de septiembre",
}
TRENDS = {"PTO-0020": 0.30, "PTO-0030": 0.25, "PTO-0036": 0.30}
PROTECTED_ABC_PAIRS = {
    ("PTO-0004", "PRD-0001"), ("PTO-0004", "PRD-0003"),
    ("PTO-0005", "PRD-0001"), ("PTO-0005", "PRD-0003"),
    ("PTO-0006", "PRD-0001"), ("PTO-0006", "PRD-0003"),
    ("PTO-0015", "PRD-0001"),
}
ADVERSE_RETIRED_VEHICLE = "VEH-0001"


def http_json(url, timeout=60):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read().decode("utf-8"))


def snap(lat, lon):
    """Ajusta la coordenada a la calle más cercana con OSRM nearest."""
    data = http_json(f"{OSRM}/nearest/v1/driving/{lon},{lat}?number=1", timeout=30)
    wp = data["waypoints"][0]
    return round(wp["location"][1], 5), round(wp["location"][0], 5), round(wp["distance"], 1), wp.get("name", "")


def haversine(a, b):
    r = 6371.0
    p1, p2 = math.radians(a[0]), math.radians(b[0])
    dp, dl = p2 - p1, math.radians(b[1] - a[1])
    h = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * r * math.asin(math.sqrt(h))


def write_csv(name, header, rows):
    path = os.path.join(OUT, name)
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(header)
        w.writerows(rows)
    return path


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def read_csv(name):
    path = os.path.join(OUT, name)
    with open(path, newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def main():
    only_history = "--solo-historia" in sys.argv
    use_osrm = "--sin-osrm" not in sys.argv and not only_history
    rnd = random.Random(SEED)
    os.makedirs(OUT, exist_ok=True)

    nodes = [("warehouse", w[0], w[2], w[3]) for w in WAREHOUSES] + [("service_point", p[0], p[2], p[3]) for p in POINTS]
    snapped, snap_info = {}, {}
    existing_manifest = {}
    if only_history:
        for name in ("bodegas.csv", "puntos.csv"):
            for row in read_csv(name):
                snapped[row["code"]] = (float(row["lat"]), float(row["lon"]))
        with open(os.path.join(OUT, "manifest.json"), encoding="utf-8") as f:
            existing_manifest = json.load(f)
        snap_info = existing_manifest.get("snapToRoad", {})
    else:
        for kind, code, lat, lon in nodes:
            if use_osrm:
                try:
                    slat, slon, dist, street = snap(lat, lon)
                    snapped[code] = (slat, slon)
                    snap_info[code] = {"distanceM": dist, "street": street}
                    time.sleep(0.3)
                    continue
                except Exception as ex:  # noqa: BLE001 - se registra y se usa la coordenada original
                    print(f"aviso: nearest falló para {code}: {ex}", file=sys.stderr)
            snapped[code] = (lat, lon)

    # --- bodegas y puntos ---
    if not only_history:
        write_csv("bodegas.csv", ["code", "name", "lat", "lon"],
                  [(w[0], w[1], *snapped[w[0]]) for w in WAREHOUSES])
        write_csv("puntos.csv",
                  ["code", "name", "lat", "lon", "region", "category", "criticality", "window_start", "window_end",
                   "service_minutes", "lead_time_days", "fuente_ubicacion", "referencia_osm_o_barrio"],
                  [(p[0], p[1], *snapped[p[0]], p[9], p[4], p[5], p[6][0], p[6][1], p[7], p[8],
                    "OpenStreetMap (ODbL); relación comercial ficticia" if p[10] == "osm" else "sintética", p[11])
                   for p in POINTS])

    # --- productos y lotes ---
    write_csv("productos.csv",
              ["sku", "name", "base_unit", "weight_per_unit", "volume_per_unit", "unit_value", "units_per_package",
               "presentacion", "peso_bulto_kg"],
              [(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], round(p[3] * p[6], 2)) for p in PRODUCTS])
    lots = []
    for p in PRODUCTS:
        # Lote A vence antes (prueba FEFO); el hipoclorito tiene un lote que vence en 20 días.
        first = END_DATE + timedelta(days=20 if p[0] == "PRD-0001" else min(p[8] // 3, 240))
        second = END_DATE + timedelta(days=p[8])
        lots.append((p[0], f"L-{p[0][4:]}-2609", first.isoformat(), "Proveedor sintético A"))
        lots.append((p[0], f"L-{p[0][4:]}-2610", second.isoformat(), "Proveedor sintético B"))
    write_csv("lotes.csv", ["sku", "lot_code", "expires_on", "origin"], lots)

    # --- vehículos ---
    write_csv("vehiculos.csv",
              ["code", "depot_code", "max_weight", "max_volume", "cost_per_km", "vehicle_type", "usage_scope",
               "matricula", "marca", "modelo", "anio", "combustible", "kilometraje_actual", "autonomia_km",
               "rendimiento_km_gal_supuesto", "estado_mecanico", "pico_y_placa_dia"],
              [(v[0], v[8], v[9], v[10], cost_per_km(v), v[6], v[7], v[1], v[2], v[3], v[4], v[5], v[11],
                v[12] or "", v[13] or "", v[15], PICO_Y_PLACA[int(v[1][-1])]) for v in VEHICLES])

    # --- consumos (60 días) ---
    consumos, avg = [], {}
    start = END_DATE - timedelta(days=HISTORY_DAYS - 1)
    for p in POINTS:
        code, cat = p[0], p[4]
        scale = 1.6 if code in ("PTO-0001", "PTO-0004") else 1.0  # hospitales grandes
        for sku, base in PROFILE[cat].items():
            total, days = 0.0, 0
            for i in range(HISTORY_DAYS):
                d = start + timedelta(days=i)
                if code == "PTO-0012" and 30 <= i < 37:
                    continue  # hueco de datos: el centro de salud no reportó una semana
                factor = WEEKEND[cat] if d.weekday() >= 5 else 1.0
                if factor == 0:
                    continue
                if code == "PTO-0005" and i >= HISTORY_DAYS - 5 and sku in ("PRD-0001", "PRD-0008", "PRD-0010"):
                    factor *= 2.4  # brote: sube el consumo de desinfección los últimos 5 días
                protected = (code, sku) in PROTECTED_ABC_PAIRS
                # La ventana de decisión (últimos 14 días) se conserva para no alterar
                # el riesgo ni la planificación ya validada. El enriquecimiento vive
                # en la historia anterior.
                enrichment_window = i < HISTORY_DAYS - 14
                if not protected and enrichment_window and d.day >= 28:
                    factor *= END_OF_MONTH_FACTOR  # cierre de mes sintético
                if (not protected and enrichment_window and CAMPAIGN["start"] <= d <= CAMPAIGN["end"]
                        and code in CAMPAIGN["points"] and sku in CAMPAIGN["skus"]):
                    factor *= CAMPAIGN["factor"]
                if not protected and enrichment_window and code in TRENDS:
                    factor *= 1.0 + TRENDS[code] * (i / max(1, HISTORY_DAYS - 15))
                q = base * scale * factor * rnd.uniform(0.8, 1.2)
                q = round(q, 2) if q < 5 else round(q)
                if q <= 0:
                    continue
                consumos.append((code, sku, d.isoformat(), q, 1))
                total += q
                days += 1
            avg[(code, sku)] = total / HISTORY_DAYS
    write_csv("consumos.csv", ["point_code", "sku", "date", "quantity", "period_days"], consumos)

    # --- inventario ---
    if only_history:
        # Se conserva el inventario validado del fixture. Los quiebres diseñados
        # existentes ya representan el stock inicial desigual.
        stock = [
            (row["location_kind"], row["location_code"], row["sku"], row["lot_code"], row["quantity"])
            for row in read_csv("inventario.csv")
        ]
    else:
        stock = []
        for w in WAREHOUSES:
            for p in PRODUCTS:
                big = 1.0 if w[0] == "BOD-0001" else 0.5
                stock.append(("warehouse", w[0], p[0], f"L-{p[0][4:]}-2609", round(400 * big)))
                stock.append(("warehouse", w[0], p[0], f"L-{p[0][4:]}-2610", round(900 * big)))
        # Quiebre deliberado: la Bodega Valle no tiene guantes (obliga a traer de otra bodega).
        stock = [s for s in stock if not (s[1] == "BOD-0003" and s[2] == "PRD-0010")]
        critical = {("PTO-0005", "PRD-0001"): 0.8, ("PTO-0007", "PRD-0010"): 0.5, ("PTO-0011", "PRD-0008"): 1.0,
                    ("PTO-0002", "PRD-0005"): 1.5, ("PTO-0013", "PRD-0006"): 1.0}
        for p in POINTS:
            for sku in PROFILE[p[4]]:
                daily = max(avg.get((p[0], sku), 0), 0.01)
                cover = critical.get((p[0], sku), rnd.uniform(3, 12))
                stock.append(("service_point", p[0], sku, f"L-{sku[4:]}-2609", round(daily * cover, 2)))
    write_csv("inventario.csv", ["location_kind", "location_code", "sku", "lot_code", "quantity"], stock)

    # --- tramos (matriz dirigida completa) ---
    if only_history:
        source = existing_manifest.get("roadSource", "matriz existente")
    else:
        coords = [snapped[n[1]] for n in nodes]
        source = "haversine x1.35, 25 km/h"
        dist = dur = None
        if use_osrm:
            try:
                q = ";".join(f"{c[1]},{c[0]}" for c in coords)
                data = http_json(f"{OSRM}/table/v1/driving/{q}?annotations=distance,duration", timeout=120)
                if data.get("code") == "Ok":
                    dist, dur, source = data["distances"], data["durations"], "OSRM público (router.project-osrm.org), red vial OpenStreetMap"
            except Exception as ex:  # noqa: BLE001
                print(f"aviso: OSRM table falló, se usa haversine: {ex}", file=sys.stderr)
        tramos = []
        for i, a in enumerate(nodes):
            for j, b in enumerate(nodes):
                if i == j:
                    continue
                if dist is not None and dist[i][j] is not None:
                    km, mins = dist[i][j] / 1000, dur[i][j] / 60
                else:
                    km = haversine(coords[i], coords[j]) * 1.35
                    mins = km / 25 * 60
                tramos.append((a[1], b[1], round(km, 3), round(mins, 3), "", a[0], b[0]))
        write_csv("tramos.csv", ["from_code", "to_code", "distance_km", "duration_min", "geometry", "from_kind", "to_kind"], tramos)

    with open(os.path.join(OUT, "incidentes.json"), "w", encoding="utf-8") as f:
        json.dump(INCIDENTS, f, ensure_ascii=False, indent=2)
        f.write("\n")

    mapping = {"name": "demo-quito-planificacion", "files": {
        "warehouses": {"file": "bodegas.csv"}, "service_points": {"file": "puntos.csv"},
        "products": {"file": "productos.csv"}, "vehicles": {"file": "vehiculos.csv"},
        "lots": {"file": "lotes.csv"}, "stock": {"file": "inventario.csv"},
        "consumptions": {"file": "consumos.csv"}, "road_segments": {"file": "tramos.csv"}}}
    with open(os.path.join(OUT, "mapping.json"), "w", encoding="utf-8") as f:
        json.dump(mapping, f, ensure_ascii=False, indent=2)
        f.write("\n")

    appsettings_data = {
        "Logistics": {
            "ServiceCost": {
                "FuelPricePerGallonUsd": DIESEL_USD_GAL,
                "FuelPriceEffectiveDate": "2026-10-12",
                "FuelPriceSource": "Primicias (12-oct-2026; precio vigente 12-oct a 11-nov-2026)",
                "LoadFuelFactor": 0.3,
                "HelperCostPerHour": 4.13,
                "ExpectedShrinkPct": 0.5,
                "OverloadShrinkPct": 2.0,
                "OverloadThresholdPct": 90.0,
                "VehicleProfiles": {
                    "camioneta": {"LitersPer100Km": 12.21, "WearPerKm": 0.065},
                    "camion_liviano": {"LitersPer100Km": 14.84, "WearPerKm": 0.09},
                    "camion_pesado": {"LitersPer100Km": 18.93, "WearPerKm": 0.10}
                },
                "LaborRules": [
                    {"MaxKg": 150.0, "MaxPackageKg": 25.0, "Helpers": 0},
                    {"MaxKg": 400.0, "MaxPackageKg": 25.0, "Helpers": 1},
                    {"Helpers": 2}
                ]
            },
            "TrafficBands": [
                {"Start": "06:00", "End": "09:30", "SpeedFactor": 0.64},
                {"Start": "12:30", "End": "14:00", "SpeedFactor": 0.80},
                {"Start": "16:00", "End": "20:00", "SpeedFactor": 0.64}
            ],
            "ContractedCarrier": {
                "BaseFeeUsd": 25.0,
                "PerKmUsd": 0.55,
                "PerKgUsd": 0.01,
                "ResponseHours": 4,
                "Name": "Operador Logístico Contratado (Quito)",
                "IsEnabled": True
            },
            "Recommendation": {
                "WeightCost": 0.35,
                "WeightCriticalOnTime": 0.35,
                "WeightPendingDemand": 0.20,
                "WeightRisk": 0.10,
                "ActiveIncidentPenalty": 15.0,
                "OverloadThresholdPct": 90.0,
                "OverloadPenalty": 10.0,
                "ResponseTimePenaltyPerHour": 2.5
            },
            "VehicleRestrictions": [
                {
                    "VehicleType": "camion_pesado",
                    "Corridors": ["Av. Simón Bolívar", "Ruta Viva"],
                    "Bands": [
                        {"Start": "06:00", "End": "10:00"},
                        {"Start": "16:00", "End": "20:00"}
                    ]
                }
            ]
        }
    }
    with open(os.path.join(OUT, "appsettings.demo-quito.json"), "w", encoding="utf-8") as f:
        json.dump(appsettings_data, f, ensure_ascii=False, indent=2)
        f.write("\n")


    files = ["bodegas.csv", "puntos.csv", "productos.csv", "lotes.csv", "vehiculos.csv", "consumos.csv",
             "inventario.csv", "tramos.csv", "incidentes.json", "mapping.json"]
    manifest = {
        "dataset": "demo-quito-planificacion", "version": 1, "seed": SEED, "endDate": END_DATE.isoformat(),
        "historyDays": HISTORY_DAYS, "warehouses": len(WAREHOUSES), "servicePoints": len(POINTS),
        "realPublicLocations": sum(1 for p in POINTS if p[10] == "osm"), "products": len(PRODUCTS),
        "vehicles": len(VEHICLES), "lots": len(lots), "stockRows": len(stock), "consumptionRows": len(consumos),
        "roadSegments": existing_manifest.get("roadSegments", 1806) if only_history else len(tramos),
        "incidents": len(INCIDENTS), "roadSource": source,
        "adverseRetiredVehicle": ADVERSE_RETIRED_VEHICLE,
        "enrichmentVersion": ENRICHMENT_VERSION,
        "historyRules": {
            "weeklySeasonality": "WEEKEND por categoría; sin cambios",
            "endOfMonth": {"days": "28-31", "factor": END_OF_MONTH_FACTOR,
                            "note": "supuesto sintético; no aplica a los pares protegidos del pitch"},
            "campaign": {"start": CAMPAIGN["start"].isoformat(), "end": CAMPAIGN["end"].isoformat(),
                          "factor": CAMPAIGN["factor"], "label": CAMPAIGN["label"]},
            "trends": TRENDS,
            "protectedPitchPairs": sorted([f"{p}/{s}" for p, s in PROTECTED_ABC_PAIRS]),
            "existingDesignedStock": "los cinco quiebres deliberados del fixture se conservan",
        },
        "snapToRoad": snap_info, "sha256": {f: sha256(os.path.join(OUT, f)) for f in files},
    }
    with open(os.path.join(OUT, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print(json.dumps({k: v for k, v in manifest.items() if k not in ("sha256", "snapToRoad")}, ensure_ascii=False, indent=2))
    far = {k: v for k, v in snap_info.items() if v["distanceM"] > 300}
    if far:
        print("aviso: puntos a más de 300 m de una calle:", far, file=sys.stderr)


if __name__ == "__main__":
    main()
