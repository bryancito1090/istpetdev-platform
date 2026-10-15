---
id: 007
titulo: Inventario cotidiano y conciliación para un piloto
estado: borrador
creado: 2026-10-13
actualizado: 2026-10-13
aprobado_por: PENDIENTE
depende_de: [001, 002, 004]
boveda: ["02 Producto/Usuarios y flujos", "02 Producto/Requisitos y aceptacion", "03 Arquitectura/Contratos API y eventos", "04 Datos y algoritmos/Inventario y prediccion", "06 Equipo/Continuidad del piloto y specs 007 a 012"]
---

# 007 — Inventario cotidiano y conciliación para un piloto

> Primera entrega operativa posterior al núcleo de demo. Una organización, una bodega y puntos/SKU acordados. El [plan](plan.md), las [tareas](task.md) y las [pruebas E2E](test-e2e.md) desarrollan este borrador con reglas propuestas; su redacción no acredita aprobación, implementación ni pase técnico del piloto.

## Contexto

El núcleo 001 importa saldos y consumos históricos; 002 calcula riesgo con `StockBalance` y `Consumption`; 004 define los movimientos de despacho y recepción. En el código revisado, `CanonicalWriter` es el único escritor encontrado de `Consumption` y de movimientos de inventario del importador. `InventoryEntities.cs` ya contiene `InventoryMovement`, `StockBalance` y `Consumption`, pero no existe aún el flujo operativo completo para registrar consumo cotidiano por lote, consultar el saldo conciliado y ver su efecto sobre el riesgo.

La [continuidad del piloto](../../boveda/06%20Equipo/Continuidad%20del%20piloto%20y%20specs%20007%20a%20012.md) propone 007 después del pase técnico de 001–006. El [análisis de cobertura](../../boveda/06%20Equipo/Analisis%20de%20cobertura%20de%20specs.md) identifica además que RF-15, anomalía y merma investigada, no tiene dueño completo en 001–006. Este spec lo asume para el piloto y deja explícita la decisión sobre su prioridad de demo.

## Objetivo y aporte

Permitir que un punto registre consumo real y que el sistema conserve una relación auditable entre operación, movimiento, saldo y riesgo. Con esto el riesgo deja de depender exclusivamente de un archivo importado: una organización puede usar el producto día a día y comprobar dónde se perdió o corrigió stock. El valor de negocio se evalúa en 009 con datos reales; este spec no promete reducción de quiebres por sí solo.

| Resultado | Evidencia visible |
|---|---|
| Saldo confiable | Consulta por ubicación, SKU y lote que explica saldo inicial, movimientos y saldo actual. |
| Detección útil | Un consumo aceptado modifica el saldo y el siguiente cálculo de cobertura sin duplicar demanda histórica. |
| Operación segura | Reintentos, concurrencia, permisos y correcciones conservan una historia verificable. |

## Alcance por entregas

**MVP de 007:** registrar consumo por lote en un punto autorizado, consultar movimientos/saldo y recalcular riesgo. **Ampliación de 007:** entradas físicas de bodega, demanda solicitada no atendida, solicitudes de reposición, ajustes justificados e investigación de anomalías. Cada ampliación se activa con datos y reglas acordados; no bloquea la prueba del MVP. La recepción y devolución de una entrega siguen siendo responsabilidad de 004, aunque 007 las muestre en el historial.

### Precisiones para planificar (propuestas del 13-oct)

