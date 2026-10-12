---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-07
tags: [arquitectura, infra, aws, operacion, acuerdos]
---

# Entornos y operación acordados

## Alcance y relación con el trabajo del compañero

Bryan confirmó el 7 de octubre de 2026 que hay cuenta AWS y presupuesto disponibles, que cada integrante ejecutará API/web/móvil localmente, que se quiere una base AWS compartida y que los despliegues completos AWS se encenderán pocas veces para pruebas después de desarrollar el sistema. También delegó completar red, IAM, identidad, datos, backups, S3, observabilidad y recuperación.

Se conserva el aporte del compañero en `4c3a9ef` sin sustituir sus secciones: Perfil A EC2/Compose/GHCR/Nginx, Perfil B ECS/RDS/ALB, CI/CD DAG, hardening de seis capas, Signals/RxJS/FSD y shared-core. Esta nota es el complemento operativo para implementar esos acuerdos. Sus parámetros posteriores concretan los ejemplos previos; ninguna configuración documentada significa que ya se haya aplicado en AWS.

## 1. Entornos, disponibilidad y gasto

| Entorno | Ejecución y datos | Activación |
|---|---|---|
| `local` | API .NET 8 y Angular/Ionic en cada máquina; dependencias Compose propias o conexión a RDS dev por túnel | Cada integrante inicia su stack; el repo solo contiene ahora el esqueleto |
| `dev-shared` | Un RDS PostgreSQL 16 privado, bases aisladas por integrante y una base de integración, S3 dev y Cognito dev | Crear cuando comience el trabajo contra DB; disponibilidad coordinada entre el equipo |
| `ci` | Runners GitHub efímeros; PostgreSQL/PostGIS de prueba aislado y builds/tests temporales | PRs y pushes a `develop`; no usar la base compartida para suites destructivas |
| `demo` | Stack completo **Perfil A**: EC2 `t3.medium`, Compose, PostgreSQL/PostGIS con volumen, Nginx, S3 y n8n externo | Encendido y deploy manual cuando el sistema esté listo para ensayos |
| `prod` | **Perfil B** reservado para operación nacional, separado de dev/demo | Aprovisionar después cuando se necesite producción; no crearlo para cada prueba |

El primer sistema completo publicado en AWS para pruebas será `demo`/Perfil A. Si se lo llama informalmente producción durante el evento, registrar igualmente que es demo, datos sintéticos y host único. Pasar a producción empresarial exige el perfil y garantías de recuperación correspondientes; no reutilizar silenciosamente la DB de desarrollo.

**Región seleccionada para estos diseños: `us-east-1`.** Concentrar cómputo, RDS, S3 y Cognito en esa región y medir latencia desde Ecuador. La cuenta y presupuesto están confirmados; no hace falta volver a pedirlos. El ID de cuenta, permisos efectivos y valores monetarios concretos se cargan fuera de Git por Bryan. AWS no se aprovisiona durante esta entrega de documentación/esqueleto.

La DB compartida es la excepción al encendido esporádico del stack completo: necesita estar disponible cuando el equipo trabaja. Pararla requiere coordinación. RDS puede permanecer detenido como máximo siete días y después AWS lo inicia; almacenamiento/backups siguen cobrando. EC2 parada conserva costos de EBS y Elastic IP retenida. Llevar tags `Project=IstpetDev`, `Environment`, `ManagedBy=Terraform` y `ExpiresAt` para recursos temporales; registrar encendido/apagado y revisar costos al cerrar cada ensayo.

Las cifras mensuales del diseño original son referencias, no límites operativos. Para cargas intermitentes calcular horas reales, almacenamiento persistente, IPs, RDS dev, Cognito/M2M, logs y APIs; el precio no escala íntegramente con las horas de EC2. Configurar alertas de costo real y previsto al 50/80/100% del presupuesto que Bryan cargue, sin inventar una cifra.

## 2. Red y acceso

VPC dedicada `10.42.0.0/16`, sujeta a comprobar que no colisiona con redes del equipo/institución antes del apply:

