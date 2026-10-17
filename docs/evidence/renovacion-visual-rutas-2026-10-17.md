# Revisión local para el plan visual de rutas

Fecha: 17-oct-2026, America/Guayaquil. Código base: `develop`, `9e1d6d4`. Workspace: `/home/bryan/Projects/ISTPETDEV`. Al inicio, árbol de trabajo limpio. Los cambios de esta entrega son documentación.

Alcance: análisis de interfaz y disponibilidad del entorno de desarrollo existente. Se aplicó `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` y se consultaron sus referencias de seguridad, runbook ADR-21, esquema, RBAC, validación y evidencia de revisión/frontend/backend/dbflow. No es una aprobación de release o demo expuesta.

## Evidencia observada

| Comprobación | Resultado | Alcance y límites |
| --- | --- | --- |
| Rama y SHA | `git status --short --branch`: `develop...origin/develop`; HEAD `9e1d6d4` | No se realizó fetch; no se acredita vigencia del remoto por esa salida |
| Runtime web | PID 1398076, `ng serve --port 4200 (web)`; cwd `apps/web` de este workspace | Ya estaba iniciado; se conservó sin reinicio |
| Runtime API | PID 1398484, binario de `src/Api/bin/Debug/net8.0`, entorno Development | Ya estaba iniciado; no se recompiló ni se acredita el SHA embebido del binario |
| Landing y entrada SPA de rutas | `GET http://localhost:4200/` y `/rutas`: 200 | El 200 de `/rutas` entrega la SPA; no acredita acceso autenticado a la página |
| Landing visual | Captura real Chromium headless, 1440 × 1000 | Revisión de composición, colores, tipografía y controles; captura temporal en `/tmp/istpetdev-landing-review.png` |
| Salud del proceso | `GET http://localhost:5000/health/live`: 200, `Live` | Sonda de lectura |
| Disponibilidad de DB | `GET http://localhost:5000/health/ready`: 200, `Healthy`; `database_ready` disponible | No identifica esquema aplicado ni demuestra invariantes de negocio |
| Dependencias locales | `docker ps`: PostgreSQL y Redis locales `healthy` | Sin arrancar, resetear ni modificar contenedores |
| Acceso sin token | `GET /api/v1/route-plans`: 401 | Verifica rechazo anónimo de este endpoint; permisos por rol/asignación pendientes |
| Pruebas web | 31 archivos y 173 pruebas aprobadas, Vitest 4.1.11 | Suite completa del código actual, incluida rutas y mapa; no verifica un rediseño aún no implementado |
| Auditoría npm | `npm run audit:check`: exitosa, 7 avisos autorizados | Excepciones aprobadas vigentes según la compuerta del repositorio; no significa cero vulnerabilidades |
| Documentación | `python docs/boveda/.github/scripts/check_vault.py`: 0 errores, 4 avisos de redacción; `git diff --check`: correcto | Los avisos están en notas de requisitos, ADRs y sobrecostos sin cambios en esta entrega |
| Cabeceras web locales | `curl -I http://localhost:4200/`: 200; servidor de desarrollo sin CSP ni cabeceras de proxy de producción | No acredita las cabeceras finales de Nginx/CDN |

La ejecución inicial intentó filtrar las pruebas mediante `npm run test:web -- --watch=false --include=...`; npm consumió `--include` y emitió una advertencia. La salida confirma que se ejecutó **toda** la suite con `ng test --watch=false`, 173/173. Para un filtro efectivo, el plan documenta el comando directo al workspace con `--` antes de los argumentos Angular.

Logs temporales de la sesión: `/tmp/istpetdev-rutas-baseline.log` y `/tmp/istpetdev-rutas-audit.log`. Se resumen aquí los resultados; los temporales no se versionan.

## Registro complementario de prelaunch

| Control | Estado | Evidencia o pendiente |
| --- | --- | --- |
| Aislamiento/RLS | No aplica al diseño DemoCore según ADR-21; esquema runtime pendiente | No se consultaron políticas, grants ni migraciones de la DB; la salud no los acredita |
| Autorización | Verificado parcialmente | Anónimo recibe 401; sesión Cognito, cuenta habilitada, Auditor y asignación deben probarse con acceso autorizado |
| Campos y plantillas administrables | No aplica al cambio documental | No se modificaron DTOs, formularios o plantillas; no se ensayaron payloads de privilegios |
| Datos clasificados/cifrado | Pendiente para runtime | Sin consultas de datos personales, exportaciones ni pruebas de cifrado por campo |
| Contenido, SQL y archivos | No aplica al cambio; runtime no revalidado | No cambian renderers, queries ni uploads; el plan preserva el escape de datos del mapa |
| Abuso, QR y cabeceras finales | Pendiente para entorno expuesto | Sin ráfagas, tráfico QR ni ensayos a través de proxy; solo cabeceras del dev server local |
| Dependencias | npm verificado; .NET e imágenes pendientes | Lockfile sin cambios; no se ejecutó un nuevo scan NuGet ni de contenedores |

