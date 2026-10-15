# national-v1 — seed 11 (sintético)

Salida del generador sintético existente para la versión `national-v1`.
Todos los registros, coordenadas y cantidades son sintéticos: no representan
clientes, establecimientos ni mediciones reales.

## Parámetros de generación

| Parámetro | Valor |
|---|---|
| Generador | `ScenarioGenerator.GenerateFromScenarioFileAsync` (`src/Application/Simulation/ScenarioGenerator.cs`) |
| Configuración | `data/fixtures/simulation-national/scenario.json` |
| `datasetVersion` | `national-v1` |
| `seed` | `11` |
| `scenarioCode` | `base` |
| `startDate` | `2026-10-12` |
| `horizonDays` | `90` |

Conteos declarados en `manifest.json`: 80 puntos, 6 bodegas, 12 vehículos,
2 productos, 14400 claves de demanda (80 puntos × 90 días × 2 SKU).

## Archivos

| Archivo | Filas | Unidades / notas |
|---|---|---|
| `warehouses.csv` | 6 | Coordenadas sintéticas |
| `service_points.csv` | 80 | Categorías `hospital`, `clinica`, `centro_salud`, `dispensario`; ventanas y criticidad sintéticas |
| `products.csv` | 2 | Peso en kg y volumen en m³ por unidad base |
| `vehicles.csv` | 12 | Capacidad en kg y m³; costo por km |
| `lots.csv` | 2 | Vencimiento a 180 días de `startDate` |
| `stock.csv` | 172 | Cantidad en unidad base (L o UN) |
| `consumptions.csv` | 2240 | Histórico de 14 días previos; cantidad en unidad base por día |
| `road_segments.csv` | 1148 | Distancias y duraciones sintéticas en km y minutos; `geometry` vacía |
| `calendar.json` | 14400 claves | Calendario exógeno con semilla y `Sha256` |
| `manifest.json` | — | Conteos, seed, hashes SHA-256 de todos los archivos |
| `mapping.json` | — | Perfil de importación canónico |

Todos los CSV están en UTF-8, ordenados por código/fecha/SKU, con coma como
separador y punto decimal.

## Comando reproducible

No hay todavía un ejecutable de línea de comandos. La generación es la llamada:

```csharp
await ScenarioGenerator.GenerateFromScenarioFileAsync(
    scenarioPath, outputDirectory, seed: 11, scenarioCode: "base");
```

La reproducibilidad byte a byte está cubierta por
`tests/IstpetDev.Tests/Unit/Simulation/ScenarioGeneratorTests.cs`:

```bash
dotnet test tests/IstpetDev.Tests/IstpetDev.Tests.csproj \
  --filter "FullyQualifiedName~ScenarioGeneratorTests"
```

## Hashes de referencia

- `manifest.json`: `233b968fa854f1d0d1ea20a73d10f8adc455d8fe6f5fe3215662f0c3d4d32809`
- `calendar.json`: `73bc5e99225df82c110173ffdbce0cde57872d4f5d80d452c81cd5b5cfd92dec`
- Hashes por archivo: ver `manifest.json`.
- `mapping.json` (copia sin cambios de `data/fixtures/simulation-national/mapping.json`): `775d5650d2774fdfb8ea22ed18f63fc9a19a138eae91e52a6c62573b7451dadc`

## Uso del perfil de importación

`mapping.json` es el perfil canónico del importador (spec 001): mapea cada CSV
de esta carpeta a su entidad por nombre de archivo. Se copia sin cambios desde
`data/fixtures/simulation-national/mapping.json` para que la muestra sea
autocontenida; se envía al importador como texto con
`-F 'profile=<ruta/mapping.json'`. Importar esta muestra no mide ni declara
resultados del simulador.
