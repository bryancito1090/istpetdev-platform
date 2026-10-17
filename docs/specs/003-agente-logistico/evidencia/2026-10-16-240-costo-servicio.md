# #240 — evidencia y prelaunch

## Alcance previo al arranque

16-oct-2026; rama feature/240-costo-total-servicio. Pruebas locales con PostGIS
16/3.4 descartable y TestServer, catálogos/identidades sintéticos. Sin tráfico a
RDS, Cognito, n8n ni modificación de producción. Una migración aditiva nullable.
No se despliega ni se promueve a main.

Skill aplicada: docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md, con
Verificación de seguridad, esquema, RBAC y Plan de validación de la bóveda.
DemoCore usa autorización por rol/asignación; RLS P2 no aplica. Se reutilizan los
controles existentes de autorización, SQL parametrizado, archivos y proxy donde
no cambian. Solo se consultan productos y puntos de DemoOrganization mediante
LINQ parametrizado; configuración de costos exclusiva de servidor. No hay campos
personales nuevos, plantillas ejecutables, credenciales, cambios de proxy, QR ni
storage. Migración probada en bases descartables; nunca en una base compartida.

Controles exigidos antes del ensayo: fórmulas, importador, API, migración completa
y sobre fixture demo, suite/auditorías y CI del HEAD final (resultados debajo). No se acredita una demo
expuesta ni la aplicación de la migración en producción.

## Resultados de implementación y ejecución local

- Migración generada con EF 8.0.11: `20261016222608_ServiceCostCatalog`.
  Solo cuatro columnas nullable; `HasPendingModelChanges` falso y comprobador
  de migraciones aditivas aprobado. Historial completo: 10 migraciones sobre
  base vacía; importación con rol runtime sin permiso DDL aprobada.
- Copia de la base **local existente** `istpetdev_dev_01` mediante pg_dump/restore:
  API real `--migrate` sobre el clon pasó de 9 a 10 migraciones. Hash JSONB y
  cantidad de filas de **37 tablas / 387 filas** idénticos antes/después,
  excluyendo solo las cuatro columnas nuevas del hash. No se modificó el origen;
  se eliminó el clon. [Hash/conteo por tabla](240-migracion-demo-local.json); log local en
  `artifacts/issue240/demo-migration.log`.
- Prueba adicional reproducible `Migration_upgrades_populated_demo_preserving_every_existing_row_and_value`
  recrea el esquema anterior con fixture demo-base y una ruta histórica: compara
  todas las tablas tras actualizar; conserva también TotalCost histórico.
- 39/39 pruebas dirigidas (`ServiceCost*` y `MigrationSafetyTests`) aprobadas:
  25 unitarias y 14 de integración. Límites 150/400 kg, bulto >25 kg, máximo de
  ayudantes por ruta, combustible creciente, merma estricta >90%, valoración sin
  duplicar entregas, descarga y tiempo explícito 0/27, JSON fechado y autorización
  (Receptor 403), importación PreserveMissing y rechazo transaccional.
- La API real vía TestServer devuelve USD **15,05** para la secuencia
  BOD-UIO → HOS-001. El selector calcula USD **19,94** incluyendo regreso al
  depósito: combustible 2,26 + desgaste 2,15 + conductor 7,22 + ayudante 4,81 +
  merma 3,50. Carga 210/1200 kg, 200 L DES-01, 20 bultos, 1 ayudante, 20 min
  de descarga; precio USD 3,785/gal, 16-oct-2026, fuente sintética.
- [Respuesta JSON](240-desglose-demo.json), [reporte HTML](240-desglose-demo.html)
  y [captura del reporte](240-desglose-demo.png). Datos obtenidos del motor,
  sin inventar cifras de pantalla; no es una captura de la UI de #242.
- `npm run check` aprobado. Auditoría .NET: sin paquetes vulnerables.
  `npm run audit:check`: aprobado con las 7 excepciones vigentes del repositorio,
  sin añadir excepciones. Bóveda: 0 errores y 4 avisos preexistentes.
  `build-workflows.py --check`, migraciones aditivas y `git diff --check` aprobados.

Correcciones durante el ensayo: la primera ejecución dirigida tuvo cinco fallos
de preparación/aserción del harness (reporte PreserveMissing, fixture ausente en
API y siembra del esquema histórico con modelo actual). Se corrigieron y las
39 pruebas pasaron. La primera suite completa fue abortada por caída del host de
pruebas tras 201 casos aprobados, concurrente con la compilación Angular; no se
cuenta como suite aprobada. Se conserva `backend-full-aborted.*` y se repite con
`DOTNET_PROCESSOR_COUNT=4`: **559/559 aprobadas** en 4 min 35 s. Después se
ajustó la cuadrilla máxima de todas las paradas y se añadió su regresión; la
cotización sin fecha/fuente se oculta completa. Las dirigidas de la compilación
final verifican esas ramas y readiness 200. El CI del PR debe cubrir toda la
suite sobre el HEAD final.

## Límites y coordinación

Sin despliegue, sin migración de RDS/producción y sin activar ServiceCost por
defecto. #243 calibra Quito; aquí se usa demo-base con parámetros sintéticos
explícitos del test. `service-cost.example.json` no se carga automáticamente.
Anthony (#237/#245) debe consumir ServiceCostCalculator/ServiceCostBreakdown en
el `costBreakdown` legado de `/suggest` y en `/confirm` si necesita persistencia
desglosada: ese controlador se dejó intacto según la coordinación. La pantalla
es #242. #244/#245 deben rebasar sobre este PR. No se afirma que la calibración
con viajes reales (#204) esté terminada.

## Verificación final local

Compilación final: **40/40 pruebas dirigidas aprobadas** (26 unitarias, 14 de
integración, incluidas las 2 de seguridad de migración); sin omitidas. Se verifica
la cuadrilla de recogida viajando a entregas menores, sustitución de reglas por
configuración, precio sin metadatos oculto, readiness 200 y preservación de tiempo
explícito. La suite completa anterior a esos ajustes pasó 559/559; el CI debe
repetir las categorías Unit/Integration/E2E sobre el commit publicado. ManualDeepSeek
no pertenece al alcance ni a esas categorías. No cambian paquetes ni lockfiles.

Finalización remota: consultar el PR vinculado en [#240](https://github.com/bryancito1090/istpetdev-platform/issues/240)
para CI del HEAD final, conflictos, revisión y SHA de merge; el estado del issue
acredita el cierre. GitHub impide aprobación propia si autor y sesión coinciden;
no se sustituye con un comentario de revisión. El reporte para #200 usará la
captura real de este ensayo e indicará que el alcance es develop, sin despliegue.

Sincronización de rama: se integró develop `82c4f65` (#239). Conflicto documental
en Rutas y sobrecostos resuelto conservando la tabla de propuestas del equipo y
la evidencia de #240, sin afirmar que el resto esté implementado. Código y
migración sin conflictos; no se reescribió ningún snapshot manualmente.

Comprobaciones después del rebase documental: bóveda 109 notas / 1199 enlaces,
0 errores y 4 avisos preexistentes; diff completo contra develop sin errores ni
marcadores de conflicto; migraciones aditivas aprobadas. No hubo cambios de
código respecto a las 40 pruebas finales. El hook pre-commit del repositorio no
es ejecutable; el lint requerido fue cubierto por `npm run check` aprobado.
