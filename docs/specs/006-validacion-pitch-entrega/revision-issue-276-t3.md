# Issue #276 — T3: selección propuesta para el pitch

Estado: **propuesta preparada; pendiente de aprobación de Joseph y revisión del usuario**. No se publicó ningún comentario, no se reinició la base y no se crearon pedidos. La selección estructurada está en [seleccion-issue-276.propuesta.json](seleccion-issue-276.propuesta.json); no es una corrida ni contiene resultados calculados de rutas. Actualización 16-oct: la configuración #275 ya entró en develop por PR #283 (`33387ca`); la decisión sobre puntos, cantidades, prioridades y fecha sigue pendiente.

## Idea del caso

Mostrar tres destinos críticos y uno normal para comparar alternativas propias y contratada, con una carga que incluye sacos pesados. Tomar como referencia narrativa `BOD-0001`, Bodega Central Sur - Turubamba (sintética). La API selecciona las bodegas según disponibilidad: no existe un parámetro para obligarla a usar exclusivamente esa bodega. Se registrarán las bodegas efectivas que devuelva el motor y se ajustará la narración si difieren.

El guion T4 describe A/B/C pero no fija códigos de puntos, pedidos ni cantidades. Esta selección es una propuesta nueva para aprobación, no una decisión ya tomada por Joseph.

| Punto del fixture | Nombre | Ventana del fixture | Prioridad propuesta del pedido |
|---|---|---|---|
| PTO-0004 | Hospital Carlos Andrade Marín (IESS) | 06:30–10:30 | Critical |
| PTO-0005 | Hospital General Enrique Garcés | 07:00–11:00 | Critical |
| PTO-0006 | Hospital General IESS Quito Sur | 07:00–11:00 | Critical |
| PTO-0015 | Restaurante La Mariscal (sintético) | 06:30–10:30 | Normal |

Las ventanas y relaciones comerciales son las del escenario; no se certifican horarios reales de atención. Se propone fecha operativa **2026-10-16**, consistente con el día siguiente al último consumo del fixture (15-oct). La fecha de captura de la futura corrida será la real y se registrará por separado.

## Pedidos: regla explícita y reproducible

El `mapping.json` del fixture no importa pedidos de entrega y `DemoResetService` no los crea. Para llamar a `/suggest` se necesitan pedidos concretos. Propuesta: crear uno por cada punto anterior mediante la API de pedidos, con líneas derivadas de `consumos.csv`.

**Regla propuesta:** cantidad = techo(suma de consumo diario del 9 al 15 de octubre de 2026 × 3 / 7). Exigir siete fechas distintas, `period_days = 1`, cantidades válidas y no negativas. Ante un dato ausente, fallar en lugar de asumir cero. Esta regla representa un pedido sintético de tres días de consumo; no descuenta stock del destino ni pretende ser la fórmula de reposición del motor.

| Punto | Producto | Suma de siete días | Cantidad propuesta |
|---|---|---:|---:|
| PTO-0004 | PRD-0001, hipoclorito (galón) | 98 | 42 |
| PTO-0004 | PRD-0003, detergente (saco) | 6,38 | 3 |
| PTO-0005 | PRD-0001 | 117 | 51 |
| PTO-0005 | PRD-0003 | 4,12 | 2 |
| PTO-0006 | PRD-0001 | 59 | 26 |
| PTO-0006 | PRD-0003 | 4,08 | 2 |
| PTO-0015 | PRD-0001 | 9,66 | 5 |

Estas son **entradas propuestas**, no cifras de costo ni resultados de rutas. El producto PRD-0003 figura como saco de 30 kg y peso unitario de 30,3 kg en el CSV; permitirá revisar las reglas de carga/ayudantes sin afirmar de antemano cuántos ayudantes asignará el motor.

Se descartó Guamaní (PTO-0011) para esta propuesta porque PRD-0001 tiene cinco registros en esa semana, no siete. El restaurante aporta un destino no hospitalario con datos completos. No se añade el centro comercial PTO-0013 en esta primera escena para no mezclar la demostración de ventana estrecha de #238 con este caso; puede acordarse como escena separada.

## Qué debe registrar el futuro generador

