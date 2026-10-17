# Issue #235 — indicadores de la operación

Plan: verificar contratos existentes, implementar seis filas en Gerencia, probar ausencia/error/cálculos, capturar escritorio y móvil, publicar PR a develop y comprobar CI/conflictos antes de integrar.

Base: `8caeac1` de develop. Rama: `feature/235-indicadores-operacion`. Código y validación en worktree separado del directorio utilizado por Antigravity. Sin cambios de backend, esquema, dependencias o despliegue.

## Contratos y alcance

- Costos/km: última comparación completada de las 50 consultadas; suma de `costo_transporte` dividida por suma de `km_total`, solo política `istpetdev-v1`. El DTO de métricas no identifica la corrida, por lo que se agregan todas las corridas de esa política y se declaran comparación, dataset, semillas, escenarios y cantidad de corridas. Se exige una métrica válida por corrida, moneda USD y km positivos. No se divide una mediana por otra ni se emparejan filas por posición. Se conserva el costo que entrega la simulación; no se redefine #240/#204.
- Faltantes: **sin dato**. `MetricCalculator.CalculateStockoutComparison` existe pero el worker no publica sus resultados en las métricas de comparación. No se infiere desde entregas ni quiebres totales.
- Servicio: **sin dato**. `delivery-orders` tiene límite y no ofrece los totales completos comparables de pedidos entregados y planificados. No se usa `puntualidad`, que tampoco corresponde a esta razón.
- Tiempo: suma de duración estimada de las rutas `Published` consultadas (hasta 50). Una duración ausente/cero se considera no disponible porque el backend y el adaptador convierten nulos en cero. No se muestra una suma parcial si alguna duración falta ni se afirma tiempo de viaje medido.
- Mermas: cantidad de **líneas**, no cantidades físicas ni valor monetario, con `quantityRejected > 0` y `rejectionReason === 'Daño'`. Hasta 100 recepciones con novedades; la tarjeta declara el alcance y advierte conteo parcial cuando `total > items.length`. Una consulta vacía exitosa sí permite cero líneas observadas.
- Exactitud: **sin dato**. `GET /api/v1/receipts/exceptions` filtra las recepciones con rechazo/ajuste antes de contar: su `total` no es el total de recepciones y no permite obtener las recepciones limpias. Requiere disponibilidad futura del denominador completo en backend; no se agrega endpoint.

La etiqueta de Tiempo es «rutas publicadas» para identificar su fuente real. Las otras etiquetas son «simulación», «recepciones» o «sin dato». Las seis filas permanecen durante carga, error y ausencia. Auditor y Planificador no ven ni consultan esta tarjeta de Gerencia.

## Prelaunch antes del arranque local

Aplicada la skill local `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` del workspace principal y sus referencias de seguridad, frontend, revisión, RBAC, validación y runbook DemoCore.

| Control | Estado y evidencia / límite |
|---|---|
| Alcance | Entorno local aislado, solo loopback; captura del componente Angular con sesión de prueba inyectada y respuestas HTTP sintéticas. No se inicia API ni se conecta a bases compartidas o AWS. No acredita login Cognito ni operación real. |
| Roles | Verificado en código: tarjeta y llamadas exclusivas de Gerencia; endpoints existentes mantienen autorización. Pruebas de componente para Auditor y Planificador. Autorización real de API no reensayada porque no cambia. |
| RLS / DB / VerifyFull | No aplica al cambio de presentación; DemoCore sin RLS según ADR-21/runbook. Ninguna migración, conexión o grant modificado. Runtime RDS pendiente de #198, fuera del alcance. |
| Campos, plantillas, SQL, cifrado, QR, archivos | No aplica: solo lecturas de APIs ya existentes; no se agregan entradas, escrituras ni acceso a evidencias personales. Texto interpolado por Angular, sin HTML dinámico. |
| Dependencias | `npm run audit:check` superado: 7 avisos cubiertos por excepciones aprobadas vigentes. Lockfile sin modificaciones. No se escanean imágenes ni se valida un release en este trabajo. |
| Cabeceras/proxy y límites de abuso | No aplica a servidor estático local de captura. No se certifican cabeceras productivas, proxy, disponibilidad ni capacidad. |
| Resultado previo | Puede arrancar el entorno local aislado para la captura. Este registro no declara listo un despliegue ni modifica #198. |

## Validación

Resultados observados el 16-oct-2026, zona America/Guayaquil (17-oct UTC):

- `npm run test --workspace=apps/web`: **24 archivos y 144 pruebas en verde**, incluido el cálculo de #235 y los componentes de Gerencia/Auditor/Planificador. Cubre falta de métricas, nulos, km cero, unidades, estado incompleto, ceros reales, rechazo por Daño, conteo parcial, duración ausente y errores independientes de cada fuente.
- `npm run check`: **verde**, incluidos lint FSD, ausencia de login simulado, compilación .NET, shared-core, web, móvil y validación de artefactos `/campo/`. Avisos existentes de compilación y de deprecación del linter no bloquean el comando.
- `node_modules/.bin/tsc -p apps/web/tsconfig.spec.json --noEmit --rootDir .`: **verde**. El `tsc` directo sin ese override produce TS6059 por la configuración heredada del monorepo; no se cambió la configuración para este issue.
- `npm run audit:check`: **verde**, 7 avisos con excepciones aprobadas vigentes; no se cambiaron dependencias.
- `python docs/boveda/.github/scripts/check_vault.py`: **0 errores**, 4 avisos preexistentes.
- Playwright/Chromium: **verde** a 1366 y 390 px. Seis filas en orden, valores sintéticos esperados (600 USD ÷ 200 km = 3 USD/km, 150 minutos estimados y 2 líneas por Daño), error de simulación que conserva recepciones, reintento, respuesta vacía, ninguna escritura HTTP, ningún error de ejecución del navegador y sin desbordamiento horizontal en móvil. Resultado en [browser-validation.json](browser-validation.json).
- [Captura de escritorio](desktop.png) y [captura móvil](mobile.png): render del componente real `InicioPage` con providers de sesión de prueba y respuestas HTTP interceptadas. No corresponden a RDS, producción ni a un ensayo logístico real. El entrypoint temporal usado para la captura fue retirado antes del commit; no existe acceso alternativo de sesión en la aplicación entregada.
- `git diff --check`: sin errores. Cambios de documentación llegados a develop durante el trabajo se incorporan por rebase antes de publicar el PR.

## Para el pitch (#200)

Las seis filas conectan con la lámina de la charla. Se puede explicar y rastrear Costos/km, Tiempo y Mermas con las fuentes disponibles; Faltantes, Servicio y Exactitud permanecen explícitamente sin dato por las limitaciones anteriores. Estos resultados prueban la presentación y los cálculos con respuestas sintéticas, no beneficios reales de la operación ni el despliegue #198.
