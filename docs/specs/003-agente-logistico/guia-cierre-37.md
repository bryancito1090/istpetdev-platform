# Cómo completar #37

El estado verificable se mantiene en [task.md](task.md). #37 entrega workflows,
prompt, copia local y aceptación local; #38 provee la identidad M2M; #39 instala
la versión integrada en el servidor y mide la aceptación oficial. No hace falta
cerrar #39 para desarrollar #37, pero sí contar con su versión fijada y con el
cliente de #38 para probar las herramientas reales.

## 1. Identidad M2M de Cognito (#38)

Cuenta AWS comprobada: `747456040427`; región `us-east-1`; user pool
`us-east-1_4Y2j5DINp`. El perfil predeterminado de esta laptop corresponde a otra
cuenta: usar expresamente `istpetdev-admin`.

El código ya contiene el scope y el cliente; **no crear otro cliente a mano en la
consola**. Antes del apply, el paso 2 de #38 exige publicar y revisar el plan.
El plan autorizado debe mostrar solo:

- `aws_cognito_resource_server.api`: actualizar scopes, conservar `access` y
  añadir `automation`. Conservar el nombre desplegado `IstpetDev human API`;
  renombrarlo provoca sustitución con borrado en este proveedor.
- `aws_cognito_user_pool_client.automation`: crear `istpetdev-dev-n8n`, secreto
  generado, `client_credentials`, solo `istpetdev-api/automation`.
- Ningún borrado ni cambio de EC2, red o RDS. Coordinar con #43; si otro operador
  modifica el state, generar un plan nuevo y revisarlo de nuevo.

```bash
export AWS_PROFILE=istpetdev-admin
export AWS_REGION=us-east-1
export AWS_DEFAULT_REGION=us-east-1
export TF_DATA_DIR="$HOME/.config/istpetdev/private/terraform-dev"
private_dir="$HOME/.config/istpetdev/private"
umask 077
terraform -chdir=infra/terraform/environments/dev plan -input=false \
  -target=aws_cognito_resource_server.api \
  -target=aws_cognito_user_pool_client.automation \
  -var-file="$private_dir/dev.tfvars.json" \
  -out="$private_dir/agent-m2m.tfplan"
# Solo después de la revisión del paso 2 de #38:
terraform -chdir=infra/terraform/environments/dev apply "$private_dir/agent-m2m.tfplan"
```

Después del apply, en AWS Console → Amazon Cognito → User pools →
`istpetdev-dev` → Applications / App clients → `istpetdev-dev-n8n`, copiar el
Client ID y el Client secret al `.env` local ignorado. No publicar el secreto ni
el JWT. Guardar el secreto autorizado en Secrets Manager siguiendo #38.

```dotenv
COGNITO_TOKEN_URL=https://istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com/oauth2/token
COGNITO_M2M_CLIENT_ID=
COGNITO_M2M_CLIENT_SECRET=
```

El token usa `grant_type=client_credentials`, autenticación Basic con ID/secreto
y scope `istpetdev-api/automation`. La API también necesita incluir el ID en
`Authentication:AllowedClientIds` y `Authentication:TechnicalClientIds`, aceptar
ese scope y tener una cuenta técnica con subject igual al Client ID y rol
`Automatizacion`. Obtener un token no prueba por sí solo que la API lo autorice.

## 2. Clave DeepSeek para el ensayo local

El servidor ya dispone de `DeepSeek account`, tipo `deepSeekApi`, enlazada a los
dos borradores. MCP devuelve referencias a credenciales, no sus secretos.

