---
tipo: especificacion-seguridad
estado: propuesta
actualizado: 2026-10-09
tags: [seguridad, lanzamiento, rls, configuracion, verificacion]
---

# Verificación de seguridad antes del lanzamiento

Revisión complementaria solicitada por Bryan el 9 de octubre. La skill [[istpetdev-prelaunch]] usa esta nota antes de lanzar, desplegar o dar por lista una versión. Describe controles y evidencia que todavía deben implementarse/verificarse: no afirma que la aplicación actual ya los cumpla.

Las reglas existentes conservan un solo dueño: [[istpetdev-revision]], [[istpetdev-backend]], [[istpetdev-frontend]], [[istpetdev-dbflow]] y sus referencias. Esta nota agrega los controles que no tenían una verificación específica, con fuentes oficiales. El diseño de acceso/datos está en [[Esquema completo de base de datos]] y [[RBAC y configuracion del sistema]].

## Controles complementarios y evidencia observable

| Control | Implementación requerida | Prueba que decide el resultado |
|---|---|---|
| RLS realmente activo | Política por tabla tenant, ENABLE y FORCE; rol API sin owner/superuser/BYPASSRLS; `USING` y `WITH CHECK`; políticas especiales en identidad/catálogos | Inspeccionar pg_class/pg_policies/pg_roles; dos tenants y contexto vacío prueban SELECT/INSERT/UPDATE/DELETE, tablas puente y conexión reutilizada |
| Campos protegidos | Allowlist de DTO/campos configurables; permisos de campo en lecturas y escrituras; actor/organización/estado calculado fijados por servidor; evitar binding directo a entidad EF | Payload que intenta actorId, organizationId, permissions, QuantityOnHand o approvedBy no altera esos valores; tampoco un rol/plantilla editable los habilita |
| Cifrado de contenido sensible | Clasificación previa; campos personales y formularios sensibles con cifrado autenticado mantenido por librería/proveedor, claves fuera de DB y versión de clave; mínimos datos persistidos | Lectura SQL/backup autorizado ve ciphertext en campos clasificados; rol no autorizado no obtiene plaintext por API/exportación; descifrado y rotación ensayados con datos sintéticos |
| Abuso a nivel aplicación | Límites por identidad/organización/acción, concurrencia para cálculos/rutas/simulaciones, límites de tamaño y frecuencia de búsqueda QR y upload grants | Ráfaga controlada obtiene 429/Retry-After o límite de concurrencia sin ejecutar todos los jobs; límites separados para usuarios distintos; no confiar en X-Forwarded-For arbitrario |
| Automatización abusiva del acceso público | Token QR de alta entropía, búsqueda no enumerable, respuestas mínimas y presupuesto de peticiones; desafío/bot rules solo si el canal expuesto lo requiere | Enumerar IDs sin token no descubre datos; ráfaga QR rechazada sin afectar tráfico válido; prueba a través de proxy confiable, no solo directo a API |
| Contenido de plantillas seguro | Condiciones declarativas; texto escapado; renderer/prompt sin ejecución arbitraria ni fetch a endpoints suministrados por usuario; esquema, profundidad y tamaño acotados | Plantilla/valor con script, HTML activo, URLs internas, SQL o expresión ejecutable se rechaza o renderiza como texto; captura histórica mantiene su versión |
| Consultas fuera de EF seguro | Revisar solo SQL manual, búsquedas/filtros/configuración y rutas que evitan el ORM. Parámetros para valores y mapping/allowlist para nombres de columna, sort y operadores | Payload de SQL en filtro/plantilla no cambia consulta ni ámbito; revisar que no haya concatenación en FromSqlRaw/ExecuteSqlRaw, comandos ADO o repositorios de QR |
| Confirmación segura de archivos | Además de permisos/límites existentes: verificar bytes/firma real, checksum/tamaño, rechazo de formatos activos, claves generadas en servidor y validación antes de download | Archivo con MIME/extensión falsificados o checksum incorrecto queda rechazado; objeto pendiente no se sirve; path/StorageKey ajeno no permite confirmar ni descargar |
| Cabeceras de respuesta | CSP basada en orígenes usados; frame-ancestors, nosniff, Referrer-Policy y Permissions-Policy; permisos de cámara/GPS solo donde son funcionales | Respuestas web reales detrás de Nginx/CDN tienen cabeceras; CSP permite flujo Cognito/mapa y bloquea inyección; framing ajeno rechazado; sin wildcard que anule política |
| Dependencias vulnerables | Escaneo de paquetes .NET/npm y, cuando se despliega, imagen base/contenedores; evaluar transitive deps y exposición real del hallazgo | Reporte de la misma revisión/lockfiles/image digest; vulnerabilidad crítica o alta explotable sin mitigación queda abierta y bloquea release; fix verificado, no confiar solo en nombre de herramienta |

Para paquetes npm y .NET usar las herramientas del proyecto disponibles; antes de fijar comandos comprobar versión y capacidad. En el workspace inspeccionado existen `.config/dotnet-tools.json` y proyectos .NET 8; el escaneo de paquetes puede usar `dotnet list package --vulnerable --include-transitive` y `npm audit --json` desde su raíz, con salida revisada y sin afirmar que se ejecutaron. Estos comandos requieren acceso a feeds y no verifican imagen base por sí solos. Restaurar/build no implica scan ni corregir hallazgos con upgrades de versión mayor sin revisar compatibilidad.

## RLS: pruebas y límites