| Subred | CIDR | Uso |
|---|---|---|
| Pública A/B | `10.42.0.0/24`, `10.42.1.0/24` | Host Perfil A / nodo de acceso dev y ALB del Perfil B |
| Aplicación privada A/B | `10.42.10.0/24`, `10.42.11.0/24` | Fargate solo cuando se implemente Perfil B |
| Datos privada A/B | `10.42.20.0/24`, `10.42.21.0/24` | DB subnet group de RDS y ElastiCache posterior |

Mantener la referencia a dos AZ; elegir sus IDs en Terraform en la cuenta real. Para dev y Perfil A usar IGW y salida pública del host/nodo de acceso sin NAT. RDS no tiene acceso público. IPv6 no se habilita en la primera entrega; si se activa después, replicar explícitamente SG/firewall y pruebas de exposición.

### Acceso del equipo a RDS dev

- Nodo EC2 de acceso `t3.micro`, Ubuntu 24.04, SSM Agent, perfil IAM de instancia y sin reglas inbound. No ejecuta aplicaciones ni aloja datos.
- El nodo está en subred pública con IP pública para salida HTTPS a Systems Manager; RDS queda en subred privada.
- Security Group RDS: TCP 5432 únicamente desde SG del nodo de acceso y desde los consumidores AWS que se autoricen después; nunca `0.0.0.0/0` ni allowlist de todos los hogares.
- Cada integrante usa IAM Identity Center/SSO, MFA, AWS CLI v2 y Session Manager plugin. Puede iniciar túnel solo al nodo etiquetado dev y usar su credencial PostgreSQL propia.

Ejemplo de acceso después de provisionar, con identificadores reales cargados localmente:

```bash
aws sso login --profile istpetdev-dev
aws ssm start-session \
  --profile istpetdev-dev \
  --region us-east-1 \
  --target '<INSTANCE_ID_ACCESO_DEV>' \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["<RDS_DEV_ENDPOINT>"],"portNumber":["5432"],"localPortNumber":["15432"]}'
```

Usar TLS PostgreSQL `SSL Mode=VerifyFull` y CA oficial RDS. Para conservar validación de hostname a través del túnel, el cliente Npgsql usa el **endpoint DNS real** en `Host`, puerto local 15432 y un mapeo DNS/hosts local de ese nombre a `127.0.0.1`; el nodo SSM resuelve el endpoint real dentro de AWS. No cambiar DNS cloud ni desactivar validación del certificado por conectarse a localhost. Documentar y retirar el mapeo local al dejar de usar el túnel. Si se ejecuta la API en un contenedor local, configurar y probar su ruta al túnel del host; el `127.0.0.1` del contenedor no es el del portátil.

El nodo de acceso se apaga cuando nadie necesita la DB y se enciende antes de las sesiones coordinadas. Port forwarding registra acceso en CloudTrail, pero no registra el contenido SQL de la sesión; la auditoría de datos corresponde a PostgreSQL y la API.

### Acceso y despliegue del Perfil A

Se conserva **SSH + SCP** del compañero. Administración humana preferentemente por SSM; SSH administrativo solo desde CIDRs específicos cuando se requiera. Para el runner GitHub, la identidad OIDC de deploy autoriza temporalmente una regla TCP 22 desde su IP de salida `/32` en el SG de demo, ejecuta SSH y revoca la **misma rule ID** en una fase `always`, incluso si falló el deploy. El job debe verificar primero que logra esa conectividad; no asumir IP fija del runner.

La regla tiene descripción con run ID y caducidad operativa; el runbook limpia reglas temporales huérfanas tras cancelaciones abruptas. El rol solo modifica ese SG del entorno y el workflow se limita al repo/rama/environment aprobado. No abrir SSH globalmente. Guardar huella/clave del host en `known_hosts` por un canal confiable; no usar `StrictHostKeyChecking=no`.

HTTPS 443 llega a Nginx. HTTP 80 sirve redirección/ACME según determine el compañero de dominio. Con CDN, restringir origen a rangos del CDN y verificar mTLS antes de afirmar que no se puede evadir; un header Host o `return 444` solo no autentica el CDN.