Para la copia local, usar la clave de desarrollo disponible o crear una en
[DeepSeek API keys](https://platform.deepseek.com/api_keys). Guardarla como
`DEEPSEEK_API_KEY` en `.env`, con presupuesto de desarrollo controlado. No pegarla
en GitHub/chat ni incluirla en los JSON. El nodo es **DeepSeek Chat Model**,
modelo `deepseek-chat`.

## 3. Probar localmente (#37)

Completar las variables descritas en `.env.example`. La imagen exacta es
`docker.n8n.io/n8nio/n8n:2.39.5`. El secreto HMAC local debe coincidir con el de la
API local; no copiar la clave de cifrado del servidor al entorno local.

```bash
scripts/agent/local-up.sh
```

Ese comando configura solamente n8n. Antes de arrancar la API, comprobar que sus
cadenas de conexión y de migración apuntan al Docker local: la configuración
habitual del repositorio puede apuntar a la base compartida. El script de
aceptación ejecuta reset de demo; usar datos sintéticos en una base local
aislada. Seguir [api-guide.md](../../api-guide.md) para iniciar la API y cargar el
fixture, con `Agent:Enabled=true` y
`Agent:N8n:WebhookUrl=http://localhost:5678/webhook/istpetdev/agent`.

La API debe tener también la identidad M2M del paso 1 y las cuentas demo de
planificación. Antes de iniciar sesión debe existir un usuario humano en Cognito. El 14-oct se comprobó que el pool tenía 0 usuarios: un administrador debe crear el usuario, enviar invitación autorizada y completar contraseña/MFA. El cliente M2M no sirve para el formulario de login. Iniciar sesión en la web local como Planificador usando Cognito.
En las herramientas del navegador → Network, seleccionar una petición privada
a la API y copiar únicamente el valor Bearer de `Authorization`. Introducirlo
en la terminal con entrada oculta, sin pegarlo en el historial:

```bash
read -rsp 'Token del Planificador: ' ISTPETDEV_PLANNER_TOKEN
export ISTPETDEV_PLANNER_TOKEN
scripts/agent/acceptance.sh --base-url http://localhost:5000/api/v1 --runs 10
unset ISTPETDEV_PLANNER_TOKEN
```

El flag `--token` de la issue sigue disponible; la variable de entorno permite
evitar escribir el token literalmente en el comando. Si expira durante el
ensayo, obtener uno nuevo y repetir la corrida completa.

Exigir 30 ejecuciones y los cuatro umbrales: herramientas válidas ≥95%, p95
≤10 s sin fallback como éxito, explicaciones Llm ≥90% e inyección 10/10. El
informe queda en `artifacts/agent-acceptance/<fecha>/report.md`, fuera de Git.
Registrar también el caso n8n detenido → `Fallback (n8n_unreachable)` y el
ensayo CE-06 en la laptop del evento. Actualizar `test-e2e.md` solamente con
fechas, SHA y resultados observados.

## 4. Flujos en tu n8n remoto (#39)

Ya existen como borradores en el proyecto personal indicado por Bryan:

- [IstpetDev Agent - vigia](https://n8n.bryan-bano.com/workflow/HO5zltuyBMATIwLT)
- [IstpetDev Agent - incidente](https://n8n.bryan-bano.com/workflow/u5l4WnjD2sPhhvEb)

Están inactivos. Parámetros, conexiones y comportamiento de nodos coinciden con
el repositorio; el DeepSeek superior está enlazado a la credencial del servidor.
El M2M permanece pendiente, incluida la referencia de los HTTP Request dentro
de los cinco subworkflows de herramienta. No basta con enlazar `Send Result`.
Los borradores no acreditan una importación oficial del SHA de `develop`.

Seguir #39 para fijar la imagen, configurar API base/HMAC y crear la credencial
OAuth2. Mantener el secreto de cifrado ya existente del servidor. El workflow
requiere `N8N_BLOCK_ENV_ACCESS_IN_NODE=false` y el módulo `crypto` disponible
para Code; copiar las opciones correspondientes del Compose local a la
configuración del servidor conservando sus demás ajustes. Limitar la edición
de workflows a administradores de confianza, porque las expresiones acceden a
variables del entorno.

Después de integrar #37, importar exactamente los JSON de ese SHA y resolver
las referencias de credenciales también en los subworkflows. Para preservar
sus referencias M2M, el ID esperado es
`8e186867-4916-54b1-b968-c1c273f79a11` y el nombre `IstpetDev M2M`; se puede
importar esa credencial mediante `n8n import:credentials` con el mismo formato
usado por `local-up.sh`, archivo privado 0600 y limpieza al salir. Los valores
van al almacén cifrado de n8n, nunca al JSON del workflow.

Solo activar los webhooks cuando #38/#39 estén configurados. El Schedule de
vigía queda deshabilitado. La URL base real es
`https://n8n.bryan-bano.com/webhook/istpetdev/agent`: la API agrega `/watch` o
`/incident`. La aceptación de producción hace reset de demo; coordinarla con el
equipo y registrarla en #39.

## 5. Cierre en GitHub

1. En #37 y #39 registrar la versión `2.39.5`. En #38 guardar la evidencia del
   plan revisado y del token/403, sin secretos.
2. Terminar la aceptación **local**, adjuntar su informe/resumen a #37 y
   registrar CE-06 real en `test-e2e.md`. La corrida oficial pertenece a #39.
3. Commit/push de `feature/37-n8n-workflows-local`, PR a `develop`, revisión de
   Bryan y CI verde, incluida `WorkflowSecretsTests`. Indicar los cambios de
   correlación backend y el ajuste Terraform que evita reemplazar Cognito.
4. Tras el merge, comentar el SHA listo para importación en #39. Cerrar #37
   únicamente cuando estén satisfechos sus cuatro criterios; mantener #39
   abierto hasta su validación de producción.

Fuentes: [issue #37](https://github.com/bryancito1090/istpetdev-platform/issues/37),
[issue #38](https://github.com/bryancito1090/istpetdev-platform/issues/38),
[issue #39](https://github.com/bryancito1090/istpetdev-platform/issues/39),
[Cognito token endpoint](https://docs.aws.amazon.com/cognito/latest/developerguide/token-endpoint.html),
[n8n MCP](https://docs.n8n.io/connect/connect-to-n8n-mcp-server/mcp-server-tools-reference.md).
