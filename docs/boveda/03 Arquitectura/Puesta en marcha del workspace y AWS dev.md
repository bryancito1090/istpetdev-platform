---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-07
tags: [arquitectura, equipo, workspace, aws, guia]
---

# Puesta en marcha del workspace y AWS dev

> **Actualización posterior del 7 de octubre:** Bryan quiere aprovechar sus créditos en una cuenta independiente y eliminar la infraestructura al terminar la hackathon. Leer primero [[AWS temporal para la hackathon y cierre]]. Esa nota sustituye los pasos de Identity Center/Organizations/permission sets por usuarios IAM individuales con MFA y `aws login` (CLI >=2.32.0), añade compatibilidad `credential_process` y cierre del bootstrap al final. Aplicar su addenda al prompt antes de ejecutarlo. Las instrucciones SSO siguientes se conservan como antecedente, no como el acceso vigente de este ensayo.

Bryan pide un prompt para preparar y publicar el repositorio local ISTPETDEV y una guía para habilitar la base AWS y el acceso seguro del equipo. Esta guía describe trabajo pendiente: no acredita instalaciones, commits, recursos cloud ni permisos ya aplicados. Complementa [[Repositorio de software y versiones]], [[Entornos y operacion acordados]] y [[Credenciales y acceso del equipo]]. Conserva el aporte del compañero y su responsabilidad sobre CI/CD/Compose de despliegue.

La ruta propuesta es: agente prepara aplicaciones, Terraform dev y scripts; Bryan configura identidad y bootstrap persistente en la consola; Bryan ejecuta Terraform y el bootstrap PostgreSQL; cada integrante inicia sesión y obtiene sus secretos autorizados. Red/RDS se crean mediante Terraform para que el código y los recursos coincidan. No crear un segundo RDS manualmente en la consola.

## Prompt para el agente de ISTPETDEV