- **Corrección del MVP:** anular completamente un consumo propio mediante otro registro compensatorio; registrar una cantidad correcta exige una nueva operación. Los ajustes por conteo físico y las mermas pertenecen a la ampliación. Nunca borrar el consumo original ni convertir una corrección en entrada física de bodega.
- **Captura:** Receptor en PWA, lote conocido y unidad base del SKU; no imputar FEFO automáticamente. El consumo puede guardarse offline con estado pendiente. Bodega registra entradas físicas en PWA; Planificador/Gerencia consultan y revisan excepciones en Web. Son capacidades propuestas para el piloto, no una modificación implícita de los roles de la demo.
- **Demanda:** sumar consumos operativos netos por punto/SKU/día, sin contar varias veces ese día en el denominador. Un cierre diario explícito acredita que el día está completo, incluido un cero observado. Al cerrar el día operativo, este reemplaza para el cálculo la aportación histórica de ese mismo día; la fila importada permanece intacta. Los totales históricos de varios días conservan el prorrateo declarado de 002 en los días no reemplazados. Un día operativo abierto no se interpreta como demanda completa y se muestra como dato parcial; su salida física sí afecta el saldo inmediatamente. Captura tardía o corrección reabre el día y exige nuevo cierre.
- **Frescura:** la hora de aceptación del movimiento no demuestra la fecha de un conteo físico. Mostrar por separado corte de inventario, última operación y último día de consumo cerrado. Una reimportación de stock sobre un lote/ubicación con operaciones de piloto se rechaza en el MVP y se dirige a conciliación; no basta con enviar una fecha más reciente.
- **Idempotencia y sesión:** conservar claves de operaciones aceptadas mientras existan sus movimientos; no depurarlas durante el piloto. Revalidar cuenta y asignación antes de replay/sincronización. El outbox queda separado por identidad y organización y no guarda tokens. Si cambia contenido de una operación rechazada, se crea otra clave con referencia local a la anterior.
- **Entregas:** el MVP incluye H1/H2, corrección, cierre diario y offline; la ampliación H3/FR-08/09 se planifica con tareas propias. Cerrar solo el MVP no cierra todo 007 ni acredita RF-15/V-19.

## Historias de usuario

### H1 — Registrar consumo cotidiano (P1 del piloto)

**Como** Receptor autorizado en un punto, **quiero** registrar cuánto producto se usó de cada lote, **para** que el saldo y el riesgo reflejen la operación real.

**Escenarios de aceptación:**

1. Dado un lote utilizable con saldo suficiente en el punto asignado, cuando registro cantidad positiva en unidad base y una clave de operación, entonces se crean un consumo operativo y un movimiento de salida vinculados; el saldo disminuye una sola vez en la misma transacción.
2. Dada la misma clave y el mismo contenido, cuando reintento tras perder la respuesta, entonces recibo el resultado original sin segundo movimiento; la misma clave con contenido distinto produce conflicto.
3. Dado saldo insuficiente, lote bloqueado/vencido, punto ajeno o dos consumos concurrentes que exceden saldo, cuando se intenta registrar, entonces no queda saldo negativo ni escritura parcial y se informa la causa permitida.
4. Dado un consumo histórico importado para el mismo punto/SKU/día, cuando registro un consumo operativo, entonces el histórico no se reemplaza y el cálculo de demanda cuenta cada hecho según una regla explícita, sin duplicación por fuente.

### H2 — Consultar y conciliar (P1 del piloto)

**Como** Bodega o Planificador autorizado, **quiero** consultar saldo y movimientos por lote y ubicación, **para** explicar una diferencia y comprobar el riesgo que se muestra.

**Escenarios de aceptación:**

1. Dado un saldo inicial importado, una recepción de 004 y un consumo de 007, cuando consulto el lote, entonces veo origen, destino, cantidad, actor, fecha de servidor, referencia y saldo resultante; la suma de movimientos explica el saldo a la fecha de corte.
2. Dada una operación aceptada, cuando se recalcula el riesgo, entonces cobertura y cantidad sugerida usan el saldo vigente y exponen fecha/fuente del consumo; una operación pendiente de sincronizar no se presenta como aceptada.
3. Dado un error de registro, cuando el rol autorizado lo corrige, entonces se crea una operación compensatoria con motivo y vínculo a la original; el movimiento original permanece consultable.

### H3 — Registrar faltantes y excepciones (P2 dentro de 007)

**Como** Planificador, **quiero** distinguir consumo atendido, demanda no atendida y anomalía por investigar, **para** no confundir un quiebre con demanda cero ni una alerta con merma confirmada.

**Escenarios de aceptación:**

