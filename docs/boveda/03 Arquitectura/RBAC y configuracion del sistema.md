---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-09
tags: [rbac, configuracion, plantillas, seguridad, datos]
---

# RBAC y configuración del sistema

Bryan pidió el 9 de octubre que roles, permisos, parámetros y plantillas se gestionen desde la base de datos. ADR-16 acepta esta ampliación y reemplaza la parte de ADR-14 que limitaba la demo a roles fijos sin administración dinámica. Las tablas y restricciones pertenecen a [[Esquema completo de base de datos]]; esta nota define el comportamiento que deben implementar API, web, PWA y automatización. Estado: diseño; implementación pendiente.

## Qué significa configurable

| Elemento administrable | Persistencia | Límite del contrato |
|---|---|---|
| Roles y concesiones | Role, RolePermission, AccessRole, AccessLocation, AccessServicePoint, DeliveryAssignment | Se pueden crear, renombrar, desactivar y combinar roles por organización; permisos efectivos siguen un catálogo de acciones implementadas |
| Campos visibles/editables | RoleFieldPermission, TemplateFieldAccess | Solo campos del catálogo admitido; configuración no permite falsificar actor, organización, saldo, auditoría o aprobación |
| Parámetros operativos | ConfigurationVersion y ConfigurationActivation | Cobertura/margen, lead time, costos, ventanas, capacidades, límites de evidencia y retención dentro de rangos admitidos |
| Catálogos | Catalog, CatalogOption | Motivos, tipos, etiquetas y opciones; valores usados se desactivan, preservando historial |
| Procesos | WorkflowDefinition, WorkflowVersion, WorkflowState, WorkflowTransition | Etiquetas, pasos y transiciones entre acciones implementadas; los comandos mantienen las invariantes de negocio |
| Formularios | Template*, TemplateField*, FormSubmission | Campos, obligatoriedad, opciones, orden, validaciones y condiciones declarativas |
| Documentos/etiquetas QR | TemplateVersion con Kind propio | Bloques y placeholders permitidos, contenido escapado, generación por renderer confiable |
| Prompts explicativos | TemplateVersion y RiskExplanation | Contexto/idioma/texto versionado; salida de IA nunca modifica saldos ni cifras oficiales |
| Integraciones | IntegrationConnection y configuración versionada | Proveedor, programación y referencia de secreto autorizados; endpoint con allowlist y permisos técnicos acotados |

«Configurable» significa modificar estas reglas de operación sin recompilar por cada organización. Agregar una acción nueva, un tipo de campo ejecutable, una fórmula/renderer nuevo o una integración sin adaptador requiere código y revisión. Autenticación, integridad, aislamiento, auditoría y protección de campos son garantías del producto; no opciones que un administrador pueda apagar desde una plantilla.

## Identidad y evaluación de acceso

Cognito autentica: humano identificado por `(issuer, sub)` y actor técnico por `(issuer, client_id)`. Account conserva esa referencia, sin credenciales del proveedor. OrganizationAccess vincula cuenta y organización; las concesiones de roles y ámbitos se almacenan aparte. Un grupo Cognito o un rol enviado por el cliente no sustituye esas concesiones.

Acceso efectivo = identidad válida + cuenta activa + organización activa + membresía vigente + al menos un rol activo que conceda la acción + ámbito autorizado sobre el recurso + campos permitidos + invariantes de negocio. Todo componente ausente deniega. RLS aporta aislamiento en PostgreSQL; las comprobaciones completas de acción/campo/transición siguen en backend.

La autorización de consulta también controla listados, conteos, exportaciones, archivos, jobs y mensajes SignalR. Primero filtrar el ámbito autorizado y luego paginar/agregar. No cargar toda la organización y confiar en ocultar filas en Angular. Una respuesta de idempotencia se reautoriza antes de devolverla.

Permisos se combinan por unión de concesiones positivas de roles activos dentro de la misma organización; no introducir denegaciones negativas con precedencia ambigua. El contrato de cada Permission fija tipo de recurso y máximo ámbito permitido. Las restricciones por asignación de recepción, participación en chat o entorno demo siguen siendo obligatorias aun cuando el rol tenga nombre personalizado.

Cada cambio de acceso incrementa `Organization.AuthorizationVersion` y registra RbacChange en el mismo commit. Caches incluyen organización, cuenta y esa versión, comprobada en cada petición crítica y reconexión. Revocación y escritura concurrentes se coordinan mediante lectura/bloqueo de versión de autorización dentro de la transacción; no prometer revocación inmediata con un cache que se renueva cada varios minutos. Se retiran canales SignalR y se conserva la cola offline rechazada sin aplicarla.

