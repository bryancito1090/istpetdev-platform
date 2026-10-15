# Datasets de IstpetDev

Datos usados para probar el sistema y fuentes públicas que aportan contexto.
Aquí no hay datos operativos ni mediciones de la operación real.

## Organización

```text
docs/dataset/
├── README.md
├── plan.md
├── test/
│   ├── README.md
│   └── national-v1-seed-11/       # salida del generador existente; todo sintético
└── reales/
    ├── README.md
    └── acess-2026-09/            # agregado público anonimizado, no operativo
```

| Carpeta | Contenido | Origen | Uso |
|---|---|---|---|
| `test/` | Dataset reproducible para pruebas del sistema | `ScenarioGenerator` sobre `data/fixtures/simulation-national/scenario.json` | Importación canónica, rutas, simulación y pruebas locales |
| `reales/` | Muestras públicas pequeñas o derivadas | Fuentes abiertas citadas en cada README | Contexto geográfico o estadístico; no se inserta solo |

## Reglas

- Todo dato sintético se etiqueta como sintético; sus coordenadas son plausibles, no clientes reales.
- Los agregados públicos eliminan identificadores personales, tributarios, institucionales y de permiso.
- Los datos públicos no se insertan automáticamente en la base de demo.
- Ni los datos sintéticos ni los públicos se presentan como demanda, inventario o rutas observadas.
- Cada muestra conserva fuente, fecha de descarga, licencia, transformación y SHA-256.

## Artefactos actuales

- `test/national-v1-seed-11/`: fixture canónico `national-v1`, seed 11, escenario base; 80 puntos, 6 bodegas, 12 vehículos, 2 productos, horizonte 90 días. Ver `test/national-v1-seed-11/README.md`.
- `reales/acess-2026-09/`: agregado de permisos de funcionamiento de salud (ACESS) por provincia, cantón, tipo y categoría. Ver `reales/acess-2026-09/README.md`.
