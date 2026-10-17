# Revisión T6 — corrida final y lámina 4

**Estado:** base sincronizada y configuración validada; ejecución final y captura pendientes.

## Progreso comprobado

- `develop` avanzó a `33387ca`, merge PR #283, que incorpora #275. La rama del issue se actualizó por fast-forward y los cambios de T1–T5 se reaplicaron sin conflictos.
- Configuración versionada: `data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json`. Huella logística: `69cc3e59b1d4b223e52f209e83ae524e6792ae4606af5e1f525fee46e9c82b79`.
- `node scripts/demo/generar-corrida-abc.mjs --config data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json` validó fixture, selección y configuración en modo offline. No llamó a la API ni tocó la base.
- `npm run test --workspace=apps/web`: 28 archivos, 161 pruebas aprobadas.
- `npm run build:web`: ESLint y compilación de producción aprobados; advertencia existente de Leaflet CommonJS.
- `dotnet build src/Api --no-restore`: correcto, cero advertencias y errores.
- `dotnet test tests/IstpetDev.Tests --filter FullyQualifiedName~ContractedCarrierAndRecommendationTests --no-restore`: seis pruebas aprobadas.

## Qué impide cerrar la evidencia final

En esta máquina Docker no tiene un daemon activo y no responden los puertos locales 5000 ni 5001; por eso no hay API local configurada que reciba el reset. No se ejecutó `POST /api/v1/demo/reset`, no se crearon pedidos y no se generaron JSON ni capturas. La propuesta de selección sigue con estado `propuesta-pendiente-joseph`; no se marcará como final hasta que Joseph apruebe puntos, cantidades, prioridades y fecha.

La guía T4 documenta el comando y la protección de reset. Cuando se tenga la base local aislada, API levantada con la configuración logística integrada y selección aprobada, el flujo será:

```powershell
$env:DEMO_API_TOKEN = 'TOKEN_LOCAL_DE_PLANIFICADOR'
$env:DEMO_LOCAL_RESET_ACK = 'I_CONFIRM_LOCAL_DEMO_DATABASE'
node scripts/demo/generar-corrida-abc.mjs --execute --config data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json
Remove-Item Env:DEMO_API_TOKEN
Remove-Item Env:DEMO_LOCAL_RESET_ACK
```

Si se decide validar antes de la aprobación, el run debe ser explícitamente provisional con `--allow-provisional-selection`, y no se usa como evidencia final. Revisar que la respuesta tenga propias y contratada, recomendación del motor, combustible con valor/fecha/fuente, cuatro pedidos del caso y metadatos de commit/configuración/selección. Solo tras esa comprobación cargar la corrida guardada en la UI y capturar la lámina 4.

## Revisiones aún necesarias

1. Joseph aprueba o corrige la [propuesta T3](seleccion-issue-276.propuesta.json).
2. El operador inicia Docker y la API contra una base demo local aislada, con `appsettings.demo-quito.json` y credenciales locales.
3. Ejecutar el generador; cotejar selección y respuesta con la tabla de Rutas.
4. Abrir la corrida guardada, comprobar rótulo y lectura de solo visualización, y capturar la tabla para la lámina 4.
5. Guardar el JSON y la captura en `docs/specs/006-validacion-pitch-entrega/evidencia/issue-276/`; actualizar guion y matriz solo con lo que haya quedado verificado.

**Resultado de usuario/Joseph:** pendiente. **Corrida final:** no generada. **Evidencia del pitch:** pendiente.
