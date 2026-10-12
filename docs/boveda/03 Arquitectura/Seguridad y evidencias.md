---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-09
tags: [arquitectura, contratos]
---

# Seguridad y evidencias

## Identidad y autorización

Identidad común web/móvil mediante un proveedor estándar compatible OIDC, pendiente de elegir. No desarrollar un sistema de autenticación propio solo para el hackathon.

El servidor deriva el actor de su sesión. Valida organización, punto, asignación y permiso de acción en cada endpoint y grupo SignalR. El cliente no puede convertir un actorId enviado en autoridad.

Para captura offline: permitir solo asignaciones previamente descargadas. La aceptación en nube requiere sesión válida y permisos vigentes; una sesión expirada conserva la cola y solicita reautenticación. Si se revoca el permiso o la asignación necesarios, no se aceptan operaciones pendientes, sin borrar su captura local.

## RBAC — control de acceso basado en roles

**Decisión inicial aceptada el 6 de octubre y ampliada por Bryan mediante ADR-16 el 9 de octubre; implementación pendiente.** Web, móvil, API y automatización usarán RBAC por acción/recurso. La matriz siguiente es el seed inicial de roles funcionales, administrable por organización según [[RBAC y configuracion del sistema]]. ADR-10 selecciona Cognito; implementación de sus flujos y validación efectiva se comprueban por separado.

Un rol concede acciones; cada acceso requiere además pertenencia activa a la organización y ámbito autorizado sobre el recurso. Tener el rol Conductor no permite recibir cualquier entrega. Aplicar mínimo privilegio y denegar por defecto toda acción sin permiso explícito.

### Matriz inicial de roles y permisos

| Rol | Acciones permitidas | Ámbito obligatorio |
|---|---|---|
| Gerencia | Consultar puntos, inventario, riesgos, rutas, indicadores, trazabilidad, evidencias y resultados de simulación; aprobar decisiones de negocio; chat | Su organización; solo conversaciones en las que participa |
| Planificador | Consultar puntos, inventario, riesgos y trazabilidad; solicitar reposición, reservar stock, calcular/publicar rutas y ejecutar/consultar simulaciones; chat | Su organización y puntos habilitados; solo sus conversaciones |
| Bodega | Consultar stock y trazabilidad; registrar lotes, preparar unidades QR, reservar y despachar | Almacenes autorizados y entregas que salen de ellos |
| Conductor | Consultar ruta y trazabilidad; registrar recepción, cargar/consultar evidencia de sus entregas, reportar incidentes y chat | Solo rutas/entregas asignadas y sus conversaciones |
| Receptor | Registrar consumo y solicitar reposición; consultar resumen de sus entregas y evidencias; aceptar cantidades | Solo puntos y entregas expresamente autorizados |
| Automatización | Solicitar cálculos de riesgo, consultar snapshots mínimos y adjuntar explicaciones; generar incidentes sintéticos | Organización habilitada; incidentes solo en entorno y escenario demo autorizados |
| ConfigurationAdmin (seed administrativo propuesto) | Administrar roles/concesiones delegables, configuración y plantillas; leer auditoría | Cuenta humana, solo organización delegada; no concede por sí mismo funciones logísticas ni evidencia privada |

La consulta pública por QR conserva su contrato limitado: no asigna un rol ni concede permisos de recepción, evidencia privada o escritura. Automatización no puede despachar, recibir, publicar rutas ni administrar accesos.

### Aplicación de permisos

- Mantener en backend el catálogo de acciones implementadas; roles, asociaciones, campos y ámbitos son administrables en DB bajo ADR-16. Políticas compartidas por API y SignalR; gestión auditada y sin autoescalación. La regla inicial de excluir un editor dinámico queda sustituida por [[RBAC y configuracion del sistema]].
- Vincular la identidad validada a sus roles por organización; no aceptar roles, actorId o ámbitos enviados por el cliente como autoridad. Una cuenta puede tener varios roles explícitos, sin trasladarlos a otra organización.
- Sembrar acceso inicial mediante bootstrap controlado y administrar asignaciones/revocaciones mediante API autorizada y auditada. Ningún usuario puede autoconcederse capacidades/ámbitos fuera de su delegación; ni web/móvil ni n8n escriben concesiones directamente en DB.
- Validar permiso de acción y recurso en cada consulta, comando, reintento offline, acceso a evidencia y entrada/envío a grupos SignalR. Consultas de trabajos y resultados heredan el ámbito de la operación original.
- Los guards, menús y botones de web/móvil reflejan permisos para orientar al usuario; el servidor aplica la decisión incluso ante una petición directa.
- Revalidar permisos al sincronizar y reconectar. Una revocación invalida también el acceso al canal; conservar la cola y mostrar el rechazo sin aplicar cambios parciales.
- Auditar cambios de roles y rechazos con actor, organización, acción, recurso, fecha y correlationId, sin guardar tokens ni evidencias sensibles.

Roles funcionales en [[Usuarios y flujos]], requisito RNF-01 en [[Requisitos y aceptacion]] y pruebas V-13/V-14/V-24 en [[Plan de validacion]].