```text
Trabaja en ~/Projects/ISTPETDEV. Prepara un workspace real, instalado,
compilable y documentado, y publícalo en develop de:
https://github.com/bryancito1090/istpetdev-platform.git

Tu alcance es código, instalaciones locales, scripts e infraestructura como
código. No crees recursos AWS ni ejecutes terraform apply; yo seguiré la guía
para configurar permisos y aprovisionar después. No accedas ni modifiques mi
servidor n8n. Continúa hasta completar los builds, verificar y publicar.

1. Contexto y preservación
Lee AGENTS.md aplicables y la bóveda ~/Projects/HACKATHON, especialmente:
- 03 Arquitectura/Repositorio de software y versiones.md
- 03 Arquitectura/Entornos y operacion acordados.md
- 03 Arquitectura/Credenciales y acceso del equipo.md
- 03 Arquitectura/Identidad OIDC y sesiones.md
- 03 Arquitectura/Puesta en marcha del workspace y AWS dev.md
- Backend y tiempo real, Frontend con Feature-Sliced Design, Movil offline
  y sincronizacion, Contratos API y eventos, AWS y Terraform, CI-CD y hardening.
Los acuerdos del 7 de octubre complementan el aporte de mi compañero. Conserva
ese aporte; adapta rutas/configuración sin reemplazar su arquitectura ni sus
workflows. CI/CD y Compose de despliegue los implementará él. Puedes agregar
dependencias Compose locales en un archivo distinto. No edites ni publiques
la bóveda durante este encargo.

2. Git y versiones
Inspecciona estado e historial, confirma origin y haz fetch antes de integrar.
Conserva cambios locales y remotos. Usa develop existente; si solo existe main,
deriva develop; si el remoto está vacío, publica el primer commit en develop.
No hagas reset destructivo, force-push, cambios de visibilidad ni push a main.
Si develop está protegida, publica una rama de bootstrap y abre PR a develop;
no cambies sus protecciones. Usa la autenticación GitHub individual existente.

Instala .NET SDK 8 con parche vigente fijado en global.json, EF Core/Npgsql 8.x,
Node 22.23.3, npm 10.9.9, Angular framework 22.2.1, CLI/build 22.2.2,
TypeScript 6.0.3, RxJS 7.8.2, Ionic Angular 8.8.19 y Tailwind 3.4.19.
Comprueba registros oficiales, peers y compatibilidad antes de instalar.
No sustituyas .NET 8/Angular 22 por otra generación. Fija paquetes y lockfiles;
documenta cualquier ajuste de parche necesario. La primera app móvil es PWA;
Capacitor 8.5.2 solo si hace falta, sin exigir SDK Android/iOS para arrancar.
Instala/configura herramientas locales necesarias: Git/gh si faltan, Docker
Engine soportado >=28 con Compose v2, Terraform >=1.10 compatible, AWS CLI v2,
Session Manager plugin y cliente PostgreSQL 16. Evita alterar otros proyectos.
Si la máquina no permite una instalación del sistema, usa instalación de
usuario o deja el comando exacto y la limitación comprobada; no finjas éxito.

3. Aplicaciones reales
Crea solución .NET 8 con src/{Api,Application,Domain,Infrastructure}, referencias
correctas entre capas, EF Core/Npgsql/PostGIS, configuración por entorno,
logs sin secretos, CORS local 4200/8100 y endpoints de salud de proceso/DB
separados. Api escucha localmente en localhost:5000; la falta de una conexión
AWS no impide arrancar el proceso, pero readiness DB indica indisponibilidad.
No implementes el dominio completo ni agregues repositorios genéricos vacíos.
Prepara configuración JWT/Cognito conforme ADR-10; no inventes un proveedor
de autenticación ni habilites endpoints de negocio sin autorización.

Genera Angular web en apps/web con las capas FSD app, pages, widgets, features,
entities, shared y sus rutas existentes. Usa standalone, Signals y RxJS según
el diseño. Genera Ionic Angular/PWA en apps/mobile/src/app, sin imponer FSD
móvil. Configura libs/shared-core/src/{contracts,models,units,validation} para
compartir contratos TypeScript sin importar frameworks de UI. Respeta app/
como raíz de composición de FSD, no como padre de todas las capas.
Web y móvil deben arrancar y mostrar una comprobación sencilla de la API.
Configura scripts de raíz npm run dev:api, dev:web, dev:mobile, build:web,
build:mobile y check; documenta puertos, restauración y comandos de arranque.
Instala realmente las dependencias, registra lockfiles y comprueba builds.

4. Infraestructura como código, solo preparación
Implementa infra/terraform/environments/dev y módulos necesarios en la
estructura acordada. Provider AWS ~>5.50 y backend S3 use_lockfile=true.
Deja bootstrap persistente documentado y separado del destroy de dev:
la guía crea su bucket privado/versionado y KMS desde consola; no intentes
crear de nuevo ese mismo bucket/clave en el stack dev. Si agregas código de
adopción del bootstrap, que requiera import explícito, nunca creación duplicada.
Los entornos demo/prod mantienen su diseño documentado y el trabajo pendiente
del compañero; no implementes ni actives despliegues de producción aquí.

Dev debe declarar: us-east-1, VPC dedicada 10.42.0.0/16 con subredes públicas
y privadas de datos en dos AZ, IGW y nodo EC2 de acceso t3.micro Ubuntu 24.04
oficial, sin inbound, con IP pública para salida y rol SSM. No NAT Gateway.
RDS PostgreSQL 16 privado db.t4g.medium Single-AZ, gp3 20 GiB/max100, cifrado,
force_ssl, backups siete días y protección de eliminación. SG de DB permite
5432 únicamente desde SG del nodo de acceso. SSM Agent actualizado y túnel
remoto soportado; IMDSv2. S3 dev para evidencias privado/versionado, TLS,
cifrado y permisos por prefijo autorizado. Tags del proyecto/entorno.
RDS administra el password master con Secrets Manager; no generes ni pases
ese password mediante variables Terraform. No crees secret versions con
contraseñas/tokens en Terraform ni publiques sus valores en outputs.
Outputs dev: IDs/ARNs, endpoint, bucket y ARN de secreto master, sin valores
secretos. Estos outputs se usarán localmente fuera del repositorio.

Prepara ejemplos backend-dev.hcl.example y terraform.tfvars.example sin
credenciales ni identificadores reales. terraform init -backend=false,
fmt y validate deben pasar sin una cuenta AWS configurada. No ejecutes plan
contra AWS ni apply durante esta tarea.

5. Datos y scripts seguros para ejecutar después
Implementa estos comandos con --help y documentación que coincida con ellos:
- scripts/ops/export-dev-config.sh --outputs <dev-outputs.json>
  --output-dir <directorio-privado>
- scripts/ops/dev-tunnel.sh --profile <perfil> --config <dev-access.json>
- scripts/base_datos/bootstrap-dev.sh --profile <perfil-admin>
  --config <dev-access.json> --outputs <dev-outputs.json>
- scripts/ops/render-dev-policies.sh --profile <perfil-admin>
  --config <dev-access.json> --output-dir <directorio-privado>
- scripts/ops/load-dev-secrets.sh --profile <perfil-dev> --member dev_01
  --database personal|integration --config <dev-access.json>

Export-dev-config escribe solo metadata necesaria al equipo y dev-access.json,
sin incluir el secreto master ni su valor. Tunnel abre SSM al nodo dev, remoto
RDS5432/local15432. Instala/descarga CA RDS oficial fuera del repo. Documenta
TLS VerifyFull para Npgsql conservando Host DNS real y mapeo local loopback;
para psql puede usarse host real + hostaddr=127.0.0.1. No desactives TLS.

Bootstrap PostgreSQL se ejecutará solo por administrador con túnel abierto:
crea istpetdev_dev_01..05 e istpetdev_integration, PostGIS compatible, roles
personales app/migrador separados y usuarios de integración por integrante.
Revoca CONNECT de PUBLIC y grants/default privileges innecesarios, limita
cada rol a su DB. Integración tiene migrador controlado por el administrador.
Genera passwords seguros en memoria, almacénalos en Secrets Manager con
los nombres de la guía, y coordina DB/secreto de forma recuperable e
idempotente: reusar secretos existentes, no rotar passwords ni borrar bases
al repetir. Nunca loguear SQL con passwords ni ejecutar migraciones al startup.
Los secretos personales de app usan /istpetdev/dev/db/dev_01..05 y los de
migración /istpetdev/dev/db/dev_01/migrator..dev_05/migrator. Integración:
/istpetdev/dev/db/integration/dev_01..05; migrador solo del administrador.

Render-dev-policies materializa JSON por dev_01..05, con ARN efectivos,
en un directorio privado: SSM al nodo y documento de túnel exactos, sesiones
propias compatibles con SSO, GetSecretValue a secretos personales/migrador
e integración asignados y S3 solo a prefijos dev autorizados. Sin acceso al
master, otros secretos personales, producción, state ni administración AWS.
Admite añadir ARN de integraciones autorizadas explícitamente sin wildcard.
Load-dev-secrets valida la asignación, obtiene los valores sin imprimirlos,
construye conexiones y las carga mediante stdin a .NET user-secrets o archivo
privado 0600 con launcher explícito. No pasa passwords por argumentos,
no modifica Angular y no vuelca secrets a stdout. Nunca uses mi access key.

6. Configuración, comprobación y entrega
Mantén .env.example sin valores privados; .NET no carga .env automáticamente.
Ignora .aws, secretos, .env privados, state, planes, tfvars privados, llaves,
backups, dependencias y archivos locales de herramientas. Versiona lockfiles.
Mi n8n es https://n8n.bryan-bano.com/: credenciales y N8N_ENCRYPTION_KEY solo
en ese servidor; no instales otro n8n ni agregues tokens a exports/workflows.
GitHub Actions usará OIDC después, nunca mis claves AWS estáticas. Documenta
la comprobación del subject real de OIDC con IDs inmutables si corresponde.
No reescribas el CI/CD del compañero ni publiques un workflow de deploy nuevo.

Agrega docs/onboarding.md con esta secuencia y docs/aws-dev.md con recursos,
outputs, configuración backend/variables, plan/apply y comandos exactos.
Incluye la política TLS del bucket state para copiar en consola con placeholder.
Comprueba .NET restore/build, web/móvil builds, shared-core, humo API,
fmt/validate Terraform y shell syntax. Prueba el aislamiento e idempotencia
del bootstrap con PostgreSQL local desechable si Docker está disponible;
no uses RDS ni afirmes pruebas AWS. Pruebas no realizadas quedan explícitas.

Revisa staged files y ejecuta detección de secretos antes de publicar.
Corrige fallos y publica código/configuración pública en origin develop;
si la rama está protegida, deja el PR hacia develop. No publiques archivos
privados ni la bóveda. Termina con commit/URL/rama, herramientas y versiones,
verificaciones ejecutadas, comandos de arranque y pasos AWS pendientes.
No solicites credenciales por chat ni me pidas confirmar elecciones rutinarias.
```

