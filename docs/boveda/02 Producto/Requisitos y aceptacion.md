---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-09
tags: [producto, reto-1]
---

# Requisitos y aceptación

Estos requisitos convierten el reto oficial y la propuesta del equipo en comportamientos verificables. IDs estables para backlog, pruebas y skills.

## Funcionales

| ID | Prioridad | Requisito | Aceptación mínima |
|---|---|---|---|
| RF-01 | P0 | Catálogo y puntos | SKU tiene unidad; punto tiene ubicación y ventana |
| RF-02 | P0 | Existencias y movimientos | Saldo conciliable por lote/ubicación; no negativos por concurrencia |
| RF-03 | P0 | Reabastecimiento crítico | Cobertura, lead time y datos de origen visibles; demanda cero/ausente tratada |
| RF-04 | P0 | Ruta factible | Capacidad, horarios y tiempos válidos; pendientes visibles |
| RF-05 | P0 | QR y custodia | QR resuelve unidad y líneas; cada transferencia tiene actor y cantidad |
| RF-06 | P0 | Recepción offline | Persiste al reiniciar; reintento no duplica; conflicto no se oculta |
| RF-07 | P0 | Dashboard | Riesgo, ruta y entrega corresponden a los mismos datos |
| RF-08 | P0 | Comparación | Ambas políticas usan misma demanda y recursos iniciales |
| RF-09 | P1 | Evidencias | Foto/firma privada ligada a recepción; carga reintentable |
| RF-10 | P1 | Chat | Solo participantes autorizados; historial tras reconectar |
| RF-11 | P1 | Incidentes | Caso demo etiquetado; nueva ruta conserva lo ejecutado |
| RF-12 | P1 | n8n/IA | Timeout/JSON inválido mantiene cálculo determinista |
| RF-13 | P1 | Consulta pública | Muestra historial permitido sin firmas, teléfonos o tokens privados |
| RF-14 | P1 | Simulación de 90 días | Ejecución reproducible, exportación y supuestos visibles |
| RF-15 | P1 | Anomalías | Alerta separada de merma confirmada; cierre investigado |
| RF-16 | P0 | Frontend con FSD | Capas, imports descendentes y API pública por slice respetados; [[Frontend con Feature-Sliced Design]] |
| RF-17 | P0 | Roles y permisos administrables | Crear/desactivar roles, asociar acciones implementadas y conceder ámbitos por organización; delegación auditada, sin autoescalación; [[RBAC y configuracion del sistema]] |
| RF-18 | P0 | Plantillas gestionadas desde DB | Formularios/documentos/etiquetas/prompts admitidos con campos y acceso configurables; versiones publicadas inmutables; capturas offline conservan su versión y se reautorizan |
| RF-19 | P0 | Parámetros, catálogos y procesos configurables | Versionar/validar/activar valores por ámbito y transiciones sobre handlers existentes; historial reproducible, sin apagar invariantes ni seguridad |

## No funcionales

| ID | Prioridad | Requisito | Cómo comprobar |
|---|---|---|---|
| RNF-01 | P0 | RBAC y autorización de recursos en servidor | Matriz de [[Seguridad y evidencias]] aplicada; denegación por defecto; rol, organización y asignación validados; revocación comprobada al sincronizar |
| RNF-02 | P0 | Idempotencia | Misma clave + mismo contenido → un efecto; contenido distinto → conflicto |
| RNF-03 | P0 | Transacciones | Dos despachos concurrentes no exceden stock |
| RNF-04 | P0 | Recuperación offline | Cierre/reapertura conserva operación y archivos |
| RNF-05 | P1 | Tiempo real fiable | Estado se recupera por API tras desconexión |
| RNF-06 | P1 | Infra reproducible | Construir entorno con versiones y variables documentadas |
| RNF-07 | P1 | Escalado correcto | Dos réplicas reciben mensajes; ALB y backplane verificados |
| RNF-08 | P1 | Operación observable | Correlación de alerta, ruta, entrega y errores |
| RNF-09 | P0 | Usabilidad/accesibilidad | Estado comprensible sin color; controles usables con móvil |
| RNF-10 | P1 | Rendimiento medido | Registrar volumen, p95 y errores; objetivo inicial p95 ≤2 s para consultas simples |
| RNF-11 | P1 | Protección de evidencias | Acceso privado y permisos comprobados |
| RNF-12 | P2 | Recuperación de fallos | Restauración/failover ensayados con tiempos registrados |
| RNF-13 | P0 | Aislamiento PostgreSQL por fila | RLS/FORCE en tablas del tenant, rol API sin bypass/propiedad, FKs por organización; SELECT/INSERT/UPDATE/DELETE cruzados y pool sin contexto deniegan; [[Esquema completo de base de datos]] |
| RNF-14 | P0 | Verificación de seguridad previa a exposición | Evidencia vigente de controles existentes y complementarios; campos/contenido/datos clasificados/límites de abuso/cabeceras/dependencias verificados para caminos expuestos; [[Verificacion de seguridad antes del lanzamiento]] |

El objetivo de rendimiento es del equipo, pendiente de carga y recursos definidos; excluir cálculos asíncronos de rutas/simulación de esa latencia interactiva.

## Casos que bloquean cierre P0

- Scan duplicado después de perder la respuesta.
- Recepción offline con una cantidad mayor a la despachada.
- Dos dispositivos intentando completar la misma entrega.
- Stock inicial cero o consumo no registrado.
- Ruta imposible y cierre de vía en medio de una ruta.
- Cambios de ruta mientras el conductor trabaja sin señal.

La evidencia final se registra mediante [[Plan de validacion]] y [[Plantilla evidencia]].

RF-17/18/19 y RNF-13/14 se incorporan por pedido directo de Bryan del 9 de octubre, ADR-16. Amplían el alcance previo de demo; su prioridad no acredita implementación y las capacidades pueden entregarse por módulos conservando la dirección configurable.