El diseño completo del contexto/políticas está en [[Esquema completo de base de datos]]. Probar con usuario API, no solo migrador. La inspección mínima distingue `relrowsecurity` de `relforcerowsecurity`; verifica políticas y atributos de rol, y luego confirma comportamiento real. Una tabla con RLS activo pero una política `USING(true)` no demuestra aislamiento. RLS no cubre TRUNCATE y superusuarios/BYPASSRLS lo evitan. [PostgreSQL 16](https://www.postgresql.org/docs/16/ddl-rowsecurity.html).

Probar lectura ajena, insertar OrganizationId ajeno, mover una fila a otra organización, actualizar/deletar registros no asignados, chat ajeno, tabla hija y cuenta revocada. Incluir cambio de tenant y contexto vacío sobre el mismo pool. Job/outbox deben fijar contexto por organización; no usar un bypass global para que pasen pruebas. Los triggers/grants de columnas y las autorizaciones de campos cubren un riesgo diferente a RLS.

La base no valida el JWT por recibir `set_config`; el servicio es la frontera de identidad. Una credencial SQL conocida por un desarrollador no representa los permisos de un usuario final. El navegador consume API; ninguna plantilla admite SQL y ningún endpoint expone un ejecutor genérico.

## Cifrado y contenido configurable

Minimizar datos antes de cifrarlos. Cifrado de disco RDS protege almacenamiento; cifrado por campo reduce la exposición en lecturas/exports de DB. Usar cifrado autenticado de proveedor/librería mantenida, metadatos de versión y rotación; claves y referencias tienen IAM acotado. Campos personales de contacto y campos de formulario clasificados se separan del JSON indexable; documentos descargados ya descifrados conservan acceso/retención propios. No inventar algoritmo ni prometer que cifrado protege una API autorizada comprometida. [OWASP Cryptographic Storage](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html).

Los formularios solo aceptan campos declarados en su versión y autorizados para la acción. No confundir «crear un campo llamado roleId» con permiso para modificar roles reales. Binding a DTO explícito y rechazo de atributos adicionales relevantes impiden asignación masiva; los campos controlados por servidor permanecen protegidos aunque se modifique la plantilla. [OWASP Mass Assignment](https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html).

Angular interpola texto con escape; evitar saltarse esa protección para mostrar contenido configurable. HTML enriquecido solo si se incorpora un contrato/renderer seguro con allowlist; la primera versión usa texto y JSON declarativo. Las queries EF parametrizadas se reutilizan; la revisión complementaria apunta a SQL manual y filtros dinámicos, que requieren parámetros y mapping de identificadores. Las validaciones del cliente no reemplazan el esquema de servidor.

## Límite de abuso y despliegue real

ASP.NET Core 8 ofrece rate limiting; las políticas deben particionar por identidad/organización donde aplica y limitar acciones costosas además de peticiones totales. La limitación en memoria se multiplica con varias réplicas: definir alcance global mediante proxy/infra compartida cuando se escale. No presentar este middleware como defensa suficiente contra DDoS. [Microsoft: rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0).

Para QR público, evitar enumeration y aplicar presupuesto conservador por origen confiable y por token, con límites globales del servicio; no registrar tokens completos. Un CAPTCHA no se agrega a todos los endpoints por defecto: si hay un formulario/login propio expuesto y necesidad demostrada, usar desafío validado en servidor compatible con accesibilidad. WAF/CDN y sus costos son una decisión de despliegue, no un recurso ya comprado.

Las cabeceras se verifican en la respuesta final del navegador después de proxy/CDN; no basta verlas en Program.cs. CSP restringe scripts/conexiones/orígenes y frame-ancestors protege embedding. Ajustar conexiones Cognito, mapa, API y SignalR; Permissions-Policy no debe desactivar cámara/GPS requeridos por el flujo. La política se valida contra la app efectiva, evitando un encabezado inventado que rompa funcionalidad. [OWASP HTTP Headers](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html).

La confirmación de archivos comprueba contenido, no solo Content-Type enviado por el cliente. Solo los tipos realmente usados están en allowlist; no admitir SVG/HTML ejecutable bajo «imagen». Archivos no verificados no llegan a descarga y se retiran al vencer. La primera versión puede limitar adjuntos a los formatos ya definidos para foto/firma; nuevos tipos requieren contrato y análisis adicional. [OWASP File Upload](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html).

## Resultado previo a lanzamiento

Registrar revisión SHA/estado local, entorno, esquema/migraciones, identidad de prueba, fecha, herramienta/comando, resultado y evidencia anonimizada. Estado por control: verificado, fallo, pendiente o no aplica con justificación concreta. No aplica depende de arquitectura/camino sin exposición, no de «la librería lo hace» sin comprobar su uso. Una prueba estática y una prueba contra runtime real no se describen como equivalentes.

La skill reutiliza resultados de las otras skills; no duplica checklists ni marca un control realizado por estar documentado. Si cambiaron autorización, plantillas, queries, storage, proxy o dependencias, repetir sus casos afectados. No es necesario repetir toda la revisión por un cambio de texto sin comportamiento.

Release/demo expuesta: un fallo o pendiente explotable en aislamiento, acceso/campos, contenido ejecutable o dependencia crítica/alta accesible bloquea la declaración de versión lista. Desarrollo local: se puede levantar un entorno aislado de fixtures para implementar/probar controles faltantes, documentando alcance y evitando exposición de datos reales; no llamarlo un lanzamiento seguro. Los comandos de despliegue se ejecutan únicamente dentro de la tarea autorizada.

Cierre: RNF-13/14 y V-37/38/39 en [[Plan de validacion]]. La existencia de una skill no implementa middleware, políticas RLS ni un gate CI automático; B-40 exige integrarlo al flujo real de release.