1. Si se solicitaron 10 unidades y se entregaron 6, se registran 6 de consumo y 4 de demanda no atendida; el faltante no descuenta stock inexistente.
2. Una alerta de anomalía no modifica saldo ni declara merma. La confirmación o descarte exige actor autorizado, motivo, evidencia referenciada y cierre humano trazable.

## Requisitos

### Funcionales

| ID | Requisito | Traza |
|---|---|---|
| FR-01 | Una escritura operativa DEBE validar organización, punto asignado, lote del SKU, unidad base, cantidad positiva y saldo utilizable. La identidad del actor viene de la sesión. | RF-02, RNF-01, contratos API |
| FR-02 | Aceptar consumo DEBE persistir `Consumption`, `InventoryMovement`, cambio de `StockBalance` y resultado idempotente en un solo commit; operaciones concurrentes no pueden dejar saldo negativo. | RF-02, RNF-02/03 |
| FR-03 | Consumo operativo e histórico importado DEBEN coexistir sin sobrescribirse. El cálculo de demanda DEBE distinguir fuente, fecha y período y evitar el doble conteo. | RF-03, Inventario y predicción |
| FR-04 | La lectura de inventario DEBE filtrar por autorización y permitir ubicación, SKU, lote y rango de fechas con paginación. Debe mostrar saldo, reserva, disponible, unidad y corte temporal. | RF-02, contratos API |
| FR-05 | Toda corrección DEBE ser compensatoria, enlazada a la operación corregida y con motivo; no se edita ni borra un movimiento asentado. | RF-02, Modelo de datos |
| FR-06 | Recepciones y devoluciones de 004 DEBEN aparecer en la conciliación sin crear un segundo movimiento desde 007. | RF-05/06, 004 |
| FR-07 | Riesgo y propuesta de reposición DEBEN usar saldo actualizado y mostrar fuente/frescura de consumo y supuestos de datos faltantes. | RF-03, Inventario y predicción |
| FR-08 | En la ampliación, entradas de bodega, faltantes y solicitudes de reposición DEBEN conservar actor, referencia e idempotencia; una solicitud no aumenta stock disponible. | RF-02/03, contratos API |
| FR-09 | Una anomalía DEBE conservar estados distintos: alerta, investigación y cierre; solo una conclusión humana con evidencia puede marcar merma confirmada. | RF-15/V-19, Inventario y predicción |
| FR-10 | Una nueva importación de stock DEBE declarar fecha de corte y evitar sobrescribir silenciosamente movimientos operativos posteriores; ante solapamiento debe conciliar o rechazar con informe. | RF-02, importador 001 |
| FR-11 | La PWA DEBE conservar consumos pendientes por identidad tras reinicio sin red; reautorizar al sincronizar y mostrar aceptación, conflicto o sesión requerida sin perder la captura. | H1/H2, RNF-02/04 |
| FR-12 | El cierre diario DEBE distinguir consumo completo, cero observado y dato parcial, con versión y actor. Correcciones o entradas tardías invalidan ese cierre. | FR-03/07, Inventario y predicción |

### No funcionales

| ID | Requisito | Verificación |
|---|---|---|
| NFR-01 | Autorización de servidor por rol, organización y punto/ubicación; denegar referencias ajenas sin filtrar su existencia. | Pruebas de permitido/denegado y cuenta revocada. |
| NFR-02 | Una operación reintentada conserva un único efecto y una respuesta estable; concurrencia de dos actores respeta stock y reservas. | Pruebas paralelas y reintento después de perder respuesta. |
| NFR-03 | El historial permanece auditable y el saldo se puede reconstruir desde el último corte importado y movimientos posteriores. | Conciliación con cantidades exactas por lote. |
| NFR-04 | Las consultas simples cumplen el objetivo inicial RNF-10 de p95 ≤ 2 s bajo volumen de piloto acordado; registrar volumen y entorno antes de afirmar cumplimiento. | Medición con dataset del piloto, no estimación. |

## Entidades y contratos

