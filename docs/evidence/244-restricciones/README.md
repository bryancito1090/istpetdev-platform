# #244 — restricción horaria y uso extraurbano

Plan: opciones por tipo/uso/zona, descarte por cruce temporal, aviso extraurbano independiente, pruebas de motor/API y asistente, captura, PR a develop y cierre tras CI.

Base inicial: `ef31353` (incluye #240, #245 y #241). Worktree y rama propios para no cambiar el directorio de Antigravity. No se conecta a producción ni se cargan datos en bases compartidas.

## Contrato y decisiones

- Configuración `Logistics:VehicleRestrictions` como lista; cada regla tiene `VehicleType` y/o `UsageScope`, `Start`, `End`, y `Zone` opcional. Selector doble = ambos deben coincidir. Horas locales `HH:mm`, intervalo semiabierto, soporte nocturno; regla inválida se ignora.
- `Zone` compara exactamente las regiones de puntos de la organización. No interpreta vías, tramos atravesados, perímetros ni ordenanzas. Si no hay región conocida, no se infiere coincidencia de zona.
- El matcher conserva sus estimaciones y costos previos. Para la ventana de restricción incluye viaje desde y hacia depósito, espera de apertura, servicio y factores de hora pico; distancias aproximadas según su método existente. Conserva la precedencia de descartes de capacidad, ocupación y jornada.
- Se agrega `Logistics:UrbanRegions` como segunda propiedad al final de `LogisticsOptions`, necesaria para no codificar ni adivinar qué regiones son urbanas. Ambas listas están vacías por defecto. El enlace de opciones existente ya cubre las propiedades; no requiere otro registro DI.
- `uso_extraurbano` es un elemento de `warnings`, independiente de `DiscardReason`, y no cambia la elegibilidad. El asistente permite seleccionar un vehículo con ese aviso.
- `RouteEvaluator` reutiliza la regla en `/evaluate` y `/confirm`; una confirmación manipulada recibe 409 sin crear plan. El cambio corresponde al asistente de rutas por pedidos, no agrega restricciones al planificador automático legacy.
- Ejemplo reproducible optativo en [appsettings.RestriccionesSinteticas.json](appsettings.RestriccionesSinteticas.json). **No se carga automáticamente**. El archivo `appsettings.demo-quito.json` descrito en la documentación del fixture no existe en esta base; su transporte/configuración está coordinado en #275. Este PR no modifica ese archivo compartido.
- Las horas 06:30–09:30 y la lista urbana del ejemplo son **sintéticas**. No son una interpretación normativa vigente de Quito. Sin migración, sin despliegue ni cambios a la base productiva recién limpiada.

## Prelaunch para la captura local

Aplicada la skill `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` del directorio principal; fuente editable en `docs/boveda/08 Skills/istpetdev-prelaunch.md`. Referencias revisadas: seguridad antes del lanzamiento, contrato API, backend, frontend, revisión, validación y runbook DemoCore.

| Control | Resultado y límites |
|---|---|
| Entorno | Pruebas API con `ApiFactory`, firma JWT de prueba y PostGIS descartable; configuración `Testing` impide cargar `.env` de desarrollo. Captura Angular por loopback en puerto separado, con sesión de prueba inyectada. No se valida Cognito ni RDS. |
| Acceso y campos | Se conservan las autorizaciones de los endpoints. Prueba de Receptor denegado en `/evaluate`; `/confirm` comprueba la restricción del lado servidor. Regiones filtradas por organización. |
| Datos/SQL/archivos | EF parametrizado; ninguna escritura nueva en la consulta de vehículos ni en las capturas. No cambia stock, roles, cifrado, QR, cargas de archivos o plantillas. Sin migraciones. |
| RLS / VerifyFull | DemoCore sin RLS según runbook; solo contenedor local descartable para integración. Configuración TLS de producción sin cambios, runtime RDS no reensayado. |
| Dependencias | `npm run audit:check` verde, 7 avisos cubiertos por excepciones aprobadas vigentes. No se cambia lockfile ni paquetes; imágenes productivas fuera del alcance. |
| Proxy y abuso | No se exponen servicios a la red; servidor local de captura solo en 127.0.0.1. No acredita cabeceras del proxy productivo ni capacidad del release. |
| Resultado | Se puede ejecutar la captura local aislada. No declara lista una demo expuesta ni autoriza/promueve un despliegue. |

## Verificación

Resultados locales observados el 16-oct-2026 (America/Guayaquil; 17-oct UTC):

- `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~VehicleRestriction'`: **21 pruebas en verde**, 0 omitidas; incluye PostGIS real descartable y HTTP con validación JWT de pruebas. Cubre 07:00/10:00, espera que entra en la franja, medianoche, límites, selectores, zona, aviso, ausencia de configuración y confirmación manipulada rechazada sin crear plan.
- `dotnet test tests/IstpetDev.Tests --no-restore --filter 'FullyQualifiedName~Logistics'`: **256 pruebas en verde**, 0 omitidas; incluye las regresiones de costos, hora pico y logística existentes.
- `npm run test --workspace=apps/web`: **158 pruebas en 27 archivos**, todas en verde. La nueva prueba del asistente verifica el motivo, la selección bloqueada para el descartado y la selección permitida con aviso.
- `npm run check`: **verde** (lint FSD, ausencia de login simulado, .NET, shared-core, web, móvil y artefactos `/campo/`). Se conservan los avisos de compilación/linter existentes.
- `node_modules/.bin/tsc -p apps/web/tsconfig.spec.json --noEmit --rootDir .`: verde. La raíz explícita corresponde a los imports del monorepo.
- `npm run audit:check`: verde con 7 avisos cubiertos por excepciones aprobadas vigentes; ningún paquete nuevo.
- `python docs/boveda/.github/scripts/check_vault.py`: **0 errores**, 4 avisos preexistentes.
- Playwright/Chromium: **verde**, motivo visible a las 07:00, pesado compatible a las 10:00, selección permitida con aviso extraurbano, sin errores de ejecución del navegador. Reporte en [browser-validation.json](browser-validation.json).
- Capturas: [07:00, pesado descartado](salida-0700.png) y [10:00, pesado compatible](salida-1000.png). Se usa el componente real `RutasPage`, con sesión de prueba inyectada y los resultados del matcher guardados por la integración en [matches-0700.json](matches-0700.json) y [matches-1000.json](matches-1000.json). No representan Cognito, RDS ni recorrido empresarial real. Los precios mostrados son los resultados sintéticos del calculador existente.
- El entrypoint temporal de captura se retira antes de publicar. No se entrega un acceso alternativo de sesión. `git diff --check` sin errores.

Reproducción: la prueba de integración carga el JSON de configuración de esta carpeta y genera los resultados del matcher en `artifacts/issue244/`; se comparan esos resultados al montar el asistente para la captura.

## Para el pitch (#200)

Estado comprobado: implementación y pruebas locales completas; integración se confirma al cerrar el PR y despliegue queda fuera de este cambio. Frase acotada: «En el asistente, una regla sintética puede descartar el camión pesado si la ruta cruza el horario restringido y explicar el motivo; un aviso extraurbano informa sin impedir elegir el vehículo».