### Red Docker y hardening

El binding `127.0.0.1` del diseño se aplica a **puertos publicados del host**. En una red bridge, los procesos dentro del contenedor escuchan en la interfaz de su contenedor: API `http://0.0.0.0:5000`, con publicación `127.0.0.1:5000:5000` cuando Nginx corre en host. DB/Redis se consumen como `db:5432`/`redis:6379` y no publican puertos al host. Configurar credenciales/redes/pg_hba, no `localhost` interno que impida llegar a ellos.

Conservar UFW, Fail2ban, SSH Ed25519, Nginx, controles kernel y Lynis del compañero. Verificar también comunicación y egress Docker después del firewall. Usar imágenes soportadas, procesos no root cuando la imagen lo permita, límites de recursos, health checks, logs rotados y `restart: unless-stopped`. El grupo docker y sudo otorgan poder elevado sobre el host: el usuario de deploy es una identidad operativa privilegiada, exclusiva del entorno, aunque no haga login como root. Limitar sus llaves, comandos y permisos efectivos; no presentarlo como usuario sin privilegios.

Ajustes que deben validarse al implementar: permitir `geolocation=(self)` solo para orígenes que registren pings, cámara según flujo; confiar IP real únicamente desde CDN conocido; rate limits separados para login/QR/API/hubs; no aplicar restricciones JIT/systemd incompatibles al runtime. Una IP pública es un dato de direccionamiento; rotar credenciales ante fuga de secretos. El acceso se protege por SG/TLS/identidad, no por tratar la IP como contraseña.

## 3. IAM, secretos y state

| Identidad | Permisos y límites |
|---|---|
| Equipo por SSO/MFA | SSM al nodo dev, lectura de su secreto PostgreSQL y S3 dev limitado a prefijos de su organización de prueba para la API local; sin permisos sobre producción, backups, state ni master RDS |
| Administrador de infraestructura | Terraform/bootstrap mediante rol y SSO; planes revisados, posibilidad de apply/destroy según entorno |
| GitHub build | `contents: read`, `packages: write`; GITHUB_TOKEN para GHCR, sin permisos AWS |
| GitHub deploy demo | OIDC STS, `aud=sts.amazonaws.com`, subject del repo `istpetdev-platform` y environment demo; limitar ramas desde GitHub. Solo SG de demo y las operaciones necesarias al deploy |
| Host EC2 demo | SSM, envío de logs, lectura de secrets del entorno; identidad host distinta del token de GitHub |
| Runtime API | S3 de evidencias del entorno y credenciales BD de aplicación; sin permisos Terraform ni administración de usuarios AWS |
| Migrador/backup | Credencial PostgreSQL dedicada y permisos del bucket de backups; no se entrega a web/móvil |
| Perfil B futuro | Separar ECS execution role y task role; no copiar credenciales estáticas de EC2 a tareas |

La política efectiva debe enumerar acciones/ARNs: SSM StartSession al nodo/documento permitidos y Resume/Terminate solo sesiones propias; Secrets Manager GetSecretValue al secreto asignado; API S3 Get/Put/GetObjectVersion sobre prefijos autorizados, sin administración de buckets; backup S3 Put/Get en su bucket; logs CreateLogStream/PutLogEvents en los log groups del entorno. El rol GitHub solo usa Authorize/RevokeSecurityGroupIngress sobre el SG demo y las lecturas Describe necesarias. KMS Decrypt se limita a keys/contextos usados. Las acciones Describe que no admiten ARN se acotan por región/condiciones; no usar AdministratorAccess para el runner o runtime. Trust OIDC debe usar el subject exacto de environment demo cuando el workflow lo declara; si no existe soporte de environments en el plan, usar el subject `repo:bryancito1090/istpetdev-platform:ref:refs/heads/main` y validación de rama/workflow, sin ampliar a cualquier ref.

