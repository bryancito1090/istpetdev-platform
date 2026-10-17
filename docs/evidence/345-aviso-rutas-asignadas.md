# #345 — Avisar antes de volver a planificar una entrega

Fecha: 2026-10-17. Base: develop `968edc5`; worktree aislado `ISTPETDEV-rutas`.
Primero se completó #330 en desarrollo y producción; resultados efectivos en
`docs/evidence/330-novedades-campo.md`.

## Plan e implementación

1. Reconocer asignaciones por las cantidades y el estado de cada pedido,
   usando su ID. Compartir destino con una entrega previa no acredita duplicado.
2. Mostrar el aviso en la selección, detener el cálculo y ofrecer revisar rutas
   o retirar los pedidos. Si hay remanentes, exigir «Calcular solo lo pendiente».
3. Validar otra vez en el servidor antes de abastecer o preparar una decisión;
   responder 409 con `code=orders_already_assigned` y `assignedOrders`.
4. Incorporar `planRemainingOnly`, opcional y false por defecto, a suggest y
   prepare-decision. Solo habilita calcular saldos restantes; no evita la
   comprobación de cantidades cubiertas ni la validación al aceptar.
5. Mantener la elección hasta el comparador y reiniciarla si cambia la selección.
   Probar asignación total/parcial, datos obsoletos y reposición nueva.

## Revisión de interfaz (Emil Kowalski)

| Before | After | Why |
| --- | --- | --- |
| El pedido parcialmente asignado parece disponible para calcular otra vez. | Etiqueta de asignación y aviso ámbar junto a la selección. | Explicar el estado antes de actuar. |
| Calcular inicia propuestas sin decisión sobre el saldo ya asignado. | Cálculo detenido, acción explícita para remanentes y botón para revisar rutas. | La acción describe su efecto y evita duplicados accidentales. |
| Error tardío puede enviar al paso de comparación vacío. | Conflicto de asignación conserva el paso 1 y muestra los pedidos afectados. | Permite corregir la selección en contexto. |

Modo claro, Lucide, acciones de 44px, contraste de aviso y foco visible,
disposición flexible en móvil. El aviso aparece de inmediato, sin animaciones
en una interacción frecuente; se conserva reduced-motion existente.

## Prelaunch local

Aplicada `istpetdev-prelaunch` y referencias de seguridad, modelo DemoCore,
roles fijos y validación canónica. Alcance: desarrollo local y pruebas aisladas,
sin despliegue productivo ni modificaciones de esquema.

| Control | Estado / evidencia |
| --- | --- |
| Acceso y organización | Se conservan roles Planning/Automatizacion de suggest y Planning de decisiones. Lecturas nuevas filtradas por DemoOrganization; no se modifica autenticación ni grants. |
| Cantidades | El flag de remanentes no autoriza duplicar cantidades: abastecimiento y confirmación conservan sus saldos/validaciones. Pedidos cubiertos responden 409 aun con flag true. |
| Entrada y errores | Consultas EF parametrizadas; sin SQL manual, plantillas o contenido ejecutable nuevos. El aviso muestra texto escapado por Angular. |
| QR / datos clasificados | No se cambian capturas, tokens ni datos sensibles. Los resultados de #330 contienen solo recuentos públicos; secretos y respaldos permanecen privados. |
| Dependencias | Sin dependencias nuevas. No se afirma un escaneo nuevo ni seguridad de una imagen de producción. |
| Pruebas | Primera ejecución web: cuatro fallos esperados y un caso nuevo correcto antes del cambio. Primera ejecución backend interrumpida por caída del test host; no acredita resultado. Repetición: 7/7 API y 89/89 pruebas de Rutas. Build web y lint aprobados; CI pendiente. |
| Runtime | Frontend propio reiniciado y responde 200. Backend propio reiniciado y detecta una migración local pendiente: `20261017152159_AddMetadataJsonCatalog`, ya publicada en develop. Se respaldará de nuevo antes de aplicarla solo a desarrollo local; sin cambio productivo de esquema. Sesión privada de planificador vencida: las pruebas HTTP usan JWT firmados del harness y PostgreSQL descartable; no suplantan una sesión real. |

## Resultados

7/7 API, una regresión adicional de abastecimiento mixto (retirar el pedido
cubierto y elegir el remanente del otro) y 89/89 pruebas de Rutas aprobadas. Build web y lint correctos; avisos de
presupuesto CSS de los dos componentes de rutas y CommonJS de Leaflet (sin
incrementar límites para ocultarlos). Bóveda: cero errores, cuatro avisos
preexistentes.

Runtime local: frontend 200 en `http://localhost:4200`, API readiness Healthy/200
en `http://localhost:5000/health/ready` y rutas sin sesión 401 tras reiniciar
exclusivamente procesos del propio worktree. Se aplicó únicamente la migración
publicada de metadatos que faltaba en desarrollo, sin generar ni reescribir
migraciones. Respaldo actualizado previo: 424.185 bytes, 40 tablas, catálogo
`pg_restore --list` correcto, SHA256
`b3d85683849e6b5c13df4f2c79224704366e5857249108be719bbecebb07b6da`.
No se cambió el esquema ni se desplegó la aplicación en producción.
CI final sobre `665f8236842bee3f05c6bb6f75a140551580ff49`, run `39055486451`:
377 integraciones + 361 unitarias + 25 E2E de backend (763); 24 shared-core,
227 web y 147 PWA (398 frontend). Todos los checks SUCCESS, incluido ensayo de
respaldo/restauración y política de ramas; MERGEABLE antes de integrar.
PR #349 integrado a develop el 2026-10-17 17:56:42 UTC, merge
`8b9b1effa672ae95ba5d205d2c60a8b1de5dd404`. Issue #345 CLOSED después del merge,
sin comentarios adicionales. La documentación final no modifica el software
verificado en esa revisión.

Validación visual adicional con Chromium, componente real y ApiService sintético
en una vista aislada sin sesión real: escritorio 1440×1000 y móvil 390×844,
sin errores JavaScript ni desbordamiento horizontal del aviso. En ambos tamaños
se verificó cero llamadas automáticas, bloqueo del botón principal, transmisión
de `planRemainingOnly=true` únicamente tras la acción explícita y ausencia de
esa acción con cantidades cubiertas. Capturas inspeccionadas; resultados en
`345-asignaciones-browser.json`. No representa una prueba autenticada de la
aplicación productiva ni del servicio de mapas.

La política efectiva de este repositorio admite `fix/*` y rechazó `bugfix/*`.
Se conservó el mismo código en `fix/345-aviso-rutas-asignadas`, cerrando el PR
redundante #348 sin comentarios; PR vigente #349, sin relajar la política.
