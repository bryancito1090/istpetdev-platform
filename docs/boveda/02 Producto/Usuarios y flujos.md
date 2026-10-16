---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-09
tags: [producto, reto-1]
---

# Usuarios y flujos

## Roles

| Rol | Plataforma Asignada | Acciones y Alcance | Restricción |
|---|---|---|---|
| Gerencia | 🖥️ **Web** (Torre de Control) | Leer indicadores macro, KPIs ejecutivos, auditar custodia global y aprobar decisiones | Consulta agregada según organización; sin captura operativa de campo |
| Planificador | 🖥️ **Web** (Torre de Control) | Monitorear matriz predictiva de quiebre de stock (5 días), optimizar y publicar rutas VRP en PostGIS | No confirmar recepciones físicas ajenas ni alterar lecturas QR de campo |
| Bodega | 📱 **Móvil** (PWA de Campo) | Registrar bultos/HU físicos, escaneo QR de despacho a unidades de custodia y cola outbox | Custodia física de su almacén; no opera en consola web de escritorio |
| Conductor | 📱 **Móvil** (PWA de Campo) | Navegar hoja de ruta offline, escanear bultos en vehículo, recibir avisos *DriverNotice* y capturar entregas | Solo rutas y vehículos asignados; 100% offline |
| Receptor | 📱 **Móvil** (PWA de Campo) | Escaneo rápido de QR en hospital, dictamen de recepción ("Todo Conforme" o "Pasó algo malo" con novedades) | Solo recepción física de bultos recibidos en su punto; sin acceso a inventarios globales |
| Automatización | ⚙️ **Worker** (Sin UI Humana) | Daemon en segundo plano: cálculos de demanda VRP, generación de avisos y proyecciones | Identidad técnica de fondo sin pantallas humanas |
| Auditor | 🖥️ **Web** (Torre de Control) | Trazabilidad inmutable de unidades logísticas, inspección de eventos QR y cadena de custodia | Solo lectura y verificación forense; no emite despachos |
| ConfigurationAdmin (propuesto) | 🖥️ **Web** (Administración) | Administrar configuración, plantillas y acceso delegable | Cuenta humana con administración acotada; sin funciones logísticas automáticas |

Los roles funcionales conforman el seed inicial de **RBAC aceptado en ADR-14, ampliado por ADR-16, segregado por plataforma en ADR-24 y refinado ergonómicamente en ADR-25**. La distribución física responde a la ergonomía operativa: los roles de movimiento físico (Bodega, Conductor, Receptor) operan exclusivamente mediante la PWA móvil con capacidad offline y lectura asimétrica QR, mientras que los roles de supervisión estratégica (Planificador, Gerencia, Auditor) operan en la consola web de escritorio. El QR público no concede un rol ni permite asentar recepciones.

## Flujo central

1. Bodega registra un lote con SKU, unidad y cantidad.
2. Se agrupa material en una unidad logística y se genera su QR.
3. El punto registra consumo; el backend calcula cobertura.
4. El planificador ve una alerta y revisa su explicación.
5. El sistema propone cantidades y una ruta con restricciones.
6. El planificador aprueba; se reservan cantidades de forma transaccional.
7. Bodega escanea en su terminal móvil PWA y despacha; la custodia pasa al vehículo de transporte.
8. El conductor puede registrar ubicación y eventuales incidentes en ruta.
9. El conductor marca «Recogido» al salir de bodega y «Entregado» al llegar al punto (issue #162); nunca registra el recibo final.
10. En el punto de entrega, el receptor hospitalario escanea el código QR del bulto desde su PWA móvil con su propia cuenta y emite su dictamen de recepción: «Todo Conforme» o «Pasó algo malo» (daño físico, precinto violado, faltante o inconsistencia de lote).
11. Al regresar la conexión, se sincronizan las operaciones y actas de recepción desde la cola outbox.
12. El backend procesa de forma idempotente, actualiza inventarios y asienta la custodia final.
13. Gerencia y Planificador auditan el cumplimiento, alertas e indicadores desde la Torre de Control Web.

## Excepciones obligatorias

| Situación | Respuesta |
|---|---|
| Stock insuficiente en bodega | Reserva parcial explícita o rechazo; no inventar existencias |
| Ruta sin solución factible | Explicar restricciones y paradas no atendidas |
| Incidente después de salir | Mantener paradas ejecutadas y replantear pendientes |
| QR inválido o desconocido | Mostrar error sin crear movimiento |
| Entrega parcial | Conciliar líneas y saldo pendiente; no marcar todo entregado |
| Rechazo por receptor | Registrar motivo del catálogo y custodia pendiente/devolución |
| Transición fuera de orden (issue #162) | 409 con código estable (`NOT_DISPATCHED`, `NOT_DELIVERED`, `ALREADY_RECEIVED`, `INVALID_STATE_TRANSITION`); sin movimientos parciales |
| Sin red | Persistir localmente y mostrar estado pendiente |
| Dos registros de una misma entrega | Aplicar idempotencia y validar duplicado de negocio |
| Lote vencido/bloqueado | Excluir cantidad de disponible |
| Foto aún sin cargar | Mostrar recepción operativa y evidencia pendiente por separado |

## Estados

Entrega: borrador → despachada (Bodega) → en tránsito (el Conductor recoge) → entregada (el Conductor entrega en el punto) → recibida parcial/completa (el Receptor registra el único recibo final, que cubre toda la entrega) → conciliada. Parcial y completa son terminales: no se recibe dos veces y lo rechazado queda bajo custodia del conductor hasta su devolución explícita a bodega. Puede cancelarse antes de despacho; después se registra devolución u otra acción explícita. Bulto: sellado → despachado → en tránsito → entregado → recibido/parcial. Detalle y códigos en `03 Arquitectura/Contratos API y eventos.md` (issue #162).

En el dispositivo: pendiente → enviando → aceptada, con alternativas error recuperable o conflicto. «Guardado en el teléfono» no equivale a «aceptado por el servidor».

Estado de ruta: borrador → publicada → en curso → completada/cancelada. Publicación y revisión llevan versión para proteger asignaciones offline.

Ver [[Modelo de datos]], [[Movil offline y sincronizacion]] y [[Contratos API y eventos]].