En Perfil A un instance profile protege el acceso AWS del host, pero no constituye aislamiento IAM absoluto entre contenedores que pueden alcanzar IMDS. Exigir IMDSv2, configurar hop limit según networking y comprobar alcance desde cada contenedor; restringir la metadata de contenedores que no la necesiten. La separación de task roles se implementa en Perfil B.

Secrets Manager guarda conexiones BD, credencial técnica n8n/API y secretos del entorno bajo `/istpetdev/<entorno>/...`; Parameter Store/configuración guarda valores públicos. Local: `.env` ignorado o .NET user-secrets. AWS: materialización controlada de `.env` host `0600`, fuera de imágenes/artefactos/logs. No inyectar secretos en bundles Angular ni exportaciones n8n.

Se conserva Terraform con state S3 y `use_lockfile=true`: bucket privado, versionado, cifrado KMS, policy de TLS, permisos por key de entorno y recuperación ensayada. Permitir Get/Put/Delete en `.tflock`; el state no necesita Delete para uso normal. Bootstrap usa inicialmente state local protegido y se migra al backend creado; no intentar usar un bucket antes de que exista. El bucket/KMS de state queda fuera del destroy de stacks temporales. Versionar `.terraform.lock.hcl`, excluir `.terraform/`, `*.tfstate*`, planes y tfvars privados. No confiar en `sensitive=true` como sustituto de proteger el state.

## 4. PostgreSQL, aislamiento y migraciones

RDS dev: PostgreSQL 16 con PostGIS compatible, `db.t4g.medium`, Single-AZ, 20 GiB gp3, autoscaling máximo 100 GiB, storage cifrado, `publicly_accessible=false`, `rds.force_ssl=1`, retención de backups siete días y deletion protection. Single-AZ es suficiente para colaboración; no se declara HA. Registrar motor, minor y extensión efectivos al aprovisionar.

Una instancia sirve a `istpetdev_dev_01` ... `istpetdev_dev_05` y `istpetdev_integration`. Cada integrante tiene usuario/migrador de su base; revocar CONNECT y privilegios PUBLIC innecesarios, sin acceso a las otras bases. La base de integración usa schema/release desde `develop`; no recibe migraciones experimentales ni seeds simultáneos. El equipo puede trabajar conjuntamente contra integración con usuarios autorizados y datos sintéticos, coordinando cambios.

En demo, PostgreSQL/PostGIS permanece en Docker con volumen nombrado sobre EBS cifrado; no reutilizar el RDS dev como BD de demo. Aplicación sin superusuario, rol migrador con DDL y rol backup con lectura necesaria. Extensión PostGIS la instala el administrador/migrador controlado, no la aplicación. UUID v7 se genera mediante implementación interoperable API/móvil que se valide; PG16 lo almacena, sin suponer `uuidv7()` nativo.

EF Core/Npgsql 8, `ConnectionStrings__Main` y pool inicial máximo diez conexiones por API local; medir total de conexiones antes de subir límites. Migraciones versionadas en Infrastructure y ejecutadas como **tarea única** de release, nunca desde cada API al arrancar. No usar `EnsureCreated` en bases con migraciones.

Orden: backup verificado → comprobar versión inicial del schema → migración idempotente compatible → nueva API → comprobación funcional. Usar expand/contract: primero cambios compatibles, retirar columnas/contratos solo cuando ya no existan clientes/release anteriores que los necesiten. Seed por entorno, idempotente y con fixture/seed versionados. Reset solo explícito en bases personales/CI o demo designada; prohibido por defecto en integración/producción.

El primer despliegue completo carga fixture y migraciones en su propia base. Una copia posterior de datos entre entornos exige exportación/importación controlada, permisos y verificación; nunca compartir usuarios ni usar datos reales en demo.

## 5. Backups y recuperación