Reutilizar `InventoryMovement`, `StockBalance`, `Consumption`, `Lot`, `Location`, `ServicePoint`, `RiskSnapshot` e `IdempotencyRecord`. El modelo actual de `Consumption` tiene un índice único por punto/producto/día; **antes de implementar H1 se debe definir una migración que permita varias operaciones del día sin convertir la importación histórica en un consumo físico**. La migración debe conservar datos e historial existentes. No se fija aquí la tabla o algoritmo de bloqueo: corresponde al plan técnico con pruebas de concurrencia.

Los contratos existentes proponen `GET /api/v1/inventory`, `POST /api/v1/consumptions` y `POST /api/v1/replenishment-requests`; no se consideran implementados por figurar en la bóveda. El plan define las rutas concretas de consulta de movimientos, corrección y ampliaciones. Todas las escrituras llevan `operationId` igual a `Idempotency-Key`, cantidades decimales y unidad explícita; usan `expectedVersion` donde se modifica un recurso versionado. Errores con Problem Details y código estable. El consumidor de `StockChanged` reconsulta el estado persistido; el evento no reemplaza la transacción.

## Guardarraíles aplicados

| Regla | Comprobación en 007 |
|---|---|
| G1/G7 | Punto, SKU, hospital y cantidad vienen de datos; el dominio no incorpora nombres del escenario. |
| G2/G3 | El importador sigue siendo la entrada de datos iniciales/históricos; la captura operativa se etiqueta por fuente y los faltantes no se inventan. |
| G4 | Todo movimiento usa unidad base del SKU; la conversión requiere regla declarada. |
| G5 | Umbrales de anomalía y frescura son parámetros, no constantes escondidas. |
| G8 | Migración y nueva operación quedan asociadas a commit, evidencia y estado de piloto; no se incorporan retroactivamente a la demo congelada. |

## Fuera de alcance

- Compras, facturación, contabilidad ERP y sincronización con un ERP general.
- Varias organizaciones o bodegas para la primera entrega; 010/011 gobiernan esa expansión.
- Rediseñar la recepción, devolución o custodia de 004.
- Llamar fraude a una anomalía estadística o confirmar merma automáticamente.
- Alterar la demo del 16–17 de octubre sin decisión expresa del equipo y nueva evidencia E2E.

## Contradicciones y decisiones pendientes de la bóveda

> **⚠️ Contradicción de calendario** — Hecho nuevo: la continuidad propone 007 tras el pase técnico y lo excluye del objetivo norte de la demo. Hecho establecido: RF-15 figura como P1 y el análisis de cobertura dice que 001–006 no cubren investigación/cierre. Impacto: redactar 007 no cierra RF-15 para la demo. Opciones: (a) acordar explícitamente RF-15 como parte del piloto 007; (b) incorporar un caso mínimo y probado en la demo actual. Propuesta: (a), pendiente de decisión del equipo y actualización de la nota de requisitos.

> **⚠️ Contrato de consumo** — Hecho nuevo: el importador agrega consumo histórico por punto/SKU/día, mientras el piloto requiere varias operaciones físicas por lote y día. Hecho establecido: `Consumption` y su índice único actual, más la fórmula de demanda en [[Inventario y prediccion]]. Impacto: registrar consumos físicos sobre la fila histórica sobrescribiría datos y distorsionaría riesgo. Propuesta: separar hechos operativos e historia y conservar trazabilidad durante la migración. El plan desarrolla la regla de día cerrado propuesta; su aceptación empresarial sigue pendiente antes de activar el piloto.

