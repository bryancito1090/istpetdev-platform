# Issue #276 — entrega T4: generador de corrida

Estado: **generador implementado y validación sin red aprobada; prueba contra API local pendiente**.

## Qué hace

`scripts/demo/generar-corrida-abc.mjs` carga la propuesta T3, CSV y manifiesto; valida hashes, puntos y productos; exige siete muestras diarias completas por línea y vuelve a derivar las cantidades. Sin parámetros de ejecución es una comprobación sin red ni escrituras.

Al ejecutarse, exige API HTTP en loopback, token Planificador, configuración local completa y una marca de confirmación de base aislada. Reinicia el fixture, obtiene IDs recién creados, crea pedidos con vencimiento al final de cada ventana local, solicita sugerencias con los cuatro IDs y guarda respuesta y metadatos mediante archivo temporal y renombrado. Registra fecha de captura, commit, cambios locales incluidos como hash, hashes del fixture, configuración, selección, petición, IDs/números de pedido y fecha operativa del servicio. Exige C y precio de combustible fechado/con fuente antes de llamar corrida A/B/C al archivo.

Las respuestas de puntos y productos de `GET /api/v1/imports/data/...` no exponían sus IDs. Se añadieron IDs a esos resultados para que el generador resuelva entidades sin consultas SQL.

El reset local no eliminaba pedidos previos y podía impedir repetir una corrida al borrar sus destinos. Ahora limpia primero líneas de parada y pedidos/líneas junto con los registros operativos. Una prueba de integración añade un pedido y comprueba que un segundo reset lo retira.

## Comprobaciones realizadas

- `node scripts/demo/generar-corrida-abc.mjs`: aprobado; mostró cuatro pedidos, siete líneas y cantidades derivadas (42+3, 51+2, 26+2, 5). No llamó la API ni modificó la base.
- `node scripts/demo/generar-corrida-abc.mjs --help`: aprobado.
- `node --check scripts/demo/generar-corrida-abc.mjs`: aprobado.
- `dotnet build src/Api --no-restore`: aprobado; 0 advertencias y 0 errores.
- `git diff --check`: aprobado.
- `dotnet test tests/IstpetDev.Tests --filter FullyQualifiedName~DemoQuitoPlanificacionTests --no-restore`: no pudo ejecutar la prueba; Testcontainers informó que Docker no está ejecutándose o configurado. El resultado no es un fallo funcional ni una aprobación de la prueba. Debe repetirse con Docker disponible.
- No se ejecutó `--execute`, no se llamó al reset y no se creó JSON de corrida.

## Comandos y requisitos de ejecución

1. Usar una base local aislada de Docker para la demo. No usar túnel a la base central. `localhost` del proceso API no garantiza por sí solo que su PostgreSQL sea local; el script pide confirmación separada.
2. La configuración de demo llegó en #275 (PR #283) y está en `data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json`. El script la validó offline con SHA-256 `69cc3e59b1d4b223e52f209e83ae524e6792ae4606af5e1f525fee46e9c82b79`. Para el servicio API se requiere además la configuración local de base de datos, no versionada.
3. Iniciar la API con esa configuración y autenticación local de Planificador. No guardar token en el JSON de configuración ni en Git.
4. Validar antes de tocar datos:

   ```powershell
   node scripts/demo/generar-corrida-abc.mjs --config RUTA_A_CONFIG_LOCAL.json
   ```

5. Para prueba provisional local antes de que Joseph apruebe la selección, ejecutar conscientemente:

   ```powershell
   $env:DEMO_API_TOKEN = 'TOKEN_LOCAL_DE_PLANIFICADOR'
   $env:DEMO_LOCAL_RESET_ACK = 'I_CONFIRM_LOCAL_DEMO_DATABASE'
   node scripts/demo/generar-corrida-abc.mjs --execute --allow-provisional-selection --config RUTA_A_CONFIG_LOCAL.json
   Remove-Item Env:DEMO_API_TOKEN
   Remove-Item Env:DEMO_LOCAL_RESET_ACK
   ```

   La selección de T3 sigue marcada como pendiente de Joseph. El switch hace explícito que esa ejecución es provisional. Al aprobarse, cambiar el estado de selección según la decisión documentada y ejecutar sin `--allow-provisional-selection`.

El reset reemplaza los datos operativos de la organización demo, incluido pedidos y rutas. En caso de respuesta incompleta o error de red después del reset, no se escribe la corrida; se puede repetir el generador sobre esa base local, porque el siguiente reset limpia también los pedidos que el mismo generador haya creado.

## Reproducibilidad y límites

El hash de configuración representa las secciones logísticas seleccionadas del archivo pasado por `--config`. La API no publica una huella de configuración efectiva; para que esa huella corresponda al proceso servido, se debe iniciar la API con ese mismo archivo/valores y sin sobreescrituras ambientales. El metadata identifica los cambios locales con un hash del diff y de archivos sin seguimiento; el `commit` solo identifica HEAD.

El script exige respuesta contractual C y precio con valor/fecha/fuente. Con configuración incompleta, se detiene sin escribir el JSON final. La fecha de los pedidos se traduce desde cada ventana en `America/Guayaquil`; el servicio de reloj operativo devuelve la fecha del dataset, mientras `/suggest` crea inicialmente una hora de ruta propia. La respuesta registra la fecha operativa; comparar estos relojes en la ejecución real sigue pendiente.

## Revisión manual de T4

La validación sin red ya se puede repetir con el primer comando anterior. Para la ejecución de API, primero disponer de Docker y configuración local, confirmar visualmente el host/puerto de base local y luego ejecutar el comando provisional. Revisar que la respuesta tenga `comparison.options` con C, combustible fechado, pedidos esperados, bodegas efectivas y ruta/advertencias; abrir el JSON generado y cotejar metadatos. Un segundo run debe dejar cuatro pedidos del nuevo reset, no duplicar los de la corrida anterior.

**Resultado del usuario:** pendiente. La selección sigue pendiente de aprobación y esta corrida no es evidencia final.