| Datos | Respaldo y retención | Objetivo inicial a medir |
|---|---|---|
| RDS dev/integración | Backups automáticos/PITR siete días; snapshot antes de cambio destructivo | RPO objetivo <=15 min mientras activo; RTO <=2 h |
| PostgreSQL demo | `pg_dump` comprimido nocturno y antes de migración; copiar a S3 privado, conservar siete diarios y cuatro semanales | RPO <=24 h; RTO <=60 min |
| Release demo | Manifest, Compose/config sin secretos y tres releases verificadas | Restaurar release compatible en <30 s con imágenes precargadas; medir aparte recuperación del host/BD |
| Evidencias S3 | Versionado; preservar vínculo BD/objectKey/checksum | Recuperar versión borrada/alterada en ensayo; no prometer backup multirregión |
| n8n externo | Exportar workflows; backup de su DB y clave de cifrado protegida por Bryan | RPO <=24 h y RTO <=2 h propuestos para la integración; comprobar con el servidor real |

Los RPO/RTO son objetivos de diseño; guardar tiempos reales en [[Plan de validacion]]. El rollback de aplicación no es restauración de datos.

El backup obligatorio falla el deploy si no termina correctamente. Usar `set -euo pipefail`, comprobar tamaño/archivo y checksum, subirlo a S3 y verificar la carga antes de migrar. Si se conserva `.sql.gz` del compañero, comprobar también `gzip -t`; alternativamente `pg_dump -Fc` comprimido permite `pg_restore`. No usar `|| true` para encubrir fallos. Un archivo válido no demuestra restaurabilidad: antes del evento restaurar a una base nueva y verificar migraciones, saldos, recibos, roles y metadatos de evidencia.

Bucket backups separado de evidencias y state, con Block Public Access, cifrado, versionado, mínimo privilegio y expiración de no actuales acorde a retención. Antes de apagar/destruir demo: confirmar backup externo y manifest, guardar evidencia del ensayo, detener sin borrar volúmenes o hacer destroy explícito con snapshot/dump validado. No ejecutar `compose down -v` como limpieza rutinaria.

## 6. S3 y evidencias

- Buckets separados por entorno y finalidad, con sufijo de cuenta/región para nombres únicos; Object Ownership bucket-owner-enforced, sin ACL pública y Block Public Access completo.
- Demo/dev: SSE-S3 y policy que exige transporte TLS; producción Perfil B: SSE-KMS con permisos de key separados. Object Lock sigue opcional P2 y no se activa como requisito de la demo.
- Clave: `<organizationId>/<receiptId>/<evidenceId>/<version>` generada por backend. No aceptar claves arbitrarias del móvil; el bucket no se vuelve público por usar presigned URLs.
- Upload: presigned POST con expiración cinco minutos y `content-length-range`, o PUT con verificación posterior equivalente. Máximo **10 MiB por archivo**, JPEG/PNG/WebP para foto/firma, MIME y firma binaria coherentes; no SVG/HTML/PDF arbitrario en esta captura.
- Confirmación: validar tamaño, checksum SHA-256, tipo, objeto existente y recepción/asignación antes de marcar evidencia completa. Eliminar/aislar cargas inválidas; no confiar solo en Content-Type del cliente.
- Lectura privada: URL autorizada con máximo 15 minutos; acceso auditado. QR público no devuelve objectKey ni foto/firma privada.
- CORS: lista exacta de orígenes dev `http://localhost:4200`/`:8100` y orígenes HTTPS del dominio final; métodos/headers necesarios y exposición de ETag/checksum si se usa. No wildcard general.
- Retención demo: borrar objetos de prueba 30 días después de finalizar el escenario, incluidos no actuales; uploads incompletos siete días; huérfanos no confirmados 24 h mediante tarea que consulta metadatos. Retener evidencia necesaria del evento en una exportación separada autorizada.
- Producción: política de retención empresarial pendiente de requisitos reales. Conservar el ciclo de vida del diseño, pero comprobar umbrales de tamaño/costo antes de pasar fotos pequeñas a Standard-IA/Glacier; no borrar evidencia empresarial con la política demo.

## 7. n8n externo, IA y conectividad

