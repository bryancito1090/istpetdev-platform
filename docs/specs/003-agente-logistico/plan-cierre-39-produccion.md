# Plan de ejecución y cierre de #39 — agente n8n en producción

Preparado y ampliado el **14-oct-2026**, por solicitud de Bryan, para entregar a
Antigravity. Estado: **plan preparado; operación de producción no ejecutada en
esta revisión**. Sustituye el plan preliminar de este archivo; conserva como
antecedentes la entrega y revisión de #37. Las casillas siguientes representan
trabajo por ejecutar o verificar, no resultados.

- Issue: [#39](https://github.com/bryancito1090/istpetdev-platform/issues/39).
- Base inspeccionada: `develop` @ `8c99a71c5a0017e769af4d7e40666565d684117d`.
- Rama preparada: `chore/39-n8n-produccion`.
- Seguimiento obligatorio: [task.md](task.md), T9 (corrida oficial) y T10
  (registro); contratos: [spec.md](spec.md), [plan.md](plan.md) y
  [test-e2e.md](test-e2e.md).
- H5: **15-oct-2026 14:00 America/Guayaquil**, equivalente a 19:00 UTC.

## Estado de revisión del checkpoint — 14-oct-2026

Antigravity entregó el checkpoint y dejó de editar. Codex conservó la entrega
original en `7b58243` e integró develop @ `51c3b30` en `067dc62`, sin conflictos
de contenido. La versión de workflows/prompt no cambia; sus hashes coinciden
con #74. El informe de entrega se encuentra en
[entrega-39-antigravity.md](entrega-39-antigravity.md).

PR #78 ya fue fusionado y su CD del SHA `9d1a3d9` terminó exitosamente. Esa
release no incorpora el transporte preparado aquí. El paso siguiente es el
PR de este checkpoint a develop y otra promoción autorizada a main; no editar
manualmente la configuración del host para eludir ese orden.

Correcciones de Codex: client ID M2M explícito, transporte de las nueve claves
de configuración del agente tanto en deploy como en rollback, HMAC desde el
Secret de GitHub, default false y rechazo de activación sin configuración.
Se retira el fallback defectuoso de lectura de .env. Las variables humanas
se conservan. `restart_only` no aplica nuevas variables; necesitan recreación.

El PR del checkpoint puede quedar listo para revisar su software si sus pruebas
locales y CI pasan, **referenciando #39 sin cerrar la issue**. Esto evita exigir
aceptación productiva antes de integrar el código necesario para desplegarla.
Los pasos 1–8 y PA-01/CE siguen pendientes según evidencia; un PR que pretenda
acreditar la aceptación completa permanece borrador hasta comprobarla.

Fuentes de CD (Variables GitHub salvo el HMAC): COGNITO_M2M_CLIENT_ID,
AUTHENTICATION_TECHNICAL_READ_SCOPE, AGENT_ENABLED, AGENT_AUTO_ON_INCIDENT,
AGENT_TIMEOUT_SECONDS, AGENT_POLL_MILLISECONDS, AGENT_N8N_WEBHOOK_URL,
AGENT_N8N_API_BASE_URL y Secret AGENT_N8N_WEBHOOK_SECRET. `.env.example`
contiene placeholders y defaults públicos. No configurar el agente como true
hasta disponer de cuenta técnica, credenciales remotas y webhooks publicados.

## 1. Resultado que debe entregar Antigravity

Dejar el agente del servidor de Bryan conectado a la API de producción con
DeepSeek y OAuth2 M2M, importado desde un SHA identificable, con Schedule
apagado, firmas rechazadas antes del modelo, evidencia de Completed y Fallback,
matriz oficial de aceptación y ensayo del incidente. Registrar también cada
fallo o dependencia sin resolver. El resultado se revisa con evidencia, no con
una afirmación de que «el agente funciona».

Antigravity ejecuta y documenta dentro del alcance de #39. **Codex revisa la
entrega, resuelve conflictos, hace los commits/push necesarios y abre el PR a
`develop`** cuando Bryan retome esta sesión. Antigravity no hace push, no abre
PR, no fusiona ramas y no cierra #39. La solicitud actual no establece un job
programado ni una publicación automática al aparecer un archivo.

La promoción `develop → main`, la configuración de la API compartida y el
redeploy se coordinan con Jorge. No son una consecuencia automática de abrir
el PR de #39. Preparar primero el cambio concreto y su validación; la release
sigue el procedimiento del runbook y la coordinación del responsable. El
ensayo que resetea producción necesita una ventana expresamente acordada con
el equipo. Una dependencia pendiente no impide avanzar las tareas independientes.

### Reparto de trabajo

| Responsable | Entrega |
|---|---|
| Antigravity | Inventario, configuración de n8n dentro del acceso autorizado, pruebas reales, documentación y entrega a Codex |
| Bryan | Acceso al servidor y cuenta DeepSeek, presupuesto, decisiones de su entorno y coordinación de la ventana |
| Jorge / #38 | Variables y secretos de API, cuenta técnica, respaldo de la base compartida, release y redeploy |
| Anthony | Consulta de integración del backend de #36; #37 ya está integrada, no se le reasigna su implementación |
| Codex | Revisión final, pruebas pertinentes, integración con develop, resolución de conflictos, push de rama y PR |
| Equipo | Ventana exclusiva, decisión si falla DeepSeek o CE-03, excepciones al congelamiento si se acuerdan |

## 2. Punto de partida comprobado y límites

La inspección fue de Git, documentación, informes locales y GitHub; no del
runtime de producción ni de sus secretos.

| Área | Evidencia existente | Lo que falta acreditar para #39 |
|---|---|---|
| #36 | Cerrada; backend en develop, PR #40 | Backend y migraciones efectivos en producción |
| #37 | Cerrada; [PR #74](https://github.com/bryancito1090/istpetdev-platform/pull/74) fusionado el 14-oct, CI de backend/frontend/política/bóveda en verde | Importación de los JSON de ese SHA en el servidor y funcionamiento real |
| Release | `origin/main` @ `fac9c6e6f7f2a8b96e52a701e36424d213eb8a7b`; último job de despliegue consultado exitoso para ese SHA | main y develop divergían: 67 commits por delante y 1 por detrás en develop; #37 no estaba promovida. Reconsultar al ejecutar |
| n8n | Comentarios de Bryan en #37/#39: 2.39.5, nodo nativo DeepSeek, digest coincidente local/remoto | Configuración del servidor reportada como `latest`: fijar sin cambiar la versión; revalidar runtime |
| M2M | #38 registra apply dirigido, cliente `7h8pau7hu9pa40k85idbi7rgn4`, token y 200/403 locales | Secrets Manager, cuenta técnica y permisos en producción, transporte de variables por CD |
| Compose/CD | En el SHA inspeccionado faltan las variables Agent y M2M en Compose de producción y su transporte en deploy.yml | Jorge debe completar #38 o entregar el cambio concreto para revisión |
| Aceptación anterior | Informe inicial local: p95 9127 ms, herramientas 60/60, LLM 48/50, inyección 10/10 | Corresponde a una entrega anterior; no acredita el árbol final ni producción |
| Último informe local encontrado | `artifacts/agent-acceptance/20261014-160708-465112/report.md`, commit `4742f3b`: herramientas 60/60, p95 **10001 ms**, LLM 47/50, inyección 10/10 | El umbral de latencia falló. Conservarlo y repetir la aceptación oficial; no afirmar que la última matriz pasó |
| Incidente | task.md describe un intento antiguo con una sola ruta afectada | CE-03 exige dos decisiones, una automática y una escalada; recalibrar/verificar con el backend actual |
| GitHub | #39 tiene label enhancement y no tiene milestone; no existen labels type::* ni milestones en el repo consultado | Conciliar metadatos del flujo institucional con la política real de GitHub antes de publicación, sin inventar un hito |

El cuerpo del PR #74 y varios registros todavía describen «matriz en ejecución»
o «PR en preparación». Tratar esas frases como antecedentes, no como evidencia
final. Al cerrar #39 actualizar el estado vigente conservando intentos anteriores.

## 3. Lecturas, accesos y alcance antes de operar

### Lectura obligatoria

1. README.md, docs/boveda/README.md y Hechos canónicos.
2. Este plan, spec.md, plan.md, task.md, test-e2e.md y
   [revision-integracion-37.md](revision-integracion-37.md).
3. Bóveda: n8n e IA, Identidad OIDC y sesiones, Contratos API y eventos,
   Runbook de DevOps y base de datos para la demo, Credenciales y acceso del
   equipo, Seguimiento de implementación y pruebas y Registro de seguimiento
   y hallazgos. Referencias concretas al final del documento.
4. Antes de preparar la release, activar flujos o ensayar públicamente:
   `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` y sus referencias.
   La fuente editable es la nota de Skills de la bóveda; no versionar la copia.
5. CONTRIBUTING.md, plantilla del PR y .github/scripts/branch-policy.sh.
   Aplicar istpet-gitflow adaptando MR a PR: este repo vive en GitHub.

### Inventario de acceso sin mostrar valores sensibles

- [ ] Identificar acceso SSH/administración al servidor **n8n**, distinto del
  EC2 de la **API**. No deducir que ambos usan el mismo host, usuario o Compose.
- [ ] Identificar proyecto Compose, servicio, contenedor, DB/volumen, proxy,
  configuración persistente, runners y dueño. Registrar nombres operativos
  en evidencia privada si contienen datos sensibles.
- [ ] Confirmar sesión n8n con permisos para credenciales, importar y publicar.
- [ ] Confirmar acceso a DeepSeek para revisar presupuesto y credencial.
- [ ] Confirmar canal privado disponible con Jorge para secretos y confirmaciones.
- [ ] Confirmar token humano de Planificador habilitado en producción. Gerencia,
  M2M, ID token y token local no sustituyen ese access token.
- [ ] Confirmar herramientas existentes: git, gh, docker/Compose, curl,
  openssl, python3; revisar `n8n <comando> --help` en la versión fijada.
- [ ] Comprobar `git status`, rama y posibles ediciones simultáneas. No hacer
  reset --hard, clean -fd, stash automático ni sobrescribir trabajo ajeno.

Si falta acceso, registrar exactamente el permiso/canal que falta. No buscar
secretos en historiales, conversaciones ajenas ni archivos fuera del alcance.
Las credenciales se introducen por su mecanismo privado, nunca por el prompt.

### Límites de alcance

Esta issue importa los workflows de #37. No rediseñar el agente, cambiar
proveedor, añadir nodos por comodidad, relajar políticas, generar migraciones,
aplicar Terraform general ni crear recursos cloud como atajo. Ya existe el
cliente M2M: no crear otro para solucionar un 403. Una corrección indispensable
se prepara en repo, se revisa y se importa desde su nuevo SHA aprobado.

## 4. Secuencia de dependencias y decisiones

| Gate | Condición para avanzar | Trabajo que sí puede continuar mientras falta |
|---|---|---|
| G0 | Base/contratos leídos y branch de trabajo correcta | Inventario documental y plan |
| G1 | Versión exacta y acceso al servidor | Revisión de JSON y preparación de evidencia |
| G2 | API de #36/#37 desplegada y saludable; #38 efectiva | Credenciales n8n, SHA de importación, borradores inactivos |
| G3 | Secretos coincidentes, M2M 200/403, firmas 401 | Registro y diagnóstico, con agente deshabilitado |
| G4 | Ventana acordada y Completed/Fallback reales | Pruebas locales y preparación del reporte |
| G5 | Matriz oficial y CE-03 medidos, documentación coherente | Entrega parcial explícita si hay bloqueo |
| G6 | Revisión Codex, conflictos resueltos, CI del SHA final | PR borrador si falta evidencia de producción |

Si #38 necesita código todavía no integrado, el orden evita un bloqueo circular:
preparar el diff y pruebas → entregar un checkpoint a Codex/Jorge → integrar
por PR a develop → release coordinada a main → retomar los pasos de producción.
No intentar terminar #39 alterando solo variables temporales del contenedor.

## 5. Contrato exacto del SHA integrado

### Imagen y modelo

- n8n acordado: **2.39.5**.
- Imagen verificada previamente: `docker.n8n.io/n8nio/n8n:2.39.5`.
- Digest reportado: `sha256:cfa04788a34ae6f5d59d9086c5761315be2457a9608969553bd0add9a94959d1`.
- Nodo: `@n8n/n8n-nodes-langchain.lmChatDeepSeek`, nativo.
- Modelo configurado en los exports: `deepseek-chat`; prompt: `v1`.
- No cambiar a OpenAI compatible si el nodo nativo ya funciona en esta versión.

La issue usa `n8nio/n8n:<versión>` como ejemplo. El pull documentado usó el
registry de n8n. Registrar registry, etiqueta y digest reales. Si se exige
además comprobar Docker Hub, hacerlo como comprobación separada; no afirmar
que su pull se verificó ni cambiar de imagen por suposición.

### Rutas y workflows

| Archivo | ID exportado | Webhook de producción |
|---|---|---|
| automation/n8n/workflows/vigia.json | `14fb7192-8115-54e9-a348-f2980e20bddf` | `https://n8n.bryan-bano.com/webhook/istpetdev/agent/watch` |
| automation/n8n/workflows/incidente.json | `0754521d-91f4-5dd4-aa53-fd386c25a67b` | `https://n8n.bryan-bano.com/webhook/istpetdev/agent/incident` |

Base para API: **`https://n8n.bryan-bano.com/webhook/istpetdev/agent`**.
N8nTrigger añade `/watch` o `/incident`. No usar `/webhook-test`, no añadir el
sufijo dos veces y no copiar la base ilustrativa incompleta de la issue.

Borradores anteriores reportados: vigia `HO5zltuyBMATIwLT` e incidente
`u5l4WnjD2sPhhvEb`. Identificarlos antes del import; no activarlos junto con los
oficiales ni borrar otros workflows del servidor.

### Credenciales, incluidas herramientas embebidas

| Tipo | Nombre del export | ID del export |
|---|---|---|
| deepSeekApi | IstpetDev DeepSeek | `3b8794f0-9241-5c0c-9e19-c531bed733c8` |
| oAuth2Api | IstpetDev M2M | `8e186867-4916-54b1-b968-c1c273f79a11` |

Las cinco herramientas son nodos **ToolWorkflow** con subworkflows contenidos
en `parameters.workflowJson`; no son cinco HTTP Request Tool simples. Dentro
de esos JSON hay referencias OAuth2 al ID anterior. Además, Send Result y
Start Scheduled Watch referencian la credencial M2M; DeepSeek Chat Model usa
la otra. Coincidir solo en el nombre visible no garantiza que los IDs resuelvan.

Priorizar crear/reutilizar los IDs esperados en el proyecto correcto, usando
el mecanismo seguro que soporte 2.39.5. Si esos IDs colisionan con credenciales
ajenas, no sobrescribirlas. Construir un mapa export-ID → remote-ID y revisar
**todas** las referencias, incluidas las embebidas. Si la adaptación exige
cambiar la definición ejecutable, devolverla al repo para revisión; no dejar
un workflow distinto solo en la UI. Registrar remapeos operativos y separar
metadatos de instancia de cambios de lógica.

## 6. Paso 1 — Fijar y registrar versión de n8n

**Entrada:** acceso administrativo al servidor y evidencia de #37/#39.

1. Identificar el contenedor real. Consultar solo campos permitidos:

   ```bash
   docker exec <contenedor-n8n> n8n --version
   docker inspect <contenedor-n8n> --format '{{.Config.Image}} {{.Image}}'
   docker image inspect <imagen-verificada> --format '{{json .RepoDigests}}'
   ```

   La imagen configurada, el image ID y RepoDigests son datos diferentes.
   No comparar dos tipos de digest como si fueran el mismo.
2. Verificar que runtime sigue en 2.39.5 y el nodo nativo está disponible.
3. Antes de recrear, verificar respaldo vigente de DB/volumen y configuración,
   procedimiento de recuperación y preservación de **N8N_ENCRYPTION_KEY**.
   Un export de workflows no es por sí solo un backup completo de n8n.
4. Cambiar `latest` por la etiqueta exacta acordada en la fuente persistente
   del servidor. No modificar la versión ni borrar volúmenes; no usar down -v.
5. Validar configuración con `docker compose ... config --quiet`, sin imprimir
   el Compose renderizado. Coordinar recreación/reinicio del único servicio
   afectado y cualquier impacto sobre workflows ajenos.
6. Releer versión/imagen y comprobar credenciales accesibles tras el reinicio.
7. Publicar en #39 y #37 una nota saneada con versión, registry, digest, nodo,
   fecha y resultado de fijación. Reutilizar los comentarios anteriores como
   antecedente; no presentar `latest` como una fijación completada.

**Salida:** versión estable, fuente persistente fijada, respaldo identificado
y evidencia pública sin secretos. No actualizar n8n después sin coordinar #37.

## 7. Paso 2 — HTTPS y conectividad de ida y vuelta

1. Verificar HTTPS/certificado y hostname de n8n desde un cliente autorizado.
   No usar `curl -k` para convertir una validación TLS fallida en éxito.
2. Desde el **servidor n8n**, ejecutar:

   ```bash
   curl --fail --silent --show-error      --write-out '\nHTTP %{http_code}\n'      https://istpetdev.doicela.dev/health/ready
   ```

   Esperado: cuerpo saludable y **HTTP 200**. Conservar hora y origen de la
   comprobación; el mismo curl desde la laptop no cumple este paso.
3. Verificar también salida desde el contenedor/runner que hará HTTP hacia la
   API, Cognito y DeepSeek. Si no tiene curl, usar una herramienta instalada;
   no instalar paquetes ni imprimir un token para probar conectividad.
4. Desde el host de la API, comprobar acceso HTTPS a los webhooks publicados
   cuando estén listos; una prueba sin firma debe llegar a n8n y recibir 401.
5. Revisar sincronización de reloj en API y n8n: la firma admite ±60 s.
6. Revisar proxy: origen/base pública correctos, POST/body/headers preservados,
   sin redirecciones inesperadas y sin páginas HTML de challenge en callbacks.
   No desactivar protecciones globales de Cloudflare para pasar una prueba.
7. Registrar salida saneada del curl en #39; distinguir host, contenedor y laptop.

**Salida:** n8n HTTPS válido y comunicación entre los procesos reales. Un 200
de readiness acredita salud de API/DB, no autenticación M2M ni agente operativo.

## 8. Paso 3 — Credenciales, HMAC y entorno de n8n

### DeepSeek

- [ ] Crear/enlazar IstpetDev DeepSeek, tipo deepSeekApi, clave cargada solo en
  el almacén cifrado de n8n; conservar una credencial existente válida.
- [ ] Confirmar endpoint y modelo de los exports, y disponibilidad de la cuenta.
- [ ] Verificar **límite de gasto efectivo** en la cuenta y quién lo controla.
  Guardar evidencia sin clave, saldo personal ni datos de pago. Si la cuenta
  no ofrece un límite duro, registrar esa limitación y resolver con Bryan;
  una alerta o una afirmación sin evidencia no acredita el requisito.

### OAuth2 de Cognito

Configurar IstpetDev M2M, tipo oAuth2Api, con:

| Campo | Valor / origen |
|---|---|
| Grant type | clientCredentials |
| Access token URL | COGNITO_TOKEN_URL entregada por #38, terminada en /oauth2/token |
| Client ID | Cliente existente de #38; revalidar el ID registrado |
| Client secret | Canal privado / Secrets Manager, nunca Git |
| Scope | `istpetdev-api/automation` |
| Autenticación del cliente | Header (Basic), conforme a la credencial local validada |

No añadir openid, scopes humanos ni roles elevados. Confirmar permisos del
proyecto n8n para el workflow y sus herramientas embebidas. Reutilizar el token
hasta su expiración mediante la credencial; no persistirlo en nodos Set.

### HMAC y entorno

1. Reutilizar el HMAC ya acordado si existe. Si todavía no hay uno, generar
   32 bytes aleatorios como hexadecimal, sin mostrar la salida en el transcript:

   ```bash
   # Ejecutar en terminal privada; archivo fuera del repo y de sus artefactos.
   umask 077
   openssl rand -hex 32 > <archivo-privado-del-secreto>
   ```

   Importar su contenido por el mecanismo privado; no incluir la nueva línea
   final en el valor. API y n8n usan la **misma cadena hexadecimal de 64
   caracteres como texto UTF-8**, no decodificarla en un lado como bytes.
2. Configurar de forma persistente, en el proceso donde se usa cada variable:

| Variable n8n | Valor / criterio |
|---|---|
| ISTPETDEV_API_BASE_URL | `https://istpetdev.doicela.dev/api/v1` |
| ISTPETDEV_WEBHOOK_SECRET | Mismo HMAC que API; secreto |
| N8N_ENCRYPTION_KEY | Conservar la clave existente; no reemplazar |
| N8N_BLOCK_ENV_ACCESS_IN_NODE | false, porque estos exports leen $env; solo editores de confianza |
| NODE_FUNCTION_ALLOW_BUILTIN | crypto; sin wildcard; comprobar dónde se configura si hay runner externo |
| GENERIC_TIMEZONE | America/Guayaquil |
| WEBHOOK_URL / proxy | Base pública de n8n según el proxy real; no es la base Agent__N8n__WebhookUrl de API |
| Cookie segura | Conservar HTTPS/cookie segura del servidor; no copiar N8N_SECURE_COOKIE=false del Compose local |

El runner externo requiere revisar su propia configuración; no asumir que
hereda todas las variables del contenedor principal. La allowlist de módulos
puede necesitar `env-override` en el config del runner. Ver
[documentación de runners](https://docs.n8n.io/hosting/configuration/environment-variables/task-runners/).
El acceso a $env amplía lo que pueden leer los editores: revisar miembros y
permisos del proyecto. Ver [seguridad de n8n](https://docs.n8n.io/hosting/configuration/environment-variables/security/).

### Comprobación sin persistir el secreto

El export tiene Load HMAC Key: coloca temporalmente `hmacKey` en un item, y
Verify Signature devuelve solo `{valid, payload}`. Los ajustes de los JSON son
`saveDataErrorExecution=none`, `saveDataSuccessExecution=none`,
`saveExecutionProgress=false` y `saveManualExecutions=false`.

- [ ] Conservar esos ajustes efectivos, revisar ejecuciones/subworkflows y
  no activar pinData, guardar ejecución manual ni logs de debug con ese item.
- [ ] No fotografiar ni exportar el resultado de Load HMAC Key; puede contener
  el secreto incluso si los datos de ejecución no se guardan posteriormente.
- [ ] Revisar redacción de errores OAuth2 y AgentError antes de compartir logs.
- [ ] No exportar credenciales con --decrypted ni imprimir docker inspect completo.

### Nodo temporal de comprobación

Crear un workflow/nodo HTTP temporal, sin publicación ni Schedule, con la
credencial OAuth2 y `GET https://istpetdev.doicela.dev/api/v1/risks`.
Verificar token obtenido y **200**. Registrar solo estado, scope confirmado,
client ID esperado y momento; borrar el nodo/workflow y cualquier ejecución
que contenga credenciales o token.

Verificar **403**, con el mismo M2M, en los cinco cambios prohibidos usando
recursos sintéticos reales de la ventana de ensayo: publicar, aprobar,
rechazar, revertir y cambiar autonomía. Un 404/401 no acredita autorización
correcta. Comparar estado antes/después; si hay 2xx, detener activación y
escalar a #38/#36. Cuando se pruebe con X-Agent-Run-Id válido, conservar la
traza saneada del 403 sin contaminar las muestras de la aceptación oficial.

## 9. Paso 4 — Entrega y configuración efectiva con Jorge / #38

Entregar por canal privado URL base y HMAC. La URL puede registrarse en la
issue; el secreto nunca. Pedir confirmación saneada de #38 con SHA desplegado,
salud, M2M 200/403 y presencia de claves, sin mostrar sus valores sensibles.

### Matriz de configuración de API

| Clave .NET / variable de contenedor | Valor / comprobación |
|---|---|
| Authentication__Authority | Issuer correcto del pool, conservar clientes humanos |
| Authentication__AllowedClientIds__2 | Client ID M2M de #38, o siguiente índice libre |
| Authentication__TechnicalClientIds__0 | Mismo client ID M2M |
| Authentication__TechnicalReadScope | istpetdev-api/automation; revisar TechnicalScope si existe y tiene precedencia |
| Agent__Enabled | false durante preparación; true solo al paso 6 |
| Agent__AutoOnIncident | true para incidente automático |
| Agent__TimeoutSeconds | 10 |
| Agent__PollMilliseconds | 250 |
| Agent__N8n__WebhookUrl | https://n8n.bryan-bano.com/webhook/istpetdev/agent |
| Agent__N8n__WebhookSecret | HMAC coincidente, vía secreto |
| Agent__N8n__ApiBaseUrl | **https://istpetdev.doicela.dev/api/v1** |
| Demo__Enabled | Decisión explícita de la ventana demo; false por defecto actual |
| Demo__Accounts__N__* | Solo si #38 usa la siembra de cuentas; índice libre y cuenta técnica exacta |

**Punto adicional al texto de la issue:** N8nTrigger toma `apiBaseUrl` de
Agent:N8n:ApiBaseUrl; el valor inicial del código es localhost. Las herramientas
usan ISTPETDEV_API_BASE_URL de n8n, mientras el callback Send Result toma la
base del payload firmado. Configurar y verificar **ambas** bases productivas
con /api/v1. Un GET de risks exitoso puede convivir con un callback mal dirigido.

### Siembra de la cuenta técnica y demo

AccountRoleClaimsTransformation exige issuer/client_id, Kind=technical,
Role=Automatizacion, cuenta y organización activas. El token puede ser válido
sin que exista una cuenta y entonces la API no concede permisos.

Program.cs solo llama AccountSeeder si Demo.Enabled y hay Demo.Accounts.
`--provision-accounts` del spec 013 crea **cuentas humanas**; no asumir que
provisiona este M2M. Jorge debe registrar la vía usada para crear/reconciliar
la cuenta técnica sin tocar cuentas ajenas.

La aceptación necesita reset y avance de demo. No activar DEMO_ENABLED a
ciegas: acordar qué endpoints habilita, datos sintéticos que borra, cuentas
sembradas y duración de la ventana. No repetir el apply de Cognito como
solución. Si falta una vía de provisionamiento, preparar el cambio mínimo
revisable con su prueba en coordinación con #38, antes de operarlo.

### Transporte completo de configuración

Si #38 todavía no lo resolvió, preparar con Jorge el diff de:

1. infra/compose/docker-compose.prod.yml: claves del contenedor, defaults
   seguros, agente false por defecto y validación de valores requeridos.
2. .github/workflows/deploy.yml: origen en GitHub Secrets/Variables → env del
   step → lista envs de SSH → proceso remoto → Compose. Nombrar cada secreto
   y variable en documentación, sin incluir valores.
3. Procedimiento de redeploy/rollback: asegurar que un redeploy posterior no
   elimina HMAC ni configuración. Mantener coherencia con rollback.yml.
4. .env.example si hay nuevos nombres: placeholders únicamente.

Una variable en GitHub no llega automáticamente al contenedor. Comprobar los
saltos completos. Usar `config --quiet` y una allowlist que informe solo
«presente/ausente», longitud si aplica y valores públicos. Nunca pegar el
Compose expandido ni listar todas las variables del contenedor.

### Gate de release

- [ ] Confirmar ancestralidad de SHA workflows/backend en main, CI y SHA de
  las imágenes que corren. El éxito de un deploy antiguo no acredita #37.
- [ ] Coordinar la conciliación main/develop (había un hotfix exclusivo de
  main) y PR develop → main con Jorge. No resetear main ni promover esta rama
  chore directamente a producción.
- [ ] Registrar migraciones reales aplicadas y respaldo con el responsable;
  no generar DDL nuevo para una issue de entorno.
- [ ] Tras el deploy de preparación, health/ready 200 y M2M 200/403 reales;
  Agent.Enabled continúa false hasta tener flujos publicados y firmas probadas.

## 10. Paso 5 — Importación oficial y publicación controlada

### Fuente de importación

Usar por defecto el SHA del PR #74:
`8c99a71c5a0017e769af4d7e40666565d684117d`. Confirmarlo en #37 antes de operar.
Si hay una corrección aprobada posterior, usar su SHA y registrar el cambio;
no mezclar un JSON antiguo con el otro regenerado del working tree.

- [ ] Obtener ambos JSON **desde el commit**, por `git show <SHA>:<ruta>` o
  mecanismo equivalente, en un directorio privado de staging.
- [ ] Calcular SHA-256 de ambos archivos y del prompt; guardar hashes y SHA
  completo en un manifiesto sin secretos.
- [ ] Comprobar reproducibilidad con build-workflows.py --check sobre la fuente
  correspondiente; no importar outputs modificados sin commit.
- [ ] Consultar objetos existentes y colisiones de ID antes de importar. Respaldo
  previo del estado de los dos workflows y mapeo de credenciales, sin secretos.

### Importar y publicar en 2.39.5

Importar solo vigia/incidente en el proyecto de Bryan con credenciales resueltas.
Los JSON traen active=true, pero no demuestra publicación efectiva. Comprobar
el estado importado y usar publicación individual del workflow revisado.

La CLI documenta `publish:workflow --id=...` y `unpublish:workflow --id=...`;
los cambios de publicación por CLI necesitan reinicio para afectar al proceso
ya en ejecución. Importar IDs existentes puede sobrescribirlos. Confirmar
flags disponibles con --help y coordinar el reinicio en el servidor compartido.
No usar operaciones --all para publicar/despublicar flujos ajenos. Fuente:
[CLI de n8n](https://docs.n8n.io/hosting/cli-commands/).

- [ ] Importación inicial controlada, sin habilitar automatizaciones hasta revisar.
- [ ] Enlazar DeepSeek, callback OAuth2 y las cinco herramientas embebidas.
- [ ] Revisar contexto firmado: runId/deadline/incidentId no los elige el modelo;
  origen HTTP fijado por entorno. Conservar guardas de deadline y parser.
- [ ] Vigia: nodo Schedule (disabled) sigue con disabled=true y sin cron efectivo.
- [ ] Publicar únicamente ambos workflows y verificar rutas reales de producción.
- [ ] Despublicar/archivar solo borradores de IstpetDev que colisionen; no borrado
  masivo. Confirmar que no hay dos disparadores para el mismo evento.
- [ ] Revisar la **versión publicada**, no solo el borrador, contra los archivos
  de origen. Comparar nodos/parámetros/conexiones/ajustes, permitiendo solo
  metadatos de instancia y remapeos declarados de credenciales.
- [ ] Registrar versionId remoto, ID remoto, hash de origen y diferencias
  operativas. Export saneado sin pinData, ejecuciones ni credenciales literales.

### HMAC: prueba negativa antes de activar API

Probar en **ambos** webhooks:

| Caso | Resultado esperado |
|---|---|
| Firma inexistente / formato inválido | 401; ningún modelo/herramienta |
| Timestamp actual + firma incorrecta | 401; ningún modelo/herramienta |
| Firma correcta + timestamp más antiguo de 60 s | 401; ningún modelo/herramienta |
| Cuerpo cambiado después de firmar | 401; ningún modelo/herramienta |

Las pruebas firmadas se hacen desde un helper privado que lee el secreto sin
mostrarlo, calcula HMAC-SHA256 sobre `timestamp + '.' + bytes exactos del body`
y publica esos mismos bytes. No guardar firma completa ni headers secretos.
Si se ejecuta n8n para evaluar/rechazar el webhook, eso no es una ejecución
válida del **agente**: verificar ausencia de AI Agent y de herramientas.

Un 404 significa que falta la ruta publicada; un 202 con firma inválida es un
fallo. No seguir a aceptación ante esos resultados. El caso positivo firmado
se acredita mediante el disparo real de la API en el paso 6, con un run válido.

## 11. Prelaunch del entorno expuesto

Antes de activar/ensayar, registrar los controles pertinentes de la skill local
para el mismo SHA, proxy, DB e imagen. Reutilizar evidencia vigente de Jorge y
spec 000/#14; la evidencia local de #37 no acredita el servidor.

| Control | Qué comprobar / reutilizar | Estado inicial de este plan |
|---|---|---|
| Arquitectura DB / RLS | ADR-21 aceptado: organización fija y sin RLS en demo. Verificar entorno real; Schema95 es diseño archivado | Pendiente de runtime; RLS no aplica si se confirma ADR-21 |
| Roles/asignación | M2M limitado, rol Planificador y permisos humanos conservados; cuenta/organización activas | Pendiente producción |
| Contenido/campos | HMAC, contexto firmado separado del modelo, cifras y DriverMessage validados, cinco herramientas permitidas | Código revisado en #37; runtime pendiente |
| Secretos/ejecuciones | Almacén cifrado, clave existente, ausencia de secretos en logs/export/items persistidos | Pendiente servidor |
| Proxy/TLS/SSH | HTTPS final válido, VerifyFull RDS, Swagger restringido, postura SSH del procedimiento vigente | Reutilizar reporte vigente de Jorge o registrar faltante |
| Dependencias | CI/auditorías del SHA/lockfiles; reporte del digest n8n/base si disponible; sin upgrade rutinario | CI de #74 verificado; escaneo de imagen no acreditado |
| Datos/plantillas/archivos | No añade archivos ni plantillas administrables; fixtures sintéticos. Reutilizar controles ya expuestos de la app | No aplica a nuevas funciones de #39, con justificación; controles globales según release |

Guardar un registro por control: fecha, revisión, entorno, verificado/fallo/
pendiente/no aplica, evidencia, riesgo y acción. Si hay un fallo o pendiente
explotable de acceso, contenido o dependencia crítica/alta accesible, no
presentar la release/demo como lista. No ampliar la tarea a RLS/grants/servicios
ajenos para completar la tabla.

## 12. Paso 6 — Completed y Fallback reales

**Entrada:** G2/G3, prelaunch registrado, cuenta Planificador, ventana acordada.

1. Jorge activa Agent__Enabled=true por configuración persistente/redeploy.
   Verificar Agent__AutoOnIncident=true y salud después. Registrar SHA de API,
   image digest, SHA de workflows y versión n8n por separado.
2. Preparar datos sintéticos sin otras ejecuciones en curso. Con Planificador:

   ```http
   POST https://istpetdev.doicela.dev/api/v1/agent/runs
   Content-Type: application/json
   Authorization: Bearer <cargado-solo-por-canal-privado>

   {"kind":"Watch"}
   ```

3. Medir tiempo de pared desde envío hasta respuesta; después consultar el
   run para confirmar persistencia. Esperado: HTTP 200, Completed y ≤10 s,
   model=deepseek-chat, promptVersion=v1, n8nExecutionId no vacío, herramientas
   en orden/códigos/latencias y decisiones con explicación y DriverMessage.
4. Revisar logs saneados/callback si hay Fallback: reason, 401/403, URL,
   credential-ID, reloj, Deadline y latencia del proveedor. No reintentar sin
   límite ni editar el prompt en la UI para ocultar el fallo.
5. **Despublicar solo vigia temporalmente**, confirmar que su webhook realmente
   está detenido y repetir Watch. Esperado: Fallback, ≤11 s al panel, motivo
   coherente (normalmente n8n_unreachable), propuesta determinista y Template.
   Agent.Enabled se mantiene true: agent_disabled no prueba el workflow caído.
6. Reactivar vigia incluso si falla la prueba, verificar Schedule apagado y
   hacer un Watch de recuperación. Usar un procedimiento con limpieza/finally
   o dejar explícita la obligación de restauración antes de salir.
7. Guardar respuesta/traza saneada **antes del siguiente reset**: demo/reset
   elimina AgentRun y decisiones; no bastan enlaces a IDs que ya no existirán.

No simular proveedores lentos ni enviar callbacks falsos en producción para
repetir la suite local. El requisito de caída se acredita con el workflow
real despublicado; carreras y resultados tardíos se reutilizan de las pruebas
locales pertinentes salvo que cambie ese código.

## 13. Paso 7 — Aceptación oficial CE-01 (30 muestras)

### Antes de la ventana

- [ ] Coordinar inicio/fin con el equipo; registrar conformidad y responsable.
- [ ] Jorge identifica respaldo vigente y recuperación de datos; no crear
  snapshots nuevos repetidos como efecto de cada corrida.
- [ ] Confirmar que la base es de demo sintética y que reset/advance están
  habilitados por la decisión de Demo.Enabled, no por un bypass improvisado.
- [ ] Revalidar token humano, rol y expiración con margen para toda la matriz.
- [ ] Confirmar flujos publicados, Schedule apagado y presupuesto disponible.
- [ ] Probar preparación de incidente con el backend actual antes de las 30
  muestras: rutas publicables, dos paradas completables, cierre-norte con run
  **automático**. No atribuir al automático un run creado manualmente.
- [ ] Fijar la imagen remota real en N8N_IMAGE al correr el harness desde la
  laptop: el script puede inspeccionar el contenedor local o usar un fallback
  de imagen y eso no verifica el servidor.

### Ejecución con token fuera de argumentos

El script actual admite **ISTPETDEV_PLANNER_TOKEN**. No tiene --token-file;
no inventar esa opción. En una terminal privada con xtrace apagado:

```bash
(
  set +x
  read -r -s -p 'Access token de Planificador: ' ISTPETDEV_PLANNER_TOKEN
  printf '\n'
  export ISTPETDEV_PLANNER_TOKEN
  export N8N_IMAGE='docker.n8n.io/n8nio/n8n:2.39.5'
  acceptance_rc=0
  scripts/agent/acceptance.sh     --base-url https://istpetdev.doicela.dev     --runs 10 || acceptance_rc=$?
  unset ISTPETDEV_PLANNER_TOKEN
  exit "$acceptance_rc"
)
```

No pegar el token en un tool call, issue ni chat. La variable evita ponerlo
en argv/historial; no lo vuelve público ni permite compartir el entorno.

### Lo que el harness hace realmente

- Ejecuta un **warmup adicional** con reset y Watch, fuera de las 30 muestras;
  considerar ese reset, gasto y run al planificar la ventana.
- Hace 10 Watch + 10 Incident + 10 Injection, con reset/preparación por intento.
- Incidente: reset demo-region, proponer/publicar rutas, avanzar dos paradas
  de la primera ruta y registrar `POST /api/v1/incidents` usando el fixture.
  El preset ilustrado en la issue no es el request del harness integrado.
- Inyección: usa demo-region-injection y compara política/facts contra baseline.
- Guarda report.md y runs.json en artifacts/agent-acceptance/<fecha-hora>/,
  ignorado por Git. No detener el proceso ante un primer fallo perdiendo el reporte.
- `harnessMs` incluye preparación/reset; no equivale al tiempo de respuesta del
  POST del panel. Medir ese tiempo aparte en el paso 6 y controles del ensayo.
- Para un Fallback/error asigna una muestra de al menos 10001 ms al p95.
  Un fallback aislado puede no ocupar el percentil 95 de 30 muestras: reportar
  su número y motivo aunque el p95 agregado todavía pase. No alterar el cálculo
  ni los umbrales para encubrir resultados; aclarar el límite de la medición.

### Verificar reporte, no solo exit code

| Métrica | Umbral oficial | Evidencia a registrar |
|---|---|---|
| Herramientas válidas | 2xx / llamadas totales ≥95% | Numerador, denominador, errores y endpoints |
| Latencia | p95 ≤10000 ms con tratamiento de fallback del harness | p95, muestras, fallbacks, latencia API y tiempos de pared disponibles |
| Explicaciones | Decisiones source=Llm / total ≥90% | Llm/total, Templates y mismatchReason |
| Inyección | 10/10 sin llamadas prohibidas y política igual | Resultado por corrida y comparación baseline |

- [ ] Confirmar exactamente 30 muestras, escenario de cada una, run/execution
  IDs, modelo/prompt y fallos; el warmup no se agrega al denominador.
- [ ] Registrar por separado SHA del **harness**, del **backend desplegado** y
  de los **workflows importados**; el Commit del reporte identifica el checkout
  que lo ejecutó, no automáticamente el backend remoto.
- [ ] Conservar output/exit code y hash del report.md; revisar y sanear antes
  de adjuntarlo. No subir runs.json sin revisar su contenido completo.
- [ ] Adjuntar report.md/resumen completo en #39 mediante mecanismo soportado
  por GitHub; si no admite el adjunto, publicar el Markdown íntegro saneado o
  enlace privado durable accesible al equipo. No usar enlace público de Orca.
- [ ] Si falla, conservar el informe fallido, diagnóstico y siguiente acción.
  No cambiar el resultado del archivo original; nueva corrida = nuevo artefacto.

Si falla un umbral, registrar CE-01 **falló/pendiente**, dejar operación
segura por fallback mediante el procedimiento acordado y pedir la decisión
del equipo sobre D4. Habilitar fallback no equivale a aceptación de DeepSeek.
Toda corrección de prompt exige nuevo SHA/versionado, importación y **matriz
completa**; nunca repetir solo las corridas que fallaron.

## 14. Paso 8 — Incidente en vivo y resolución de CE-03

CE-03 es independiente del porcentaje de explicaciones del reporte. Una
matriz exitosa con una sola ruta afectada no prueba el incidente de dos decisiones.

1. Restaurar demo-region dentro de la misma ventana y preparar las rutas y
   paradas conforme al contrato actual de 002/004. Confirmar datos publicados
   y avance real, conservando paradas completadas y reservas.
2. Registrar cierre-norte usando el endpoint/fixture de la versión desplegada.
   Comprobar runId de creación automática; consultar ese run, no iniciar otro.
3. Consultar impacto, decisiones y revisiones. Exigir:

   - Dos rutas afectadas y exactamente dos decisiones correlacionadas.
   - Una AutoExecuted/publicada y una Escalated/pendiente de decisión humana.
   - Explicación en español, facts y DriverMessage ≤160 caracteres en ambas.
   - Una replanificación por ruta/transición; sin duplicados ni pérdida de
     paradas completadas. El agente nunca llama publish/approve directamente.

4. Guardar fecha/hora, API/workflow SHA, n8n/versionId, run/execution IDs,
   resultados, tiempos y respuestas saneadas antes de resetear de nuevo.
5. Registrar el incidente en **E2E-02** y referenciarlo desde el registro de
   **E2E-07** solicitado por #39. No confundir IDs del spec 013 con el spec 003.

### Si solo hay una ruta o ambas decisiones son del mismo tipo

- Registrar CE-03 fallido con affectedRoutes, outcome y preparación concreta.
- Revisar asignación de demanda, capacidad, paradas, corredor del incidente y
  política actual; distinguir preparación incorrecta de un fixture descalibrado.
- Preparar con responsable de 002 el ajuste mínimo reproducible de fixture/
  preparación y prueba determinista, documentando el contrato en specs/bóveda.
  No hardcodear la segunda decisión en n8n ni bajar los umbrales de la política.
- Si cambia fixture/contrato usado por acceptance, requerir revisión, release
  e informe completo nuevos. Conservar el fallo anterior. Si no se resuelve
  antes de H5, entregar el bloqueo: #39 no satisface su cierre completo.

## 15. Registro, documentación y criterios de cierre

### Archivos que se espera actualizar

| Archivo | Cambio autorizado esperado |
|---|---|
| task.md | Avances de T9/T10 separados por código/local/real/producción; pendientes y responsables |
| test-e2e.md | Filas reales E2E-01/02/04/07; Completed/Fallback, HMAC, aceptación e incidente |
| spec.md | PA-01 resuelta solo con credenciales remotas y uso verificado; estado y CE medidos |
| Este plan | Casillas/resultados/URLs de evidencia y entrega final |
| Bóveda n8n e IA | Hosting efectivo, SHA, modelo/prompt y resumen medido, incluidos fallos |
| Bóveda Registro de seguimiento y hallazgos | Estado de #39, bloqueos y decisión del equipo |
| Runbook demo | Estado real de #38/#39 y release; no declarar una tarea de Jorge por hecho sin evidencia |
| Bóveda Fuente y control documental | Registrar cambios relevantes según plantilla de PR |
| Compose/CD/ejemplos | Solo si #38 requiere el diff mínimo coordinado; validar antes de release |

No cambiar la fuente del prompt, templates ni workflows para una importación
operativa exitosa. Si fue indispensable corregir lógica, explicar por qué,
contrastar el cambio con la bóveda y repetir las pruebas/aceptación afectadas.

### Plantilla de evidencia por ejecución

| Campo | Contenido |
|---|---|
| Fecha | ISO-8601 con -05:00 o UTC y zona explícita |
| Entorno | Producción: API HTTPS + n8n del servidor de Bryan + DeepSeek real |
| Revisión | Tres SHA completos (API/workflows/harness), versión n8n y digest |
| Escenario | E2E del spec 003, paso de #39 y requisito/CE |
| Acción | Preparación y request, sin autorización/secreto |
| Resultado | Estado, código HTTP, tiempos, decisions/outcome/source/mensajes |
| Proveniencia | runId, n8nExecutionId, promptVersion y modelo |
| Artefacto | Ruta privada, hash y URL de resumen/adjunto saneado |
| Estado | pasó/falló/bloqueado/no ejecutado; causa y responsable del pendiente |

PA-01 se resuelve si ambas credenciales existen y son utilizables en el
servidor, con prueba M2M y ejecución DeepSeek; no acredita por sí sola CE-01.
No marcar el spec entero completado por resolver esa pregunta.

### Lista final de cierre de #39

- [ ] Versión exacta informada, fijada y estable; registry/digest/nodo documentados.
- [ ] #36/#37 y configuración de #38 desplegadas, con SHA/imágenes comprobados.
- [ ] Workflows del SHA oficial importados y publicados, sin divergencias de lógica.
- [ ] Credenciales remotas enlazadas, HMAC válido/rechazo inválido, Schedule apagado.
- [ ] M2M: risks 200 y cinco acciones prohibidas 403 sin efectos.
- [ ] Watch Completed ≤10 s y Fallback con workflow parado ≤11 s; recuperación.
- [ ] Matriz oficial adjunta y cuatro umbrales medidos; fallos registrados igual.
- [ ] CE-03 de dos decisiones medido; E2E-02/07 registrados con fecha/SHA/entorno.
- [ ] PA-01 resuelta con evidencia y T9/T10/bóveda coherentes.
- [ ] Ningún secreto/dato personal en repo, issues, capturas, artefactos compartidos.
- [ ] Prelaunch vigente, restauración de estado de demo y nota de congelamiento.

Si falta cualquiera, declarar entrega parcial o bloqueada; no cerrar la issue.
Enlazar ALK existente si el equipo usa Linear, sin crear ocho issues nuevas.

## 16. Verificación local de los cambios entregados

Para documentación: revisar enlaces, coherencia y `git diff --check`; correr
check_vault.py si cambia la bóveda. No ejecutar la matriz de producción durante
la revisión local sin ventana. La preparación de este plan no requiere levantar
la aplicación ni repetir la suite completa de #37.

Si hay cambios de configuración/scripts/código, ejecutar las pruebas pertinentes:

```bash
python3 -I scripts/agent/build-workflows.py --check
python3 -I -m unittest discover -s tests/scripts -p 'test_*.py'
bash -n scripts/agent/acceptance.sh scripts/agent/local-up.sh
bash .github/scripts/branch-policy.sh pr   chore/39-n8n-produccion develop   bryancito1090/istpetdev-platform bryancito1090/istpetdev-platform
python3 docs/boveda/.github/scripts/check_vault.py
```

Pruebas de agente/auth si cambia su código o la integración las afecta:

```bash
dotnet test tests/IstpetDev.Tests   --filter 'FullyQualifiedName~AgentJourneyTests|FullyQualifiedName~Integration.Agent|FullyQualifiedName~WorkflowSecretsTests|FullyQualifiedName~N8nTriggerTests'
```

Comprobar previamente DB de pruebas aislada; no ejecutar tests/migraciones
contra RDS compartido por un .env accidental. CONTRIBUTING exige `npm run check`
antes de pedir revisión; Codex lo ejecutará cuando el tipo de cambio lo requiera,
y comprobará los checks de GitHub sobre el SHA final. Auditoría/secrets scan:
usar las herramientas y compuertas existentes, registrar falta de herramienta;
un build y git diff --check no son un escaneo de secretos.

## 17. Entrega de Antigravity a Codex

1. Actualizar este plan y task.md durante cada avance. Dejar evidencia real y
   pendientes, sin convertir las casillas planeadas en resultados.
2. Preparar `entrega-39-antigravity.md` al concluir
   (crear entonces, no como supuesto resultado ahora) con:

   - Estado: lista para revisión / checkpoint de dependencia / bloqueada.
   - Rama y HEAD, archivos modificados y motivo; cambios externos efectivos.
   - SHA API/workflows/harness, hashes/versionId n8n y credenciales remapeadas
     por identificador, sin secretos.
   - Resumen por paso 1–8: pasó/falló/pendiente y enlace/artefacto.
   - Reporte oficial y métricas, intentos fallidos conservados, CE-03 y PA-01.
   - Pruebas ejecutadas, fallos y condiciones que no se pudieron verificar.
   - Estado final: workflows activos, cron apagado, API enabled/fallback,
     ventana cerrada, datos de demo restaurados y rollback disponible.
   - Dependencias que requieren acción de Jorge/Bryan y cambios listos para
     revisión, sin solicitar credenciales por el prompt.

3. Opcionalmente escribir el marcador local ignorado
   `.git/istpetdev-39-ready.json` con `issue:39`, `branch`, `head`, `status`,
   `deliveryReport`, `acceptanceReport`, `pending` y fecha. Resolver el git-dir
   con `git rev-parse --absolute-git-dir` si se trabaja en otro worktree.
   No guardar valores de secretos. El marcador es señal, no prueba ni trigger.
4. Dejar de editar la rama tras la entrega. Avisar a Bryan para que retome a
   Codex; no hay automatización de #39 habilitada por este documento.
5. Si hay checkpoint de #38/release, pedir revisión de ese checkpoint antes de
   esperar indefinidamente. Conservar tareas operativas pendientes para retomar.

### Prompt de delegación

```text
Resuelve la issue #39 de ISTPETDEV siguiendo íntegramente
 docs/specs/003-agente-logistico/plan-cierre-39-produccion.md.
Trabaja en la rama chore/39-n8n-produccion, desde el checkout de ISTPETDEV.
Lee AGENTS.md, los README, los hechos canónicos, spec/plan/task/test-e2e 003,
la revisión de #37 y la skill local de prelaunch con sus referencias.

#37 ya está integrada mediante PR #74. Importa sus JSON desde un SHA
identificable; no rediseñes workflows ni uses los borradores como prueba del
import oficial. #38 solo acredita M2M local: verifica deploy, cuenta técnica,
transporte completo de variables y permisos de producción. Respeta la
coordinación de Jorge para release/redeploy y la ventana de resets del equipo.
Avanza tareas independientes cuando falte una dependencia; entrega un
checkpoint con cambios concretos si #38 necesita código/release primero.

Fija n8n 2.39.5 preservando datos y N8N_ENCRYPTION_KEY; conserva DeepSeek nativo.
Revisa credenciales embebidas y las dos bases productivas /api/v1: n8n y
Agent__N8n__ApiBaseUrl. Publica solo webhooks, Schedule apagado. Prueba HMAC,
M2M 200/403, Completed <=10 s, Fallback real <=11 s y recuperación.
La aceptación son 30 muestras más warmup, con token por el mecanismo privado
existente. Conserva fallos; el último informe local observado falló latencia.
CE-03 exige dos decisiones (AutoExecuted/Escalated), no solo un reporte verde.
No marques PA-01, CE ni tareas cumplidas sin evidencia real del servidor.

No pongas secretos/tokens en el repo, prompt, issues, logs, capturas o exports.
Actualiza task.md y test-e2e.md por avance, contrasta contratos con la bóveda.
Al terminar crea entrega-39-antigravity.md con estado, SHA, evidencia, métricas,
pendientes y estado remoto final; deja de editar y avisa a Bryan.
No hagas push, PR, merge ni cierres #39: Codex hará revisión, resolución de
conflictos, publicación de rama y PR hacia develop después de tu entrega.
```

## 18. Revisión, conflictos, push y PR — responsabilidad de Codex

La instrucción de Bryan para publicar después de la entrega queda registrada;
no es necesario pedirle otra vez autorización para revisión, conflictos,
commit/push de la rama y creación del PR a develop. La fusión del PR o una
release a main siguen fuera de la entrega solicitada para hacerlo mergeable.

1. Confirmar que Antigravity terminó de editar, leer entrega/task y revisar
   diff/artefactos/estado remoto. Preservar todo trabajo no relacionado.
2. `git fetch origin`; comparar con origin/develop actualizado. Si avanzó,
   integrar en la rama chore mediante merge para conservar historia y evitar
   force-push. No usar ours/theirs de forma masiva.
3. Resolver conflictos por significado, especialmente task/spec/test-e2e,
   notas compartidas, deploy.yml, Compose y posible provisión de cuentas.
   Mantener login real, clientes humanos, ruteo, seguridad y transporte de
   variables de ambas ramas. No generar nuevas migraciones para resolverlos.
4. Si el conflicto modifica lógica, prompt, contratos, fixture o defaults que
   se ensayaron, repetir las verificaciones afectadas y matriz completa cuando
   corresponda; mantener los SHA de producción registrados sin atribuirles
   el nuevo árbol hasta desplegarlo por el flujo autorizado.
5. Revisar secretos, git diff --check, pruebas pertinentes y política de rama.
   Consultar los checks requeridos; no dar por mergeable un PR solo por no
   haber conflictos textuales.
6. Conciliar milestone y label type::chore con las convenciones institucionales.
   Actualmente no existen: usar decisión/hito del equipo, no una fecha inventada.
   En GitHub, registrar limitaciones de metadatos sin afirmar que existen los
   jobs GitLab gitflow:* o gitleaks si el CI real no los implementa.
7. Commit con asunto claro de 10–72 caracteres, por ejemplo:
   `docs(n8n): registrar operación y aceptación de producción #39`.
   Seleccionar archivos explícitos; no git add de secretos/artefactos ignorados.
8. Push **solo** `chore/39-n8n-produccion` y crear PR base=develop con plantilla,
   problema/resultado, SHA de evidencia, validación y límites. Descripción
   multilinea por archivo temporal y gh --body-file, no interpolación de texto.
9. Para el checkpoint de configuración: PR revisable con pruebas/CI verdes,
   pendientes externos explícitos y referencia a #39 sin cierre automático.
   Si pretende acreditar la operación completa y falta evidencia externa,
   mantenerlo borrador. Closes #39 solo cuando todos los criterios de la issue
   tengan evidencia; exigir checks verdes del **HEAD final**.
10. Reconsultar mergeable/mergeStateStatus, CI y avance de develop. Resolver
    conflictos nuevos si aparecen; repetir solo las pruebas afectadas.
    Entregar URL del PR y condiciones pendientes para que se pueda mergear.

## 19. Congelamiento, recuperación y fin de ventana

Después de H5 no cambiar workflows, credenciales, prompt ni versión. Si la
fecha ya pasó al retomar, registrar bloqueo y pedir la decisión del equipo
para la intervención concreta; no atribuir una excepción al plan.

| Fallo | Recuperación prevista |
|---|---|
| Agente/DeepSeek no cumple | Jorge configura Agent.Enabled=false por el mecanismo persistente acordado; API determinista, registrar agent_disabled y salud |
| Webhook caído en ensayo | Restaurar publicación de vigia, comprobar cron apagado, salud y Watch de recuperación |
| Credencial no enlaza | Mantener agente deshabilitado y corregir el mapeo autorizado; no recrear Cognito ni rotar todas las claves |
| Import defectuoso | Restaurar solo versiones previas de los dos workflows desde respaldo; no afectar flujos ajenos |
| Release API falla | Jorge usa rollback oficial por imágenes/SHA y verifica compatibilidad del esquema; revertir código no revierte automáticamente la DB |
| Reset afectó ensayo ajeno | Detener matriz, informar y recuperar mediante responsable/respaldo; no restaurar RDS por iniciativa propia |

Cerrar la ventana: quitar nodo temporal, borrar temporales privados de
credenciales que ya no sean necesarios, verificar estado operativo acordado,
Schedule apagado y datos sintéticos de demo restaurados. No borrar pruebas
fallidas, reports, backups o volúmenes como «limpieza». Un fallback operativo
es un resultado útil, pero no satisface por sí solo CE-01 ni CE-03.

## Referencias del repositorio

- [Spec 003](spec.md), [diseño](plan.md), [tareas](task.md),
  [E2E](test-e2e.md), [revisión de #37](revision-integracion-37.md).
- [Runbook de demo](../../boveda/03%20Arquitectura/Runbook%20de%20DevOps%20y%20base%20de%20datos%20para%20la%20demo.md).
- [n8n e IA](../../boveda/05%20Automatizacion/n8n%20e%20IA.md).
- [Identidad OIDC](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md).
- [Contratos API/eventos](../../boveda/03%20Arquitectura/Contratos%20API%20y%20eventos.md).
- [Prelaunch — fuente editable](../../boveda/08%20Skills/istpetdev-prelaunch.md).
- [Seguridad prelaunch](../../boveda/03%20Arquitectura/Verificacion%20de%20seguridad%20antes%20del%20lanzamiento.md).
- [Seguimiento del equipo](../../boveda/06%20Equipo/Registro%20de%20seguimiento%20y%20hallazgos.md).
- [#38](https://github.com/bryancito1090/istpetdev-platform/issues/38),
  [#37](https://github.com/bryancito1090/istpetdev-platform/issues/37),
  [#36](https://github.com/bryancito1090/istpetdev-platform/issues/36).

#55 se mantiene como coordinación informativa de cuentas humanas. No crear
las seis cuentas ni iniciar su ronda como parte implícita de #39; basta una
identidad Planificador de producción provisionada por la vía acordada del
spec 013 y con MFA. M2M sigue separado de esa identidad humana.
