# Plan de resolución del issue #165: Dataset y cifras para el simulador de 90 días

Fecha: 2026-10-15. Destino: `develop`. Rama: `feature/165-dataset-simulador-90-dias`.

## Objetivo y Criterios de Aceptación

Proveer y validar el dataset reproducible de 90 días para el simulador (/simulation), asegurar su importación limpia desde `/datos` y fijar/documentar en la bóveda (`07 Demo`) las cifras exactas y repetibles del benchmark de 12 corridas para el pitch.

Criterios de aceptación (del issue):
1. **Aislamiento y configuración corregida:** El simulador soporta `ConnectionStrings:Main` y `ConnectionStrings:Simulation` tanto en desarrollo local como en Docker Compose de producción sin disparar `422 SIMULATION_DATABASE_NOT_ISOLATED`.
2. **Importación limpia del dataset nacional:** El fixture sintético reproducible de 90 días (`docs/dataset/test/national-v1-seed-11/`) se importa desde `/datos` (`ImportService`) sin advertencias ni errores en dry-run ni en apply real.
3. **Corrida y benchmark de 90 días:** Se ejecuta la comparación de 90 días con las semillas fijadas `[42, 101, 2024]` y el escenario adverso `9999` sobre las 3 políticas (`reactive-v1`, `fixed7-v1`, `istpetdev-v1`).
4. **Cifras documentadas en Bóveda para el pitch:** Las métricas resultantes (km, costos, entregas, costos por entrega) y los artefactos exportados (`.zip` y `.json`) quedan documentados en `docs/boveda/07 Demo/` y referenciados en el guion del pitch.
5. **Verificación completa y merge:** Pasan todas las pruebas automatizadas, checks de repo/bóveda, PR aprobado, merge a `develop`, rama eliminada e issue #165 cerrado.

---

## Fases de Ejecución

### Fase 1: Configuración e Infraestructura de Simulación
- En `src/Infrastructure/Simulation/SimulationService.cs`: dar prioridad a `ConnectionStrings:Main` con fallback a `DefaultConnection`.
- En `src/Api/appsettings.Development.json`: configurar `ConnectionStrings:Simulation` con base aislada `istpetdev_simulation`.
- En `infra/compose/docker-compose.prod.yml`: exponer `ConnectionStrings__Simulation` parametrizada con `SIMULATION_DB_NAME`.

### Fase 2: Prueba de Integración de Importación de Dataset
- Incorporar `tests/IstpetDev.Tests/Integration/Imports/NationalDatasetImportTests.cs` en la suite de integración con Testcontainers / PostgreSQL efímero.
- Validar dry-run y apply real con conteos esperados (80 puntos, 6 bodegas, 12 vehículos, 2 productos, 172 saldos, 2240 consumos, 1148 tramos viales).

### Fase 3: Ejecución de Benchmark y Generación de Artefactos Canónicos
- Incorporar y ejecutar `tests/IstpetDev.Tests/Performance/GenerateBenchmarkArtifactsTest.cs` para generar:
  - Paquete exportado `docs/evidence/simulation-benchmark/simulation-{comparisonId}.zip`
  - Resumen canónico `docs/evidence/simulation-benchmark/comparison-{comparisonId}.json`
- Probar la validación de integridad (`ExportBuilder.ValidateZipPackageAsync`) y la importación de corrida (`ImportComparisonAsync`).

### Fase 4: Documentación Canónica en Bóveda
- Crear `docs/boveda/07 Demo/Cifras del simulador 90 dias.md` con la tabla comparativa de resultados, métricas clave (ahorro, km, costo por entrega) y parámetros del benchmark.
- Actualizar `docs/boveda/07 Demo/Guion del pitch.md` enlazando las cifras oficiales para la sección de Impacto y Viabilidad.
- Actualizar `docs/boveda/04 Datos y algoritmos/Dataset y escenarios.md` con las referencias a este fixture e importación.

### Fase 5: Verificaciones del Repositorio y CI
- Ejecutar `dotnet test tests/IstpetDev.Tests -c Release`.
- Ejecutar `npm run check`.
- Ejecutar `python3 docs/boveda/.github/scripts/check_vault.py`.
- Ejecutar `python3 -I scripts/db/check-additive-migrations.py`.
- Ejecutar `bash .github/scripts/branch-policy.sh`.

### Fase 6: Publicación, PR, Merge y Cierre
- Crear commits Conventional Commits claros.
- Push a `origin/feature/165-dataset-simulador-90-dias`.
- Crear Pull Request vinculando `Closes #165`.
- Esperar que todos los checks de CI pasen en verde.
- Mergear PR hacia `develop` eliminando la rama remota y local.
- Cerrar el issue #165.

---

## Estado

- [x] Plan de resolución preparado.
- [x] Fase 1: Configuración de aislamiento aplicada.
- [x] Fase 2: Prueba de importación de dataset de 90 días integrada.
- [x] Fase 3: Benchmark ejecutado y artefactos generados.
- [x] Fase 4: Bóveda y guion del pitch documentados.
- [x] Fase 5: Suite completa de pruebas y checks de repositorio aprobados.
- [ ] Fase 6: PR fusionado, ramas eliminadas e issue #165 cerrado.