1. Verificar fixture y códigos; resolver IDs actuales por código/SKU después del reset. No reutilizar GUIDs de otra importación.
2. Verificar reloj operativo: el modo Dataset calcula el día siguiente al último período de consumo; comprobar que devuelve 2026-10-16. El servicio de abastecimiento no recibe `plannedDate` directamente.
3. Crear los cuatro pedidos con las siete líneas calculadas y una marca de escenario en `notes`. Conservar petición exacta, ID y número devueltos. `orderNumber` lo genera la API; las claves del JSON son identificadores de la propuesta, no números de pedido existentes.
4. Fijar explícitamente la fecha requerida de cada pedido y documentar su conversión UTC. Propuesta: fin de su ventana en America/Guayaquil el 16-oct; validar su interacción con el motor antes de la captura final.
5. Enviar a `/suggest` exclusivamente esos cuatro IDs y `plannedDate = 2026-10-16`. Registrar fecha solicitada y fecha operativa efectiva de `comparison`.
6. Conservar bodegas, opciones, importes, notas y ganador tal como los devuelva el motor. No exigir que B gane, que C exista sin configuración ni que una ruta resulte factible.

### Hallazgos que T4 debe comprobar antes de ejecutar

- `DemoResetService.DeleteOperationalDataAsync` no enumera `DeliveryOrders` ni `DeliveryOrderLines` antes de eliminar puntos/productos. Debe verificarse qué ocurre con pedidos existentes y sus relaciones; no asumir que repetir reset deja una base limpia ni modificar el reset de otra tarea sin identificar la necesidad concreta.
- `/suggest` construye actualmente inicio a las 08:00 UTC, mientras el servicio comparativo usa su reloj operativo. No presentar esa hora como 08:00 de Quito ni afirmar equivalencia temporal sin comprobarla. Esta diferencia debe quedar visible en la validación de la corrida.
- Al preparar la propuesta, el README mencionaba un archivo de configuración aún ausente en el checkout. #275 integró después `data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json`; el generador ya la valida offline, pero la API no se ejecutó.
- Los hashes binarios de CSV locales difieren del manifiesto por CRLF. Para los cuatro archivos revisados (bodegas, puntos, productos, consumos), normalizar CRLF a LF reproduce exactamente el hash del manifiesto. Registrar hash de bytes y convención de normalización; no alterar el fixture para forzar coincidencia.

## Texto listo para publicar en #276

> Joseph, propongo esta selección para la escena A/B/C: referencia BOD-0001 (Central Sur - Turubamba), con PTO-0004 Carlos Andrade Marín, PTO-0005 Enrique Garcés, PTO-0006 IESS Quito Sur y PTO-0015 Restaurante La Mariscal (sintético). Los tres hospitales tendrán pedidos Critical y el restaurante Normal. Fecha operativa: 2026-10-16; fecha de captura real registrada por separado.
>
> Como el fixture no crea pedidos, propongo generar uno por punto. PRD-0001 para los cuatro y PRD-0003 para los tres hospitales, con cantidad = techo(consumo del 9–15 oct × 3/7), exigiendo siete días completos. Resultan 42+3, 51+2, 26+2 y 5 unidades respectivamente según las líneas documentadas. Es una demanda sintética derivada del dataset, no un resultado ni una reposición calculada por el motor.
>
> La API decidirá las bodegas efectivas; no forzaremos Central Sur ni una opción ganadora. La configuración #275 ya está integrada en develop. ¿Apruebas puntos, regla de cantidades, prioridades y fecha, o prefieres ajustar la selección? La propuesta y los supuestos están documentados en `docs/specs/006-validacion-pitch-entrega/revision-issue-276-t3.md`.

El texto no se ha enviado. El usuario puede publicarlo o autorizar expresamente su publicación. La aprobación de Joseph sigue pendiente; no es necesaria para preparar el script con una selección provisional, pero sí para considerar final el caso del pitch.

## Revisión manual contigo

1. Abrir los CSV de puntos y bodegas y contrastar códigos, nombres y ventanas de la primera tabla.
2. Filtrar `consumos.csv` por cada punto/SKU y fechas 9–15 oct. Confirmar siete filas diarias y aplicar la fórmula de la segunda tabla.
3. Revisar el JSON de selección: cuatro puntos, tres prioridades Critical, una Normal y ninguna cifra de costo/ganador.
4. Revisar el texto del comentario y enviarlo a Joseph por el issue para decidir la selección.
5. En la prueba local de T4, verificar en Rutas que se crean y seleccionan únicamente esos cuatro pedidos. Comparar sus líneas y anotar bodegas efectivas. No se ejecutó esta comprobación de UI durante T3.

**Verificación de esta entrega:** lectura del generador, CSV, guion, importación/reset y contrato de creación de pedidos; cálculo de cantidades y comprobación de hashes normalizados. No hubo cambios funcionales ni nuevas pruebas de aplicación por ser una entrega documental.

**Siguiente etapa:** T4, escribir y probar el generador local con selección explícitamente provisional mientras llega la aprobación.