No se iniciaron procesos adicionales: el entorno solicitado ya estaba activo y se verificó su disponibilidad. Se mantiene para probar la interfaz actual en [landing](http://localhost:4200/) y [rutas](http://localhost:4200/rutas), con API en [health/ready](http://localhost:5000/health/ready). La ruta privada requiere inicio de sesión real y cuenta habilitada; no se generaron credenciales ni se alteraron guards.

## Pendientes de la implementación

El [plan de renovación](../specs/002-motor-logistico/plan-renovacion-visual-rutas.md) define las tareas y los criterios de aceptación. No se implementó el nuevo tema, no se sustituyeron todavía los emojis del componente y no se desplegó software. La prueba visual autenticada de rutas y los flujos reales del agente siguen pendientes.

## Reinicio solicitado — 17-oct, 09:31 Guayaquil

Registro previo al arranque: el usuario solicita reiniciar frontend y backend. HEAD sigue en `9e1d6d4`, con cambios exclusivamente documentales; no hay diferencias en `apps`, `src` o lockfiles. Se reutiliza la revisión de controles anterior con sus pendientes, sin declararlos verificados por el reinicio. PostgreSQL y Redis locales están saludables; `/health/ready` responde 200 antes de detener la API. Alcance: mismos servicios y configuración de desarrollo, sin reset de datos, cambios de autenticación, infraestructura ni despliegue.

Se reiniciarán únicamente el frontend web de 4200 y el backend de 5000, conservando la configuración efectiva del proceso previo y su enlace con el bridge local. La PWA de 8100 y los contenedores permanecen fuera del reinicio. La interfaz seguirá siendo la actual porque los planes todavía no se implementaron. Resultado posterior de arranque: pendiente de registrar.

## Implementación y prelaunch del worktree propio — 17-oct, 10:10 Guayaquil

Las secciones anteriores registran el estado documental inicial y quedan como historial. Ahora hay código de aplicación en `feature/242-rutas-interactivas`, base `8ca4e8b`, worktree hermano `ISTPETDEV-rutas`. Se recuperaron los cambios de esta tarea después de que otra sesión sincronizara el repositorio compartido; esa sesión continúa en su propia rama y no se modifica su checkout. No se ha creado un commit, PR ni despliegue.

Revisión previa al reinicio, con la skill local de prelaunch ya consultada y sus referencias pertinentes:

| Control | Estado y evidencia | Límite práctico |
| --- | --- | --- |
| Alcance | Desarrollo local; frontend 4200 y API 5000 desde el worktree de rutas | No es release ni demo pública |
| Base y migración | Configuración efectiva Development comprobada sin imprimir credenciales: PostgreSQL `localhost:15432`, base `istpetdev_dev_01`; migración aditiva `20261017143616_RoutePlanningDecisions` | Aplicación local pendiente del arranque; no se modifica RDS ni se resetean datos |
| Autorización y aislamiento | DemoCore ADR-21, sin RLS P2. Roles fijos, organización en consultas, aceptación restringida a planificación; prueba de conductor prohibido y transacciones concurrentes aprobadas | Sesión Cognito real y prueba manual por cuenta aún pendientes |
| Negocio/transacciones | 9 pruebas específicas aprobadas en este worktree: decisiones, esquema y seguridad de migración; aceptación de dos vehículos, replay, recarga, conflicto, cantidades obsoletas y rollback al publicar | Suite .NET completa en curso; no acredita n8n remoto |
| Frontend | 184/184 pruebas, 34 archivos; compilación de producción aprobada y lint sin errores | Advertencias de CSS 7.85 kB sobre umbral de aviso 4 kB, bajo límite 8 kB, y Leaflet CommonJS |
| Campos/contenido/SQL | DTOs acotados, snapshot calculado en servidor, EF parametrizado, interpolación Angular y escape previo del mapa; sin plantillas ejecutables ni nuevos uploads/QR | No cambian los controles de contenido de otros módulos |
| Datos clasificados | No se añaden credenciales ni nuevos datos personales; conductor se selecciona de cuentas activas de la organización | Cifrado por campo no ensayado; no se acredita por build |
| Dependencias | Sin cambios de lockfile ni dependencias nuevas; se reutiliza auditoría npm aprobada con 7 excepciones vigentes | Scan nuevo NuGet/contenedores pendiente; no se presenta restore como scan |
| Cabeceras/proxy | Dev server local; se conserva la configuración anterior y el enlace local con el bridge | Cabeceras finales de despliegue fuera de alcance |

Se autoriza el arranque local para verificar lo pendiente en este entorno. No se declara un lanzamiento expuesto seguro. El proceso de API recibirá `ApplyMigrations=true` solo en su entorno, conservando los demás parámetros del proceso previo.

## Revisión visual de la implementación clara

Aplicadas las skills `emil-design-eng` y `ponytail`; se reutilizan Leaflet, Lucide y controles nativos sin instalar dependencias. Revisión en Chromium con componentes reales y datos sintéticos, en un harness temporal fuera de la aplicación: 1500 × 1000 y 390 × 844. El harness no usa ni sustituye el login de producción.

| Before | After | Why |
| --- | --- | --- |
| Plan inicial nocturno y panel de seguimiento oscuro | Fondo claro, tarjetas blancas y seguimiento claro | Corrección explícita de Bryan |
| Mapa secundario y controles extensos antes de la ruta | Mapa amplio primero; planificación automática plegable | El recorrido concentra la atención |
| Recorridos estáticos sin secuencia por vehículo | Recogida → entrega → siguiente parada, con foco por vehículo | Explica la asignación de varias rutas |
| Animación sin controles de recorrido | Reproducción, pausa, reanudación, reinicio y pasos manuales | El usuario controla el análisis visual |
| Lista vertical de vehículos en móvil | Selector horizontal; documento y modal de 390 px sin desbordamiento | Deja más mapa visible |
| Refresco potencialmente interrumpiendo la animación | Se conserva la capa si el trazado no cambió | Evita perder el avance en autorefresco |

Prueba de navegador del jugador dentro del diálogo: selección por evento de teclado/programático comienza pausada en tramo 1; reproducir y pausar conserva tramo 1; reanudar alcanza tramo 2 y `finished=true`. Las capturas temporales están en `/tmp/istpetdev-rutas-light-{desktop,workbench,mobile}.png`. La selección manual autenticada con Cognito y una planificación operativa real siguen pendientes; estos datos sintéticos no acreditan una ejecución real del agente.

Resultado de arranque: frontend 4200 devuelve 200; API `/health/live` y `/health/ready` devuelven 200 (`Live` / `Healthy`); decisión sin token devuelve 401. Los procesos y binario corresponden al worktree `ISTPETDEV-rutas`. Consulta SQL local confirma `RoutePlanningDecision` y `20261017143616_RoutePlanningDecisions` en `__EFMigrationsHistory`. No se ejecutó reset de la base local ni despliegue externo.

Durante la regresión .NET completa, el primer intento terminó con el proceso de pruebas abortado (479 aprobadas y una prueba fallida). Esa prueba restauraba stock en una fila arbitraria y podía dejar sin stock la bodega de la ruta: se corrigió el fixture para restaurar todas sus filas, sin alterar el código productivo. Se repite la suite limitando los procesadores del proceso de pruebas a dos para reducir presión de memoria. El resultado final se registra al terminar.

Validación web final: **186/186 pruebas aprobadas, 34 archivos** (incluye refresco del mapa y selección de parada con numeración desde cero). `npm run build:web` y lint aprobados; CSS del workbench 7.88 kB bajo límite de 8 kB, con aviso sobre presupuesto recomendado de 4 kB; aviso CommonJS de Leaflet. `check-no-mock-login` aprobado. Bóveda: 0 errores y los 4 avisos previos de redacción.

Última comprobación móvil tras ajustar la altura automática: mapa 717.5 px, reproductor 697.5 px; el resumen comienza después del mapa, sin superposición. Ancho del documento/modal/viewport: 390/390/390 px. Se cerraron los procesos temporales del harness y Chromium de revisión; quedan activos únicamente los servicios solicitados y los servicios ajenos previos.

## Cierre de comprobaciones — 17-oct, 10:21 Guayaquil

- Suite .NET final: `DOTNET_PROCESSOR_COUNT=2 dotnet test tests/IstpetDev.Tests --filter 'Category=Integration|Category=Unit|Category=E2E'`: **741 aprobadas, 0 fallidas, 0 omitidas**, duración 8 min 4 s. Incluye las nuevas pruebas reales de PostgreSQL de aceptación exclusiva, varios vehículos, concurrencia, recuperación, stock obsoleto, rollback y publicación de conjunto. Log temporal `/tmp/istpetdev-rutas-own-full-backend-retry.log`.
- Suite web final: **186/186**, 34 archivos. Log `/tmp/istpetdev-rutas-final-web-tests.log`. Compilación y lint finales correctos en `/tmp/istpetdev-rutas-final-build.log`.
- La suite regenera archivos de evidencia de spec 006. Se restauraron únicamente esos archivos generados en este worktree; se conserva la evidencia de esta tarea.
- PID frontend Angular 1553985, cwd `ISTPETDEV-rutas/apps/web`; PID API 1552308, cwd `ISTPETDEV-rutas/src/Api`. Rama `feature/242-rutas-interactivas`, base `8ca4e8b` más los cambios locales documentados. `.gitignore` acepta también el enlace local de dependencias del worktree.
- Entorno disponible: web `http://localhost:4200/rutas`; API `http://localhost:5000/health/ready`. No se detuvo la otra sesión ni se cambió su rama para el reinicio final.
- Pendiente: recorrido manual completo con sesión Cognito real y datos operativos del usuario. La visualización del vídeo y las capturas son referencia/prueba visual, no evidencia de ejecución del agente ni de seguimiento GPS. No hubo despliegue externo.

## Prelaunch del ajuste de tarjetas y aprobación

Mismo worktree `feature/242-rutas-interactivas`, misma base `8ca4e8b`, servicios locales previamente verificados. Se aplica de nuevo la skill de prelaunch reutilizando los controles pertinentes y la evidencia del cierre anterior: backend 741/741, migración local aplicada, roles y transacciones sin cambios, lockfiles sin cambios y auditoría npm con excepciones vigentes. El ajuste afecta únicamente frontend y documentación; se repetirán pruebas web, build/lint y revisión de entrada vacía, aprobación y disposición móvil. No introduce campos de privilegio, SQL, uploads, QR, dependencias ni datos personales nuevos; RLS P2 no aplica a DemoCore. Cognito manual y cabeceras del proxy de un despliegue siguen pendientes y no se acreditan por este ajuste local.

## Resultado del ajuste de tarjetas, vehículos y aprobación

Se completó el plan y la ampliación recibida durante la implementación, en el mismo worktree propio y modo claro.

| Before | After | Why |
| --- | --- | --- |
| Siete rutas apiladas en la columna lateral | Una fila horizontal desplazable y mapa debajo a todo el ancho | El mapa ocupa el área principal |
| Sin etiquetas en las rutas registradas y una sola razón por alternativa | Badges de menor/mayor costo, menor distancia y rapidez simultáneos; empates/datos ausentes explícitos | Explicar las diferencias reales |
| Botón pequeño debajo del mapa | Acción de 56 px encima del mapa y botón de opción en el pie del comparador | Aprobación identificable |
| Comparador vacío por «Abrir último cálculo» sin identificador | Entrada condicionada a un cálculo existente y selección de pedidos como salida útil | Evitar consultas y acciones sin efecto |
| Selector de vehículos debajo del mapa | Selector y conductor por vehículo encima del mapa | Asociar conductor y recorrido sin perder contexto |
| Seleccionar de nuevo el mismo plan no reiniciaba; una revisión posterior podía ocultar la elegida | Clave de reproducción por selección y revisión seleccionada incluida en el mapa | Reproducir todo el plan elegido |
| Cambios tardíos de geometría podían cortar el avance | Inicio al recibir datos, continuidad entre tramos y regreso final del contrato de rutas registradas | No quedarse en el primer tramo |
| Al guardar una elección seguían visibles otras propuestas históricas | Área de trabajo filtrada por IDs confirmados, conservando todos los vehículos elegidos | Mostrar la propuesta aprobada sin cancelar registros ajenos |

Navegador con componentes reales y datos sintéticos, fuera de `apps`: siete tarjetas en una fila; mapa 1396 px de ancho y 814 px de alto a 1500 × 1100; botón de aprobación de 56 px. A 390 × 844, viewport y documento mantienen 390 px y las tarjetas siguen en una fila. El comparador muestra menor costo, menor distancia, mayor costo y más rápida según las métricas del fixture.

Comprobación extendida: selector de VEH-01 (6 paradas) y VEH-02 (2 paradas) encima del mapa; asignaciones distintas conservadas al alternarlos; ruta registrada alcanza tramo 6/6 y regreso a bodega; propuesta alcanza tramo 5/5 y `finished=true`; volver a seleccionar reinicia. Tras la respuesta simulada de aceptación, quedan cero tarjetas competidoras en el diálogo y dos rutas/vehículos elegidos en el área principal, mientras los siete registros originales siguen consultables. Es evidencia visual sintética, no una planificación operativa con Cognito real. Resultados temporales: `/tmp/istpetdev-rutas-complete-review.json`; capturas `istpetdev-rutas-vehiculos-arriba.png`, `istpetdev-rutas-propuesta-aprobada.png` y `istpetdev-rutas-vehiculos-mobile.png` en `/tmp`.

Backend sin cambios en este ajuste: se reutiliza la suite previa 741/741 y la migración local verificada. Compilación de producción/lint aprobados; avisos CSS 5.90 kB (página) y 7.82 kB (comparador), ambos bajo el límite de 8 kB; aviso CommonJS de Leaflet. No se instalaron dependencias ni se modificaron lockfiles.

Cierre final del ajuste: **199/199 pruebas web aprobadas, 34 archivos**; log `/tmp/istpetdev-rutas-complete-final-tests.log`. Incluye el reproductor con mapa real recorriendo seis tramos, geometría tardía y reinicio; selección de revisión anterior; conductor por vehículo; ninguna ocultación antes de confirmar publicación; conservación de todos los IDs aceptados y actualización del detalle a la ruta aprobada. Compilación y lint finales aprobados en `/tmp/istpetdev-rutas-complete-final-build.log`. La segunda pasada de navegador confirma `selectedDetail=selectedId=route-0` después de aceptar, corrigiendo la referencia al detalle anterior.

Bóveda: 0 errores, 4 avisos previos; diff sin errores de espacios; comprobación de ausencia de login simulado aprobada. Web `/rutas` 200, API live/ready 200 (`Live`/`Healthy`); procesos Angular 1553985 y API 1552308 con cwd del worktree propio. Frontend actualizado mediante el dev server activo; backend sin cambios ni reinicio adicional. Harness/Chromium temporales cerrados. No se tocó el checkout de la otra sesión ni se aplicó otra migración. Sesión Cognito operativa y despliegue externo no se acreditan.


## Prelaunch — descarte persistente de propuestas históricas

Mismo worktree propio `feature/242-rutas-interactivas`, base `8ca4e8b`, PostgreSQL exclusivamente local `localhost:15432/istpetdev_dev_01`. Se aplica la skill `istpetdev-prelaunch` y sus referencias de autorización DemoCore, esquema, RBAC y validación. Esta corrección reemplaza el filtro visual anterior: la API persiste `Discarded` y excluye ese estado en el listado normal. Conserva historial publicado y todos los vehículos del cálculo elegido. Lotes nuevos se identifican mediante `proposalBatchId`; lotes históricos mediante la respuesta persistida de `CreateRoutePlans`, sin agrupar por coincidencia temporal. Los borradores competidores son revisiones de los mismos códigos canónicos; no se cancelan rutas publicadas ni se descartan borradores con efectos operativos.

Controles afectados: transacción/lock por organización e idempotencia existentes; consultas EF parametrizadas y limitadas a organización; no cambia autorización ni se admiten campos de servidor desde DTO. Pruebas PostgreSQL/API afectadas **12/12** y web **199/199**; build de producción y lint aprobados. Se amplía además la prueba existente del publicador real para comprobar el descarte tras publicar exitosamente. Sin migración nueva, paquetes ni cambios de lockfile. Reutilizados los controles previos de roles/asignación y auditoría npm con siete excepciones vigentes; RLS/plantillas administrables P2 no aplican a DemoCore. Scan nuevo NuGet/imagen, Cognito manual y cabeceras de despliegue permanecen pendientes; no se acredita un release expuesto.

Antes del reinicio se prevé reparar solo las revisiones competidoras locales ya existentes mediante el mismo servicio transaccional, con respaldo privado de los registros y validación de ambos IDs del cálculo aceptado. No se hace reset, migración ni operación cloud. Se conserva el checkout de la otra sesión.


### Resultado del descarte persistente — 17-oct, 11:04 Guayaquil

- Reparación local ejecutada mediante `RouteProposalAcceptance` dentro de transacción y con respaldo privado previo: **8 borradores competidores pasan a `Discarded`**. Se conservan las dos rutas de la propuesta elegida (VEH-01 publicada y VEH-02 pendiente), además de una ruta publicada anterior. Resultado real: 11 registros físicos, 8 descartados, 3 en la bandeja normal y 2 publicados. No se borran entregas ni reservas, ni se hace reset.
- La pertenencia al lote aceptado se comprobó con el recibo persistido de `CreateRoutePlans` (`1a629fa1-089d-6f6f-9052-881dda03fc32`). No se agruparon registros por fecha de creación. Respaldo privado local `/tmp/istpetdev-rutas-before-persistent-discard.private.json`, permisos 0600; herramienta temporal fuera del repositorio.
- Pruebas PostgreSQL/API afectadas **12/12**; se corrigió la consulta inicial que intentaba `LIKE` sobre `jsonb`. Proyección limitada a organización y análisis JSON en aplicación para el volumen de DemoCore, sin añadir proveedor/dependencias a Application. Reejecución final del publicador **3/3** tras ampliar su caso de éxito: comprueba publicación real, descarte persistido, FEFO y replay sin duplicar reservas. Logs `/tmp/istpetdev-persistent-discard-final-tests.log` y `/tmp/istpetdev-discard-publisher-tests.log`.
- Web **199/199**, 34 archivos; aprobación conserva ambas rutas, no cambia antes del éxito del servidor y recarga usa la lista persistida. Sin `approvedRouteIds`, filtro recordado ni botón de restauración. Build/lint aprobados; avisos CSS 5.80/7.82 kB por encima del presupuesto recomendado de 4 kB y debajo del límite de 8 kB; aviso CommonJS de Leaflet.
- API reiniciada desde el worktree propio con binario actualizado, sin ejecutar migraciones (`ApplyMigrations=false`): padre 1653795, binario 1653828, cwd `ISTPETDEV-rutas/src/Api`. Frontend Angular 1553985 conserva el dev server con cambios actualizados. Web `/rutas` 200, API live/ready 200 y rutas sin token 401. No se interrumpió la otra sesión.
- Bóveda 0 errores/4 avisos previos; diff sin errores de espacios. Se distingue la reparación SQL local real de las pruebas API autenticadas con identidad de prueba en Testcontainers. El flujo manual de Cognito real y un despliegue externo siguen sin acreditarse.


## Integración a develop — PR #324

Integrado `origin/develop` hasta `78c87a1`, sin conflictos textuales; se conservan alertas automáticas de inventario. Suite web integrada **207/207**, 36 archivos, build/lint aprobados. En la primera regresión backend apareció un 409 al revertir una ruta: el plan restaurado heredaba el identificador del cálculo original y encontraba la revisión sustituida. Se corrige en el publicador compartido limitando la aceptación de propuestas iniciales a `SupersedesRoutePlanId == null`; los descartes y la resolución de lotes también excluyen revisiones operativas. La suite detectó el caso real, no se relaja la expectativa del E2E. Se comprueba además que la publicación intermedia es exitosa y que un borrador de replanificación no se descarta como propuesta competidora. Se interrumpe la suite del commit anterior para verificar la corrección. Resultados posteriores y merge se consultan en [PR #324](https://github.com/bryancito1090/istpetdev-platform/pull/324); no se acredita despliegue con el merge.


### Prelaunch de la ampliación del paso de abastecimiento

Se reutiliza la revisión local de prelaunch para el mismo worktree/entorno. El cambio del paso 2 reutiliza `RouteJourneyPlayer`, geometría y abastecimiento de API; no cambia permisos, identidad, dependencias ni configuración cloud. Compilación/lint aprobados, revisión de aceptación/replanificación/reversión **28/28**. Se verificará el componente real en un harness sintético temporal fuera de `apps`, enlazado solo a loopback; no sustituye ni omite el login de la aplicación. Se conservan los pendientes de Cognito manual, scans nuevos y despliegue ya documentados. No se acredita un óptimo global: la elección inicial es una recomendación viable proporcionada por el motor.