## Pasos para Bryan después de recibir la entrega del agente

Los comandos siguientes son el contrato que debe implementar el agente; actualmente los scripts no existen. Usarlos solo después de comprobar su entrega. Los nombres de botones corresponden a la consola en inglés; la región del proyecto es `us-east-1`. Si ya hay Identity Center o un bootstrap state adecuado, reutilizarlos sin crear duplicados.

### 1. GitHub y entrega local

Abrir [istpetdev-platform](https://github.com/bryancito1090/istpetdev-platform) → **Settings → Collaborators → Add people** → buscar usuario individual → confirmar invitación. Repetir para la bóveda si falta acceso. Revisar selector de rama **develop**, README, apps, src, infra y docs. [Instrucciones GitHub](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/repository-access-and-collaboration/inviting-collaborators-to-a-personal-repository).

### 2. Identity Center y MFA

Abrir [AWS Console](https://console.aws.amazon.com/) con identidad administrativa de Bryan y seleccionar **US East (N. Virginia)**. Abrir [IAM Identity Center](https://console.aws.amazon.com/singlesignon/) → **Enable**, si no existe → instancia de organización, **Single-Region**, acceso a cuentas/permission sets habilitado. Si la cuenta es independiente, la consola propone habilitar con Organizations; revisar esa pantalla antes de confirmar. Una instancia solo de aplicaciones no sirve para CLI de cuentas. [Habilitación oficial](https://docs.aws.amazon.com/singlesignon/latest/userguide/enable-identity-center.html).

Si aparece la conversión de una cuenta free tier a plan pagado al crear Organizations, revisar sus condiciones de facturación antes de activar; Bryan confirmó presupuesto disponible, pero la cuenta efectiva aún no se inspeccionó.

**Settings → Authentication → Multi-factor authentication → Configure**: elegir **Every time they sign in (always-on)** y **Require them to register an MFA device at sign in**, con TOTP/passkey permitidos → **Save changes**. Si se usa un IdP externo, MFA se exige en ese IdP. [Frecuencia MFA](https://docs.aws.amazon.com/singlesignon/latest/userguide/mfa-getting-started.html), [registro obligatorio](https://docs.aws.amazon.com/singlesignon/latest/userguide/how-to-configure-mfa-device-enforcement.html).

**Users → Add user**: usuario, correo, nombre/apellidos; elegir envío de instrucciones de contraseña → **Next → Next → Add user**. Crear a Bryan y los otros cuatro integrantes. Anotar asignaciones privadas `dev_01`..`dev_05`. [Alta de usuarios](https://docs.aws.amazon.com/singlesignon/latest/userguide/addusers.html).

### 3. Acceso administrativo de Bryan

Reutilizar el permission set administrativo existente si lo hay. Si hace falta el acceso inicial: **Permission sets → Create permission set → Predefined permission set → AdministratorAccess → Next**; nombre `IstpetDevInfraAdmin`, sesión una hora → **Next → Create**. Ese acceso amplio es exclusivamente para Bryan/bootstrap; el equipo recibirá políticas dev acotadas. Tras bootstrap, revisar/reducir los permisos de infraestructura a los recursos y operaciones necesarios.

**AWS accounts → seleccionar cuenta → Assign users or groups → Users → Bryan → Next → IstpetDevInfraAdmin → Next → Submit**. En **Dashboard/Settings** copiar AWS access portal URL y región de Identity Center. [Crear permission sets](https://docs.aws.amazon.com/singlesignon/latest/userguide/howtocreatepermissionset.html), [asignar cuenta](https://docs.aws.amazon.com/singlesignon/latest/userguide/assignusers.html).

Terminal local:

```bash
aws configure sso --profile istpetdev-admin
aws sso login --profile istpetdev-admin
aws sts get-caller-identity --profile istpetdev-admin
```

Seleccionar portal/región SSO, cuenta real, rol de Bryan y región predeterminada `us-east-1`. `get-caller-identity` comprueba la cuenta; no devuelve passwords ni tokens. Nunca crear access keys root para este flujo.

### 4. Bootstrap persistente del state: KMS y S3

Estos recursos se crean una vez por consola y se administran separados del stack dev. No pertenecen a un `destroy` rutinario de aplicaciones. Su creación manual se registra en docs/aws-dev.md del software; no importar ni recrear automáticamente si ya existen.

[KMS](https://console.aws.amazon.com/kms/) → región `us-east-1` → **Customer managed keys → Create key → Symmetric → Encrypt and decrypt → Next**. Alias `istpetdev-tfstate` → **Next**. Como administrador y usuario de la clave seleccionar solo el rol SSO de Bryan (`AWSReservedSSO_IstpetDevInfraAdmin_...` o su rol administrativo existente), no roles dev → **Next**, revisar política → terminar. Guardar ARN de la clave. [Crear clave simétrica](https://docs.aws.amazon.com/kms/latest/developerguide/create-symmetric-cmk.html).

[S3](https://console.aws.amazon.com/s3/) → **Create bucket**: General purpose, región `us-east-1`, nombre `istpetdev-tfstate-<ACCOUNT_ID>-us-east-1`; ACLs deshabilitadas, **Block all public access** activado, **Bucket Versioning → Enable**, cifrado **SSE-KMS** con la clave anterior → **Create bucket**. [Creación de bucket](https://docs.aws.amazon.com/AmazonS3/latest/userguide/create-bucket-overview.html).

Bucket → **Permissions → Bucket policy → Edit**: pegar la política que entregó el agente en docs/aws-dev.md, reemplazando el nombre del bucket; comprobar que niega transporte HTTP sobre bucket y objetos, sin conceder acceso público → **Save changes**. [Política HTTPS oficial](https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingEncryptionInTransit.html).

Crear archivo privado de backend en la máquina de Bryan:

```bash
umask 077
private_dir="$HOME/.config/istpetdev/private"
mkdir -p "$private_dir"
chmod 700 "$private_dir"
nano "$private_dir/backend-dev.hcl"
```

Contenido, sustituyendo placeholders por valores reales:

```hcl
bucket       = "istpetdev-tfstate-<ACCOUNT_ID>-us-east-1"
key          = "dev/terraform.tfstate"
region       = "us-east-1"
encrypt      = true
kms_key_id   = "<ARN_KMS_STATE>"
use_lockfile = true
```

No contiene passwords, pero se mantiene fuera del repo. El state y planes siempre son privados; el rol dev no debe leerlos. Versionar `.terraform.lock.hcl`, no archivos state. [Backend S3 Terraform](https://developer.hashicorp.com/terraform/language/backend/s3).

### 5. Crear los recursos dev desde el código publicado

Revisar docs/aws-dev.md y el ejemplo de tfvars del agente. En la ruta propuesta, región/tamaño/red ya tienen defaults acordados y no se requiere password en tfvars. Si hay variables obligatorias no sensibles, cargarlas en un archivo privado y usar `-var-file` tanto al plan como en posteriores planes.

```bash
cd ~/Projects/ISTPETDEV
export AWS_PROFILE=istpetdev-admin
export AWS_REGION=us-east-1
private_dir="$HOME/.config/istpetdev/private"
umask 077
terraform -chdir=infra/terraform/environments/dev init \
  -backend-config="$private_dir/backend-dev.hcl"
terraform -chdir=infra/terraform/environments/dev plan \
  -out="$private_dir/dev.tfplan"
```

Revisar el plan: recursos dev del proyecto, sin eliminar infraestructura previa, sin DB pública ni NAT/ALB/Fargate de producción. Después ejecutar:

```bash
terraform -chdir=infra/terraform/environments/dev apply \
  "$private_dir/dev.tfplan"
terraform -chdir=infra/terraform/environments/dev output -json \
  > "$private_dir/dev-outputs.json"
bash scripts/ops/export-dev-config.sh \
  --outputs "$private_dir/dev-outputs.json" \
  --output-dir "$private_dir"
```

Outputs son metadata; no passwords. La DB puede tardar varios minutos. RDS genera/administra su secreto master en Secrets Manager y Terraform solo conserva la referencia. [Gestión de password RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/rds-secrets-manager.html).

### 6. Comprobar RDS y el nodo SSM

[RDS](https://console.aws.amazon.com/rds/) → **Databases → DB creada por Terraform**: estado **Available**; en **Connectivity & security**, **Publicly accessible: No**, endpoint y SG privado; en **Configuration**, PostgreSQL16/cifrado y secreto master; en **Maintenance & backups**, backups siete días. La red/subnet group debe cubrir dos AZ aunque la instancia sea Single-AZ. [Creación y prerequisitos de RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_CreateDBInstance.html).

[EC2](https://console.aws.amazon.com/ec2/) → **Instances → nodo de acceso dev**: **Running**, comprobaciones aprobadas, SG sin reglas inbound. [Systems Manager](https://console.aws.amazon.com/systems-manager/) → **Fleet Manager/Managed nodes**: verificar nodo disponible/online. Los equipos usan el documento de port forwarding desde CLI, no una shell web con permisos de administración. [Sesiones SSM](https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-sessions-start.html).

### 7. Inicializar las bases y sus secretos

Terminal A, dejar abierto el túnel de administrador:

```bash
cd ~/Projects/ISTPETDEV
bash scripts/ops/dev-tunnel.sh \
  --profile istpetdev-admin \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
```

Terminal B:

```bash
cd ~/Projects/ISTPETDEV
private_dir="$HOME/.config/istpetdev/private"
bash scripts/base_datos/bootstrap-dev.sh \
  --profile istpetdev-admin \
  --config "$private_dir/dev-access.json" \
  --outputs "$private_dir/dev-outputs.json"
bash scripts/ops/render-dev-policies.sh \
  --profile istpetdev-admin \
  --config "$private_dir/dev-access.json" \
  --output-dir "$private_dir/dev-policies"
```

El script debe crear cinco bases personales y una de integración, usuarios/migradores limitados y secretos individuales. Repetir no debe borrar datos ni rotar contraseñas sin petición. El migrador de integración lo controla el administrador. En [Secrets Manager](https://console.aws.amazon.com/secretsmanager/) → **Secrets**, verificar los nombres `/istpetdev/dev/db/...`; no capturar/compartir su valor.

Para Npgsql, el helper guía la CA y solicita configurar la máquina de quien usa el túnel: `sudo nano /etc/hosts`, añadir `127.0.0.1 <ENDPOINT_DNS_REAL_RDS>`, guardar. Conectar usando **Host DNS real**, port15432 y `VerifyFull`, nunca desactivar validación. El nodo AWS resuelve el endpoint real; este mapeo solo es local. Para psql usar host real + hostaddr loopback. [TLS RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html).

### 8. Dar permisos limitados a cada integrante

Identity Center → **Permission sets → Create permission set → Custom permission set → Next → Inline policy**. Pegar el JSON `dev_01.json` del directorio privado generado, verificar ARN exactos del nodo/documento y secretos autorizados → **Next**. Nombre `IstpetDevDev01`, sesión una hora → **Next → Create**. Repetir `Dev02`..`Dev05` con sus JSON. No añadir AdministratorAccess, acceso master ni SecretsManagerReadWrite. [Políticas inline de Identity Center](https://docs.aws.amazon.com/singlesignon/latest/userguide/permissionsetcustom.html).

**AWS accounts → seleccionar cuenta → Assign users or groups → Users → integrante asignado a dev_01 → Next → IstpetDevDev01 → Next → Submit**. Repetir de manera individual. No asignar los cinco permission sets a todo el grupo. Bryan puede tener además el rol dev para desarrollar y usar su rol admin solo en operaciones de infraestructura.

### 9. Tokens de integraciones

Secrets Manager → **Store a new secret → Other type of secret → Key/value**. Cargar el token en el campo que requiera el consumidor, cifrado `aws/secretsmanager` para dev → **Next**. Nombre `/istpetdev/dev/integrations/<proveedor>/<integrante-o-servicio>` → **Next**. Mantener rotación automática deshabilitada hasta tener una integración de rotación funcional; revisión/rotación manual registrada → **Next → Store**. [Creación de secretos](https://docs.aws.amazon.com/secretsmanager/latest/userguide/create_secret.html).

Secret creado → copiar solo ARN. Agregar ese ARN exacto a `GetSecretValue` en el permission set de quienes necesiten la integración; guardar y reprovisionar/actualizar en la cuenta si la consola lo solicita. Una API key dev por integrante cuando lo soporte el proveedor permite retirar acceso sin afectar al resto. DeepSeek permanece en las credenciales del servidor n8n; no copiar la clave de cifrado/admin a Secrets Manager para distribuirla al equipo.

### 10. Incorporación en cada máquina

Cada integrante acepta invitación GitHub/Identity Center, establece contraseña/MFA y clona `develop`. Instala las herramientas y dependencias con docs/onboarding.md; los binarios instalados en la máquina de Bryan no viajan con Git.

```bash
git clone --branch develop https://github.com/bryancito1090/istpetdev-platform.git
cd istpetdev-platform
npm ci
dotnet restore
aws configure sso --profile istpetdev-dev
aws sso login --profile istpetdev-dev
```

Selecciona únicamente su rol `IstpetDevDevXX`, región proyecto `us-east-1` y región SSO real. Bryan le comparte `dev-access.json` (metadata, sin passwords), portal/región SSO y asignación dev_XX por canal privado. Configura CA/mapeo local y abre el túnel con `dev-tunnel.sh --profile istpetdev-dev --config <archivo-privado>`. En otra terminal:

```bash
bash scripts/ops/load-dev-secrets.sh \
  --profile istpetdev-dev --member dev_01 --database personal \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
npm run dev:api
```

Reemplazar `dev_01` por su asignación; web/móvil en terminales separadas con `npm run dev:web` y `npm run dev:mobile`. URLs `http://localhost:4200` y `http://localhost:8100`; API `http://localhost:5000`. Para pruebas conjuntas, `--database integration` carga su usuario individual de la misma base de integración; sus migraciones se coordinan y no se ejecutan desde todas las máquinas.

### 11. Verificación y costos

Comprobar V-30/V-32: integrante puede recuperar su secreto y conectar, lectura del secreto de otro integrante denegada, SQL sobre DB ajena denegado y readiness de API correcto. Revisar staged/files, artefactos y logs sin copiar valores. No confundir pruebas locales del agente con estas pruebas reales AWS.

Configurar [AWS Budgets](https://console.aws.amazon.com/billing/home#/budgets): **Create budget → Customize → Cost budget**, mensual, importe real elegido por Bryan, avisos de costo real/previsto al50/80/100% y destinatarios autorizados. RDS/nodo se mantienen disponibles durante trabajo coordinado. Al cerrar sesión sin usuarios, detener nodo; detener RDS solo coordinadamente. RDS parada se reinicia automáticamente tras siete días y mantiene costos de almacenamiento/backups. [Parada temporal RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_StopInstance.html).

La entrega inicial deja workspace y AWS dev listos tras estos pasos. CI/CD del compañero, dominio institucional, despliegue completo demo/prod y credenciales n8n pendientes se mantienen registrados como trabajo posterior. Identidad Cognito de la aplicación se implementa conforme ADR-10 cuando se habiliten los flujos humanos; no sustituye el SSO del equipo.