Tablas, FKs por organización y políticas RLS/FORCE: [[Esquema completo de base de datos]]. Versionado de configuración/plantillas y permisos de campo: [[RBAC y configuracion del sistema]]. Las pruebas complementarias previas a lanzamiento pertenecen a [[Verificacion de seguridad antes del lanzamiento]] y [[istpetdev-prelaunch]]; no se consideran realizadas por tener esta documentación.

## QR y consulta del jurado

Cada **unidad logística** tiene ID interno y líneas de lote. UUID v7 puede identificarla; el lote también tiene identidad propia. Si una caja lleva un único lote, la pantalla lo muestra, pero no se asume que caja y lote son siempre lo mismo.

Contenido propuesto del QR: URL de consulta con ID de unidad y token público aleatorio revocable. El UUID no es un secreto. El token público habilita solo el resumen deliberadamente compartido; escanear no autoriza una entrega.

Consulta pública:

- SKU/unidad, estado y hitos generales permitidos.
- Sin firmas, documentos personales, teléfonos, ubicación exacta del conductor o rutas de toda la empresa.
- Sin acceso a endpoints de escritura.
- Revocación del enlace si se publicó por error.
- Protección frente a consultas masivas y errores que revelen inventario privado.

Para recibir, el conductor usa su cuenta y asignación; la consulta del jurado es independiente.

## Evidencia de entrega

1. Capturar foto/firma de demostración voluntaria y cantidades.
2. Guardar archivo local durable y checksum.
3. Pedir a la API autorización de carga ligada a recepción/asignación.
4. Cargar a S3 privado con límite de tipo/tamaño y clave controlada.
5. Confirmar metadatos y validar que el objeto existe/corresponde.
6. Dar acceso temporal de lectura solo a actores autorizados.

Una URL presignada no vuelve público el bucket. No almacenar firmas como URLs públicas ni incluirlas en logs.

Registrar por archivo: evidenceId, receiptId, tipo, objectKey, checksum, tamaño, hora y estado. Conservar originales frente a sobreescrituras accidentales con versionado cuando corresponda.

## Auditoría e inmutabilidad

Movimientos y custodia se añaden; correcciones crean entradas compensatorias con motivo. Permisos de aplicación impiden modificar/borrar historial; las proyecciones actuales sí pueden actualizarse.

Esto ofrece **historial auditable de solo adición para la aplicación**, no inmutabilidad absoluta frente a un administrador de base de datos. El pitch debe decir lo que está implementado.

Plus opcional: hashes encadenados y copia de digest a almacenamiento protegido. Un hash dentro de la misma base modificable no impide que un atacante reescriba toda la cadena. Si se usa S3 Object Lock o firma externa, definir configuración y verificarla antes de prometer resistencia a manipulación.

## Datos para demo

Usar puntos, personas, fotos y firmas ficticias/autorizadas. La IA recibe únicamente métricas necesarias anonimizadas; no fotos, firmas, datos de identidad o ubicaciones sensibles por defecto.

Mantener separados:

- Inventario y eventos operativos.
- Escenarios y eventos demo.
- Datos locales del dispositivo.
- Evidencias compartibles para evaluación.

El generador de incidentes solo funciona en entorno de demo y con credencial limitada. No publicar una interfaz para inyectar incidentes a operación real.

## Seguridad en infraestructura y defensa en profundidad

La seguridad de la aplicación (RBAC, tokens QR y URLs presignadas) se complementa con el estándar de **Defensa en Profundidad de 6 Capas** a nivel de sistema operativo y perímetro de red:
- **Perímetro y Sockets:** Firewall UFW con Default Deny y mitigación obligatoria de la trampa de Docker vinculando todos los contenedores a loopback `127.0.0.1`.
- **Acceso:** OpenSSH con llaves Ed25519 (sin contraseñas ni root) y Fail2ban con jail recidive.
- **Transporte:** Nginx con TLS 1.2/1.3, HSTS, OCSP Stapling, rate limiting y cierre de sockets TCP `HTTP 444` ante peticiones que intenten evadir el proxy o CDN.
- **Host e Integridad:** Sandboxing systemd, particiones `/tmp` `noexec/nosuid`, sysctl endurecido (ASLR 2, syncookies) y auditoría con Lynis (> 80/100).
- **CI/CD:** Cero IPs públicas ni secretos en Git; compilación en runners efímeros.

Detalles completos en [[Hardening y seguridad de servidores]] y [[CI-CD y automatizacion de despliegue]].

## Verificación

Probar la matriz RBAC con acciones permitidas y denegadas, revocación antes de sincronizar, acceso ajeno a entrega, modificación del token QR, reutilización de URL expirada, exceso de archivo, actor/rol falsificado y salto a grupos SignalR. Los criterios están en [[Plan de validacion]].

## Identidad y almacenamiento concretados — 7 de octubre de 2026

Se conserva la matriz RBAC aceptada y sus restricciones por recurso. ADR-10 selecciona Cognito; configuración completa en [[Identidad OIDC y sesiones]]. Buckets separados por entorno, upload acotado a 10 MiB/imagen, checksum, CORS, URLs temporales, retención demo y roles están definidos en [[Entornos y operacion acordados]]. El hardening se aplica conservando las seis capas y distinguiendo host/red Docker; estas notas no demuestran todavía controles activos ni acceso a evidencias reales.