Usar exclusivamente el servidor de Bryan: **https://n8n.bryan-bano.com/**. No agregar otra instancia n8n obligatoria al Compose demo ni al EC2. La base y la clave `N8N_ENCRYPTION_KEY` pertenecen a ese servidor y no se distribuyen a los integrantes ni al repo.

Configurar WF-01 riesgo/explicación, WF-02 incidente demo y WF-03 recuperación según [[n8n e IA]]. Exportaciones sanitizadas en `automation/n8n/workflows/`; referenciar nombres de credenciales. Bryan cargará credenciales DeepSeek y del cliente técnico de [[Identidad OIDC y sesiones]]. `N8N_API_KEY` solo si se automatiza importación administrativa; `N8N_WEBHOOK_SECRET` autentica activaciones demo y no es el token de acceso a nuestra API.

**Un n8n remoto no puede llamar al localhost de un portátil.** En desarrollo el cálculo/fallback determinista funciona sin n8n; probar integración externa cuando la API demo sea accesible por HTTPS. Si se requiere antes, usar un túnel HTTPS temporal autenticado a un solo backend del equipo, con `scenarioId` aislado y cierre al terminar; nunca publicar RDS ni credenciales. No configurar un workflow con `http://localhost:5000` suponiendo que apunta al equipo.

Configurar `America/Guayaquil` como timezone de cron y UTC para timestamps/API; cron de explicación inicialmente cada 15 min, activación manual para demo. HTTP externo: timeout 15 s, tres intentos totales con espera y jitter, reintentar 429/5xx/transporte respetando Retry-After; no repetir errores definitivos. operationId/runId evitan duplicar efectos. Límite de concurrencia inicial dos ejecuciones y presupuesto/token cap cargado por Bryan. Modelo/prompt se registran por versión; no inventar un modelo ni inferir acceso a la cuenta.

El servidor sigue operando independientemente del encendido AWS; si demo está apagada, pausar cron del proyecto o usar control de disponibilidad para no generar tormentas de errores. n8n llama a la API con OIDC M2M y scope demo limitado; no se conecta directamente al RDS operativo. Su indisponibilidad no bloquea inventario/custodia.

## 8. Observabilidad

API: logs JSON con UTC, environment, release SHA, correlationId, operationId y errorCode; sin tokens, firmas, contraseñas, query tokens QR/SignalR ni payloads privados completos. Auditoría de negocio/roles vive en PostgreSQL, separada de logs técnicos.

Local: consola y logs Docker. Demo: CloudWatch Agent para memoria/disco y logs de API/Nginx/sistema, retención **14 días**; Docker `json-file` máximo `10m`, tres archivos por contenedor. Alarmas: disco >80%, memoria >85% durante cinco minutos, fallo de readiness tres comprobaciones consecutivas, errores HTTP 5xx >5% con al menos 20 peticiones/5 min y fallo del último backup. CPU >70% sostenida diez minutos y créditos T3 disparan revisión de capacidad/costo. Registrar canales/destinatarios de alertas cuando el equipo los cargue; esta documentación no envía mensajes a nadie.

`/health/live` valida proceso; `/health/ready` comprueba DB/migraciones/dependencias esenciales. n8n/LLM no son dependencias obligatorias de readiness P0. Conservar `/api/ping` del compañero como diagnóstico, pero no como única condición de release. Reporte de deploy guarda SHA de cada imagen, migración, resultado/backup y límites medidos, sin volcar secretos.

Medir p95 de consultas simples <=2 s y la consulta QR (<150 KB gzip, objetivo 1,5 s) con dispositivo/red/volumen registrados. Si demo está apagada, suspender alarmas de servicio planificadamente, manteniendo alarmas de costos/storage/RDS dev. Un host sin tráfico no demuestra carga nacional.

## 9. GitHub Actions, deploy y rollback

El compañero que incorporó CI/CD implementará los workflows y contenedores de desarrollo. **Pendiente de implementación**: esta entrega especifica contratos/rutas/controles y deja sus directorios vacíos. Actions compila/publica imágenes y puede levantar Compose efímero para integración; no sirve como hosting persistente de la DB compartida o de los portátiles. Cada integrante descarga/ejecuta contenedores localmente cuando esos artefactos existan.