## Catálogo inicial de acciones

Los códigos son una propuesta de contrato a implementar, no endpoints o permisos ya activos. Permission los sincroniza desde una fuente versionada de backend. Administrar roles puede asociar/desasociar estas acciones; escribir un código desconocido en DB no genera funcionalidad.

| Módulo | Códigos propuestos | Restricción esencial |
|---|---|---|
| Acceso | access.read, access.manage, role.read, role.manage, role.assign, audit.read | Administración delegada dentro de organización, sin autoescalación |
| Configuración | configuration.read, configuration.manage, configuration.publish | Definición admitida y ámbito concedido |
| Plantillas | template.read, template.manage, template.publish, template.field-access.manage, form.submit, form.read | Versión válida, campos permitidos y recurso autorizado |
| Catálogo/puntos | catalog.read, catalog.manage, product.read, product.manage, service-point.read, service-point.manage | Organización y puntos permitidos |
| Inventario | inventory.read, lot.create, handling-unit.create, stock.reserve, inventory.adjust | Ubicación autorizada; ajuste con motivo/conciliación y permiso específico |
| Consumo/reposición | consumption.create, replenishment.create, replenishment.read, replenishment.approve | Punto y cantidades autorizados |
| Riesgo | risk.read, risk.calculate, risk.explain, anomaly.read, anomaly.resolve | Organización/ámbito; explicación no altera cálculo |
| Rutas/flota | vehicle.read, vehicle.manage, route.read, route.calculate, route.publish, vehicle.position.create | Organización, vehículo y ruta/asignación autorizados |
| Entrega/recepción | delivery.read, delivery.assign, delivery.dispatch, delivery.capture-receipt, receipt.accept | Bodega en origen; conductor/receptor asignado; cantidades conciliadas |
| Evidencia | evidence.upload, evidence.read | Entrega/recepción autorizada, objeto privado |
| Incidentes | incident.create, incident.read, demo.incident.create | Recurso autorizado; acción demo solo para escenario/entorno habilitado |
| Chat | conversation.read, conversation.manage, message.read, message.create | Participación vigente; administración no permite leer conversaciones ajenas |
| Simulación | simulation.run, simulation.read, simulation.export | Dataset/run del ámbito y cuotas autorizadas |
| Integraciones | integration.read, integration.manage, automation.risk-run, automation.risk-explain | Cuenta técnica y scopes correctos; administración no revela secretos |

Para evitar permisos decorativos, cada código referencia handler/política, contrato y prueba permitida/denegada. Acciones de autorización, acceso a campo sensible y publicación requieren permisos explícitos; no un `*` que incluya automáticamente futuras acciones. Los permisos humanos no se conceden a Account.Kind técnica. La integración mantiene un máximo de capacidades técnico definido por servidor.

## Roles iniciales y administración

Los seis roles de [[Seguridad y evidencias]] pasan a ser **seeds iniciales editables**, conservando sus ámbitos: Gerencia, Planificador, Bodega, Conductor, Receptor y Automatización. Un rol personalizado concede una combinación admitida de acciones; cambiar el nombre no cambia sus permisos. IsSeed indica origen, no un bypass de seguridad.

Se propone además `ConfigurationAdmin` como rol administrativo humano inicial, separado de funciones logísticas. Concede administración de acceso/configuración/plantillas y lectura de auditoría en una organización. No implica poder despachar, recibir, leer toda evidencia o saltarse RLS. Su seed y asignación inicial son parte del bootstrap controlado, con una cuenta Cognito existente; la cuenta de Gerencia no recibe ese privilegio por su nombre.

Administrar acceso exige autoridad de delegación: AccessDelegation fija las capacidades y ámbitos que el administrador puede otorgar a miembros de la misma organización, aunque no pueda ejecutar esas acciones logísticas. La capacidad de gestión no implica poder darse cualquier permiso. El bootstrap fija el techo inicial en una operación controlada; otorgamientos posteriores conservan ese techo, CanDelegateFurther y auditoría, sin ampliarlo mediante gestión genérica de roles. Bloquear edición de capacidades técnicas desde roles humanos, asignación a cuentas suspendidas, autoampliación del acceso del actor, cambios de OrganizationId y eliminación del último administrador vigente sin reemplazo en la misma transacción.

