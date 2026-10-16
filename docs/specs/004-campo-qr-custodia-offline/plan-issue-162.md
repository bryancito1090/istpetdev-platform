# Plan de resolución del issue #162

Fecha: 2026-10-15. Destino: `develop`. Rama: `fix/162-estados-bulto-custodia`.

## Resultado requerido

Cada rol actúa en su turno y la traza pública muestra Despachada → En ruta → Entregada → Recibida. Se implementa el modelo completo solicitado por el issue, no solo la barrera de recepción duplicada. El conductor recoge y entrega; el receptor confirma cantidades y registra el único recibo final. La autorización procede de la cuenta autenticada y de la asignación o del ámbito del recurso, nunca del cuerpo enviado por el cliente.

## Contrato que se debe documentar antes de implementar

| Paso | Entrega | Bulto | Actor | Efecto |
|---|---|---|---|---|
| Preparación | Draft | Sealed | Bodega | Empaque completo y QR; aún no habilita recogida |
| Despacho | Dispatched | Dispatched | Bodega | Despacho y ledger existentes, una sola vez |
| Recogida | InTransit | InTransit | Conductor asignado | Evento de custodia, sin repetir movimientos del despacho |
| Entrega física | Delivered | Delivered | Conductor asignado | Evento de custodia; aún sin recibo final ni segundo movimiento |
| Recepción | Received / Partial | Received / Partial | Receptor autorizado | Un recibo y un juego de movimientos hacia destino; rechazado permanece bajo custodia del conductor |

Validar esta tabla contra el modelo actual y registrar con precisión cuándo cambia la ubicación contable frente a la custodia física. Mantener explícita la diferencia entre Draft y un bulto preparado; no crear migraciones si los campos string existentes son suficientes. Documentar la compatibilidad de los estados históricos que usaban Delivered para una recepción final: un recibo existente impide repetir efectos, aunque el estado sea legado.

## Implementación delegada a OpenCode

1. Leer README, AGENTS local, bóveda, hechos canónicos, ADR-25, contratos de campo, flujo móvil y las notas del spec 004. Revisar todos los llamadores de `CreateReceiptCoreAsync`, incluidos demo advance, PWA, mocks, consumidores web y pruebas. Consultar #160 y el PR #159: reutilizar asignaciones actuales y registrar dependencias pendientes sin incorporar cambios ajenos.
2. Actualizar la bóveda con máquina de estados, responsables, semántica del stock, endpoints y errores. Mantener spec, plan, task, decisiones y control documental coherentes; ajustar conteos con el verificador. Registrar pruebas locales, ejecución real y despliegue como categorías distintas.
3. Añadir comandos explícitos de recogida y entrega, autenticados y limitados al conductor asignado. Validar estado de entrega y de TODOS sus bultos, existencia de unidades/líneas y versión cuando aplique. No permitir preparación o despacho retroactivo después de avanzar. Rechazar saltos y repeticiones con 409/422 y códigos documentados. Mantener las transiciones y los eventos atribuibles atómicos.
4. Blindar la recepción compartida antes de cualquier efecto: Draft → NOT_DISPATCHED; Dispatched/InTransit → NOT_DELIVERED; un recibo final previo o estado terminal → ALREADY_RECEIVED. La respuesta idempotente de la misma operación válida debe recuperarse incluso después de avanzar el estado. Evitar que dos cuentas u operationId concurrentes generen dos recibos o doble stock mediante el mecanismo transaccional/bloqueo real existente. El conductor ya no debe confirmar stock en destino mediante el endpoint anterior; definir y probar su respuesta de negocio o autorización. Gerencia/demo tampoco deben abrir un salto silencioso de custodia.
5. PWA conductor: mostrar Esperando a Bodega cuando falten bultos o despacho; ofrecer Recoger y luego Entregar dentro del contexto de parada. Proteger también la entrada directa a pantallas y el envío. PWA receptor: Aún en bodega / El Conductor aún no entrega este bulto / Recibir / Ya recibido según estado. Conservar outbox durable, idempotencia y tratamiento de 409 RequiresReview; cualquier comando online u offline debe informar fielmente si quedó pendiente o fue aceptado. No presentar una captura local como confirmación del servidor.
6. Actualizar contratos C#/TypeScript, catálogo de errores, DTOs, traza pública y consumidores afectados. La traza no expone identidad privada, cantidades ni tokens. Ajustar demo y mocks para recorrer los comandos reales sin eludir validaciones.
7. Pruebas de integración HTTP con PostgreSQL efímero: recorrido por roles completo; cada transición inválida; receptor antes de entrega; conductor/rol/asignación indebidos; entrega sin bulto; doble recepción entre actores y con claves diferentes; concurrencia; replay con mismo operationId sin efectos repetidos; parciales con stock rechazado en vehículo; estado legado con recibo ya existente. Verificar cantidades y número de recibos, movimientos y eventos. Añadir pruebas móviles relevantes de visibilidad, mensajes y outbox, y ejecutar navegador donde esté disponible.
8. Ejecutar los checks del repositorio: `dotnet test`, `npm run check`, pruebas frontend pertinentes, `npm run audit:check`, verificador de bóveda y política de ramas. Aplicar prelaunch local antes del runtime de prueba y registrar limitaciones reales. No usar ni mutar base compartida, producción, cloud, secretos ni workflows externos.
9. Entregar código y documentación completos en esta rama, con commits Conventional Commits claros; guardar informe de comandos, resultados, SHA y pendientes en `docs/specs/004-campo-qr-custodia-offline/evidencia-issue-162.md`. No abrir PR, mergear, cerrar issue ni enviar mensajes al equipo: el coordinador se encarga de revisión e integración.

