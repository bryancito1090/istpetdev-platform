# Datasets de prueba

Salidas del generador sintético existente, reproducibles byte a byte con la
misma configuración y seed. No son observaciones ni mediciones del simulador.

## Muestras

| Carpeta | Versión | Seed | Escenario | Contenido |
|---|---|---|---|---|
| `national-v1-seed-11/` | `national-v1` | 11 | `base` | 80 puntos, 6 bodegas, 12 vehículos, 2 productos, 90 días |

Cada carpeta incluye los CSV canónicos, `calendar.json`, `manifest.json`,
`mapping.json` (perfil de importación copiado sin cambios) y un README con el
detalle y el comando reproducible.

## Reproducir y verificar

El generador es `ScenarioGenerator.GenerateFromScenarioFileAsync` en
`src/Application/Simulation/ScenarioGenerator.cs`; la configuración de origen es
`data/fixtures/simulation-national/scenario.json`. Todavía no hay un comando de
línea de comandos; las pruebas de `ScenarioGeneratorTests` ejercitan el mismo
llamado con seed 11 y comprueban bytes y SHA-256 idénticos entre ejecuciones:

```bash
dotnet test tests/IstpetDev.Tests/IstpetDev.Tests.csproj \
  --filter "FullyQualifiedName~ScenarioGeneratorTests"
```

Para regenerar en otra carpeta, invocar el generador con
`seed: 11`, `scenarioCode: "base"` y salida en la carpeta destino.