> **Regla provisional en código (15-oct, #111, sin migración).** La recepción de 004 ya produce consumo derivado del stock en percha:
> - Lo registra con `SourceKind = FieldReconciliation`, una fila por punto, producto y día, que se acumula entre recepciones.
> - **No modifica** un consumo de otro origen (importado); en ese caso la cantidad queda solo en el movimiento `FieldAdjustment`.
> - Toda diferencia entre el stock declarado y el saldo queda como movimiento `FieldAdjustment`.
> - El período se configura en `Logistics:FieldReconciliationPeriodDays`.
>
> Es un puente para que la recepción no falle. No resuelve la separación de hechos operativos e historia que propone esta contradicción: esa migración sigue siendo de 007. Detalle en la tarea T11 de [004](../004-campo-qr-custodia-offline/task.md).

## Riesgos

| Riesgo | Impacto | Respuesta |
|---|---|---|
| Descontar stock dos veces al importar historia o recibir un lote | Saldo y riesgo falsos | Fuente/operación únicas; prueba de secuencia importación → recepción → consumo. |
| Carrera entre consumo y reserva/despacho | Saldo o disponible negativo | Una transacción con control de concurrencia y pruebas paralelas. |
| Corrección que borra trazabilidad | Auditoría inválida | Movimiento compensatorio y vínculo a original. |
| Datos tardíos u offline | Orden temporal distinto al operativo | Mostrar hora de captura y aceptación por separado; no atribuir aceptación previa a sincronización. |
| Datos de demanda incompletos | Alerta aparentemente precisa | Mostrar fuente, frescura y demanda no atendida por separado. |

## Criterios de éxito

- **CE-01:** En el escenario de aceptación, un consumo válido produce exactamente un consumo operativo, un movimiento y un nuevo saldo por lote; tres reintentos idénticos dejan el mismo resultado.
- **CE-02:** Dos consumos concurrentes cuya suma supera el disponible no dejan saldo negativo ni estado parcial; el rechazado informa conflicto o insuficiencia.
- **CE-03:** Importar historia y registrar dos consumos físicos del mismo punto/SKU/día conserva los tres hechos; el cálculo de demanda aplica la regla aprobada sin duplicación.
- **CE-04:** La consulta concilia saldo de corte y movimientos posteriores para al menos un lote recibido por 004, consumido y corregido; muestra actor/referencia permitidos.
- **CE-05:** El riesgo posterior al consumo usa el saldo aceptado; una captura todavía pendiente no lo modifica.
- **CE-06:** Un actor de otro punto u organización no consulta ni modifica el inventario ajeno.
- **CE-07 (ampliación):** una anomalía puede cerrarse como descartada o merma confirmada con actor y evidencia; la alerta sola no descuenta stock.
- **CE-08:** reimportar un corte de stock anterior a un consumo ya aceptado no elimina ese consumo ni restablece silenciosamente el saldo previo.
- **CE-09:** consumo guardado en modo avión sobrevive al reinicio; al reconectar y repetirlo se acepta una sola vez. Otra cuenta no puede leer ni enviar esa cola.
- **CE-10:** dos consumos de 4 y 6 unidades con día cerrado equivalen a 10 unidades/día; una historia importada de 12 para ese día se conserva pero no suma otras 12. Antes del cierre se declara dato parcial; un cero cerrado se diferencia de un día sin captura.
- **CE-11 (ampliación):** una entrada de bodega de 20 unidades aumenta saldo una vez; pedir reposición no lo cambia. Demanda solicitada de 10 con 6 atendidas registra 4 no atendidas sin crear stock ni descuento adicional.

## Preguntas abiertas para aprobar el spec

| Pregunta | Decide | Estado |
|---|---|---|
| ¿La contraparte confirma Receptor/PWA, captura por lote conocido y cierre diario? | Contraparte + equipo | Regla propuesta arriba; confirmación pendiente |
| ¿Se acepta lote obligatorio y unidad base, sin imputación automática, para la primera entrega? | Contraparte + responsable de inventario | Propuesta para el MVP; pendiente |
| ¿Se acepta reemplazo por día cerrado y prorrateo declarado del histórico en días restantes? | Responsable de datos + equipo | Regla propuesta; pendiente de validación empresarial |
| ¿Se confirma conservar claves durante toda la vida de los movimientos del piloto? | Responsable técnico + operación | Propuesta; cualquier depuración futura requiere otro contrato |
| ¿RF-15/V-19 se difiere formalmente al piloto o se entrega un mínimo en la demo? | Equipo/Decider de alcance | Pendiente |
| ¿Qué volumen, frescura, roles y criterios de anomalía se pactan con la empresa? | Contraparte + equipo | Pendiente |