## Revisión e integración a cargo del coordinador

- Revisar diff, pruebas y documentación; devolver a OpenCode cualquier falta antes de integrar.
- Comprobar evidencia de los criterios y dependencia #160. No inventar un ensayo físico ni declarar despliegue por integrar en develop.
- Actualizar desde origin/develop y resolver conflictos preservando cambios del equipo. Repetir verificaciones afectadas por la integración.
- Publicar la rama y abrir PR hacia develop con descripción revisable, evidencia y `Closes #162`. Aplicar metadatos del repositorio y solicitar revisión disponible.
- Exigir checks satisfactorios y mergeability comprobada sobre el SHA final; merge mediante PR, nunca push directo a ramas permanentes.
- Cerrar #162 únicamente tras comprobar el merge y el cumplimiento de sus criterios; dejar registro de alcance local y de cualquier validación externa aún pendiente.

## Estado

- [x] Investigación inicial y plan preparados.
- [x] Delegación aceptada por OpenCode.
- [x] Contrato y documentación actualizados (bóveda, task 004 y este plan).
- [x] Backend y PWA implementados (commits `2f4953f` y `0c7c431`).
- [x] Pruebas locales y verificación de repositorio completadas; revisión del coordinador pendiente.
- [ ] PR sin conflictos y checks aprobados.
- [ ] Merge a develop e issue cerrado.

## Resultado de la implementación — 15 de octubre de 2026

La cadena `Sealed → Dispatched → InTransit → Delivered → Received/Partial` quedó implementada con `pickup`/`deliver` del conductor, recepción blindada del Receptor, outbox durable para los comandos y traza pública con hitos reales. La evidencia de comandos, resultados y límites está en [evidencia-issue-162.md](evidencia-issue-162.md); la bóveda quedó sincronizada en `Contratos API y eventos`, `Movil offline y sincronizacion`, `Usuarios y flujos` y `Hechos canonicos`.

Sin migración de estados (los campos string bastan); la única migración, `CustodyChainStates`, corrige el índice único de `CustodyEvent` para operaciones con varios bultos. No se abrió PR ni se cerró el issue: quedan a cargo del coordinador.

