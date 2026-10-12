---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-09
tags: [producto, reto-1]
---

# Usuarios y flujos

## Roles

| Rol | Acciones | Restricción |
|---|---|---|
| Gerencia | Leer indicadores, aprobar decisiones y conversar | Consulta según organización |
| Planificador | Revisar riesgo, planificar y publicar rutas | No confirmar recepciones ajenas |
| Bodega | Registrar lotes, reservar, preparar y despachar | Custodia de su almacén |
| Conductor | Ver ruta, escanear, reportar incidente y capturar entrega | Solo asignaciones autorizadas |
| Receptor | Registrar consumo, solicitar reposición, ver resumen permitido y aceptar cantidades | Solo puntos/entregas autorizados; sin inventario general |
| Automatización | Solicitar cálculos y registrar explicaciones/incidentes demo | Identidad técnica con permisos limitados |
| ConfigurationAdmin (propuesto) | Administrar configuración, plantillas y acceso delegable | Cuenta humana con administración acotada a su organización; sin funciones logísticas automáticas |

Los seis roles funcionales conforman el seed inicial de **RBAC aceptado en ADR-14 y ampliado por ADR-16**. Desde el pedido de Bryan del 9 de octubre se pueden administrar roles/concesiones y plantillas por organización según [[RBAC y configuracion del sistema]]; ConfigurationAdmin es el seed administrativo propuesto. La matriz inicial de restricciones está en [[Seguridad y evidencias]]; la API aplica permisos y ámbito, aun para roles personalizados. Una persona puede asumir varios mediante concesiones explícitas. El QR público no concede un rol ni permite aceptar entregas.

## Flujo central

1. Bodega registra un lote con SKU, unidad y cantidad.
2. Se agrupa material en una unidad logística y se genera su QR.
3. El punto registra consumo; el backend calcula cobertura.
4. El planificador ve una alerta y revisa su explicación.
5. El sistema propone cantidades y una ruta con restricciones.
6. El planificador aprueba; se reservan cantidades de forma transaccional.
7. Bodega escanea y despacha; la custodia pasa al vehículo.
8. El conductor puede registrar ubicación y un incidente.
9. En el punto, captura cantidades recibidas, foto y firma; funciona offline si ya descargó su asignación.
10. Al regresar la conexión, sincroniza operaciones y archivos.
11. El backend acepta una sola vez, actualiza saldos y muestra historial.
12. Gerencia ve estado y métricas; el receptor consulta el QR público limitado.

## Excepciones obligatorias

| Situación | Respuesta |
|---|---|
| Stock insuficiente en bodega | Reserva parcial explícita o rechazo; no inventar existencias |
| Ruta sin solución factible | Explicar restricciones y paradas no atendidas |
| Incidente después de salir | Mantener paradas ejecutadas y replantear pendientes |
| QR inválido o desconocido | Mostrar error sin crear movimiento |
| Entrega parcial | Conciliar líneas y saldo pendiente; no marcar todo entregado |
| Rechazo por receptor | Registrar motivo y custodia pendiente/devolución |
| Sin red | Persistir localmente y mostrar estado pendiente |
| Dos registros de una misma entrega | Aplicar idempotencia y validar duplicado de negocio |
| Lote vencido/bloqueado | Excluir cantidad de disponible |
| Foto aún sin cargar | Mostrar recepción operativa y evidencia pendiente por separado |

## Estados

Entrega: borrador → planificada → reservada → despachada → en tránsito → recepción pendiente de validación → recibida parcial/completa → conciliada. Puede cancelarse antes de despacho; después se registra devolución u otra acción explícita.

En el dispositivo: pendiente → enviando → aceptada, con alternativas error recuperable o conflicto. «Guardado en el teléfono» no equivale a «aceptado por el servidor».

Estado de ruta: borrador → publicada → en curso → completada/cancelada. Publicación y revisión llevan versión para proteger asignaciones offline.

Ver [[Modelo de datos]], [[Movil offline y sincronizacion]] y [[Contratos API y eventos]].