Conservar DAG, GHCR, SHA, SSH/SCP, quick-restart, backup, tres versiones y GitHub Summary. CI: `dotnet build -c Release` seguido de `dotnet test -c Release --no-build`, `npm ci`, lint/tests/build web/móvil. Filtros: `src/**`, `apps/**`, `libs/shared-core/**`, `scripts/base_datos/**`, `infra/compose/**`, `infra/nginx/**`, lockfiles y workflows. Compartidos/configuración/migraciones deben activar las validaciones necesarias aunque no cambie una app.

Para la primera versión construir **backend y web juntos** por release SHA, manteniendo jobs independientes/paralelos. Así existen ambas imágenes del SHA que pide el deploy. El filtro sigue evitando releases por cambios puramente documentales. Si el compañero mantiene builds selectivos, es obligatorio un manifest con SHA/digest independiente por servicio y condiciones que toleren jobs omitidos; no pedir una imagen inexistente. Imágenes por SHA son identificables, pero un tag se puede sobrescribir: el deploy registra y usa digest.

Disparadores: CI en PR/develop; build GHCR después de CI; deploy AWS mediante `workflow_dispatch` cuando el entorno esté encendido. `main` sigue como fuente estable; un push no debe encender/aprovisionar AWS automáticamente. `reset_database=false` por defecto, permitido solo para demo explícita con backup y barrera por entorno; jamás producción. Identidad AWS temporal OIDC y permisos GitHub mínimos; pin de acciones de terceros por SHA, validación de inputs y known_hosts.

Release manifest persistido en `/var/www/istpetdev/releases/<release-id>/`: SHA/digests backend/web, versión Compose y schema, timestamp, dataset/seed y referencia backup; secretos por separado. Mantener enlace `current` y `previous` solo después de verificación. Un reinicio toma el manifest actual; no depende de exports de una sesión SSH ya terminada. `restart_only` reinicia solo servicios de aplicación objetivo, sin reiniciar DB ni n8n externo.

Deploy serializado por `concurrency` del environment con `cancel-in-progress=false`; además lock remoto `flock` compartido por deploy/rollback/restart para impedir carreras de workflows distintos. Validar inputs con allowlist de release verificada, no interpolar texto arbitrario recibido en shell.

Orden obligatorio: validar espacio/config/digests → descargar y precargar imágenes → backup externo verificado → migración compatible única → recrear servicios de aplicación → readiness con reintentos hasta 90 s → comprobación HTTPS desde runner y recorrido mínimo → activar manifest current → limpiar solo releases fuera de las tres verificadas. Si falla salud, job rojo y volver a la última release compatible; no convertir fallos en mensajes de éxito.

Retención: seleccionar tres **manifests verificados**, proteger imágenes/digests referenciados y volúmenes; no ordenar IDs de imágenes ni hacer `rmi -f` indiscriminado. Limpiar después del éxito y sin eliminar el backup preventivo.

Rollback: validar release/digests locales y compatibilidad schema, cargar manifest persistido, ejecutar Compose con `--pull never`, verificar readiness/HTTPS y registrar resultado. Las tres releases previstas se precargan, por lo que ese camino no depende de GHCR/red para descargar. Restaurar una versión fuera de caché es otro procedimiento con autenticación GHCR y tiempo distinto. Objetivo <30 s medido desde ejecución en host hasta servicio listo; medir por separado espera del runner/workflow. El host único puede tener interrupción breve durante Compose: no afirmar zero downtime/atomicidad de todos los servicios sin una estrategia adicional validada.

Una migración incompatible bloquea rollback automático. Restaurar DB requiere runbook específico, aprobación operativa y reconciliación de operaciones aceptadas desde el backup; no rebobinar datos a ciegas. Registro de tiempo/resultado en B-21 y evidencia. Registrar por separado recuperación de instancia y failover del Perfil B.

## 10. Pendientes que continúan abiertos