Los cambios que amplían privilegios, modifican acceso a campos o publican plantillas muestran diferencias antes de confirmar; requieren razón, versión esperada y actor derivado de sesión. No se crean cuentas PostgreSQL por usuario final ni por rol de negocio. Los usuarios finales ingresan mediante Cognito y consumen la API.

## Ciclo de plantillas y formularios

1. Crear Template en una organización, con Kind y módulo admitidos.
2. Crear TemplateVersion en borrador. Definir campos/opciones, layout y reglas declarativas; validar tipos, profundidad, tamaño, referencias de catálogo y condiciones.
3. Configurar TemplateFieldAccess mediante permiso específico. Un campo sensible exige acceso explícito; campos no autorizados tampoco se filtran en validaciones, cálculos condicionales o documentos. La clasificación mínima de tipos/datos protegidos se fija en servidor: un editor de plantilla puede aumentarla, pero no hacer pública información sensible cambiando una etiqueta.
4. Publicar: validar versión esperada, compilar esquema de formulario, verificar consistencia con campos, congelar contenido y hash, registrar auditoría.
5. Activar TemplateBinding por organización, ubicación o punto, sin solapamientos. Precedencia explícita: punto → ubicación → organización. Usar permisos de recurso para elegir candidata, no parámetros de sesión falsificados.
6. Cada captura guarda TemplateVersionId. FormSubmission conserva los datos aceptados y la referencia concreta al recurso; archivos se confirman separadamente.
7. Cambiar plantilla crea nueva versión. Capturas pendientes offline siguen validándose contra su versión descargada y permisos actuales; versión retirada no destruye evidencia previa. Si ya no se acepta nueva captura con esa versión, devolver conflicto recuperable y permitir migración asistida, sin modificar silenciosamente los campos.
8. Correcciones producen nueva submission con SupersedesSubmissionId, actor y motivo; no sobreescribir el registro aceptado. Lectura de versiones históricas sigue aplicando los permisos de campo vigentes.

La definición y layout inicial son JSON declarativo. Condiciones usan operadores acotados de comparación/lógica y referencias de campos válidas, sin `eval`, consultas SQL ni scripts. Para documentos, placeholders provienen de DTOs autorizados; el renderer escapa texto y no accede a red/archivos arbitrarios. El prompt del LLM no contiene claves ni concede acciones.

Datos sensibles de formularios requieren clasificación y cifrado: separarlos del JSON público/indexable, con referencia de clave, versión de algoritmo y autorización de lectura. El cifrado de volumen RDS y las ACL de campos cumplen objetivos diferentes; tratamiento detallado en [[Verificacion de seguridad antes del lanzamiento]].

## Configuración efectiva y procesos

ConfigurationDefinition fija nombre, tipo, rango, unidad y ámbitos permitidos. Los valores se editan en nueva ConfigurationVersion, se validan y se activan en ConfigurationActivation. Un cambio de margen de cobertura o costo no reescribe riesgos y simulaciones anteriores: cada ejecución fija versión/hash.

WorkflowVersion publica estados y transiciones con PermissionId, HandlerCode y condiciones permitidas. Los comandos mantienen el contrato de [[Usuarios y flujos]]: no autorizar transición a recibida sin cantidades aceptadas, no devolver stock reservado como disponible sin liberar reserva y no modificar ruta ejecutada retroactivamente. Solo configurar transiciones entre handlers implementados. Sembrar el proceso inicial de entrega/ruta/solicitud para que la aplicación tenga una configuración utilizable.

Flags pueden habilitar módulos implementados y orientar navegación; ocultar menú no elimina permisos ni cierra endpoints. Un módulo deshabilitado debe rechazarse en servidor. Deshabilitar una función con operaciones pendientes requiere una política de finalización/cancelación, y el sistema muestra el impacto antes de activarlo.

## Contratos y cierre

[[Contratos API y eventos]] incorpora rutas propuestas para roles/concesiones, versiones de configuración, workflows y plantillas. Todas las escrituras llevan idempotencia cuando son reintentables y expectedVersion para recursos mutables. Listados paginados, acceso auditable y errores de validación que no revelan campos/organizaciones ajenos.

Cerrar RF-17/18/19 y RNF-13 exige V-34/35/36/37. La base propuesta no configura automáticamente el backend: faltan migraciones, políticas, handlers, administración web y pruebas. El editor dinámico es alcance solicitado; su implementación puede dividirse en entregas sin volver a describir los roles como un catálogo fijo no administrable.