- **Dominio institucional y DNS (RESUELTO - 12/Oct/2026):** Asignado subdominio `istpetdev.doicela.dev` administrado en Cloudflare (modo Flexible/Full) apuntando al servidor EC2 Debian con Nginx Reverse Proxy.
- **Implementación GitHub Actions/Compose (RESUELTO - 12/Oct/2026):** Materializado en el repositorio `istpetdev-platform` con workflows (`ci.yml`, `deploy.yml`, `rollback.yml`), `docker-compose.prod.yml`, Dockerfiles multi-stage y script desatendido `user-data.sh`.
- **Valores privados:** Bryan cargará account ID, SSO/permisos, credenciales DB/n8n/DeepSeek, client secrets, presupuesto y destinatarios de alarmas por los canales documentados.
- **Mapas:** ADR-05 sigue pendiente de proveedor, cobertura Ecuador, licencia de caché y cuotas; no sustituirlo silenciosamente por una librería de renderizado.
- **Verificación real:** compatibilidad Ionic/Angular, migraciones/túnel/TLS, dispositivos offline, backups/restauración, seguridad y tiempos de deploy/rollback.
- **Producción nacional:** cerrar tamaño de carga, egress NAT/endpoints en dos AZ, backplane/afinidad SignalR, capacidad RDS y failover. RNF-07/V-15 se conserva; B-21 ahora cubre rollback, por lo que la prueba de dos réplicas se registra aparte en B-30.

## Referencias y validación

- [SSM port forwarding a host remoto](https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-sessions-start.html#sessions-remote-port-forwarding), [TLS Npgsql](https://www.npgsql.org/doc/security.html).
- [Parada temporal RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_StopInstance.html), [PostGIS RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Appendix.PostgreSQL.CommonDBATasks.PostGIS.html).
- [OIDC GitHub/AWS](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws), [runners y direcciones IP](https://docs.github.com/en/actions/reference/runners/github-hosted-runners#ip-addresses).
- [Backend Terraform S3](https://developer.hashicorp.com/terraform/language/backend/s3), [Docker y publicación de puertos](https://docs.docker.com/engine/network/port-publishing/).

Ver [[Repositorio de software y versiones]], [[Identidad OIDC y sesiones]], [[Plan de validacion]], [[AWS y Terraform]], [[CI-CD y automatizacion de despliegue]] y [[Hardening y seguridad de servidores]].

## Acceso individual y distribución de secretos — 7 de octubre de 2026

[[Credenciales y acceso del equipo]] concreta el alta SSO/MFA, la lectura de secretos dev por integrante, configuración privada local y retirada/rotación de accesos para ambos repositorios. No compartir un token AWS de Bryan ni un usuario master PostgreSQL. Los permisos individuales deben implementarse con roles/permission sets diferenciados, no con una lectura general de todos los secretos del equipo. La instancia de Identity Center debe soportar acceso a cuentas AWS, no solo aplicaciones.

Al implementar GitHub OIDC comprobar el formato real de `sub`: GitHub documenta IDs inmutables en repositorios nuevos desde el 15 de julio de 2026. Los ejemplos anteriores con `owner/repo` se conservan como referencia, pero la trust policy debe usar el subject exacto del repositorio/environment real. Ver la fuente y los criterios B-35/V-32 en la guía; configuración AWS y pruebas siguen pendientes.

## Escenario temporal y retiro al finalizar — 7 de octubre de 2026

Bryan confirma uso exclusivamente para la hackathon y eliminación de recursos del proyecto al terminar. [[AWS temporal para la hackathon y cierre]] tiene precedencia en acceso humano y vida útil: cuenta independiente, propuesta de usuarios IAM/MFA con `aws login` para conservar créditos, sin Organizations; retirar también bootstrap/state cloud después del destroy y exportación privada local. Durante el ensayo se conservan red/TLS/aislamiento/backups/protección RDS. El servidor n8n propio y recursos ajenos al proyecto quedan fuera de la limpieza. B-35/V-32 se adaptan al login temporal; B-36/V-33 incorporan cierre completo.

