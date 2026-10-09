# AWS dev temporal — IstpetDev

El trabajo usa `develop`, AWS `us-east-1` y la cuenta existente. No crea Organizations, otro n8n, ni los perfiles demo/producción. El estado real y las pruebas están en [deployment-status.md](deployment-status.md); una configuración versionada no demuestra que se haya desplegado.

## Acceso y archivos privados

Cada integrante conserva su usuario IAM, contraseña y MFA. AWS CLI >=2.32 y Session Manager plugin. No crear access keys humanas. Después del primer acceso/cambio de contraseña, registrar MFA en **Security credentials → Assign MFA device → Authenticator app**. El nombre permitido por la política existente es exactamente `<usuario-IAM>-mfa`. Escanear el QR privadamente e introducir dos códigos consecutivos; salir y entrar de nuevo. Nunca enviar QR, claves o códigos al equipo.

```bash
aws login --profile istpetdev-dev-console --region us-east-1
aws configure set region us-east-1 --profile istpetdev-dev-console
aws configure set region us-east-1 --profile istpetdev-dev
aws configure set credential_process 'aws configure export-credentials --profile istpetdev-dev-console --region us-east-1 --format process' --profile istpetdev-dev
export AWS_PROFILE=istpetdev-dev AWS_REGION=us-east-1 AWS_DEFAULT_REGION=us-east-1 AWS_PAGER=''
aws sts get-caller-identity --no-cli-pager
```

La identidad administrativa usa los perfiles existentes `istpetdev-admin-console` y `istpetdev-admin`. No ejecutar el proceso de credenciales para mostrar su salida. Usar `umask 077` y `~/.config/istpetdev/private` (0700) para state inicial, planes, tfvars, outputs, respaldos y configuración; los archivos son 0600. .NET user-secrets mantiene valores fuera de Git pero no los cifra. Evitar `set -x`, `TF_LOG`, `aws --debug`, listar user-secrets o imprimir respuestas con passwords.

## Infraestructura declarada

- VPC dedicada `10.42.0.0/16`, dos subredes públicas y dos privadas de datos, dos AZ, IGW, sin NAT. Antes de una cuenta nueva revisar colisiones de CIDR.
- Ubuntu 24.04 `t3.micro`: salida a SSM, IMDSv2, disco gp3 cifrado, ninguna entrada SSH/HTTP. No sirve aplicaciones.
- PostgreSQL 16 Single-AZ `db.t4g.medium`: gp3 20 GiB, máximo 100, privado, cifrado, `rds.force_ssl=1`, protección de borrado. TCP 5432 exclusivamente desde el SG del nodo. Password master gestionado por RDS.
- S3 separados: state SSE-KMS con la clave **existente** `alias/istpetdev-tfstate`; evidencias y dumps SSE-S3. Todos privados, versionados, ownership enforced y TLS obligatorio.
- Secretos: cinco app personales, cinco migradores, cinco usuarios de integración, migrador y backup de integración. Terraform solo administra contenedores/ARN; `bootstrap-dev.sh` crea valores fuera del state.
- Cognito dev: registro administrativo, TOTP obligatorio, web y PWA públicas sin client secret, flujo code, tokens 15 min, refresh siete días con rotación/revocación. Callbacks localhost exactos 4200/8100. El cliente debe usar PKCE S256; la UI de login y el RBAC de negocio continúan siendo trabajo de aplicación.
- Logs PostgreSQL/upgrade 14 días; alarmas RDS CPU/memoria/almacenamiento/conexiones, estado EC2, eventos RDS. SNS requiere confirmar el correo.
- Dos presupuestos sobre el mismo gasto bruto mensual de la **cuenta** (real y previsto), sin descontar créditos/reembolsos, umbrales 50/80/100%. No suman dos gastos ni constituyen un tope o apagado. El importe y correo se cargan en tfvars privados.

`ExpiresAt` es inventario, no un programador de borrado. Cierre previsto el 18 de octubre de 2026; límite operativo el 20. No esperar la expiración del plan gratuito del 21.

## Bootstrap del estado

La KMS es una fuente de datos, no se crea ni se cambia su política. Inventariar antes: STS, alias/estado/policy de KMS, buckets de la cuenta, VPC/RDS/EC2, usuarios/grupos/políticas/MFA y OIDC. Ante un bucket ya existente, detener la creación, inspeccionar su policy y state e importar sus recursos; conservar declaraciones de seguridad preexistentes. El script rechaza adoptar a ciegas.

```bash
export AWS_PROFILE=istpetdev-admin AWS_REGION=us-east-1 AWS_DEFAULT_REGION=us-east-1 AWS_PAGER=''
export EXPIRES_AT=2026-10-20
umask 077
bash scripts/ops/bootstrap-state.sh prepare
# Revisar el plan: seis recursos S3, ninguna clave nueva ni borrado.
TF_DATA_DIR="$HOME/.config/istpetdev/private/terraform-bootstrap" \
  terraform -chdir=infra/terraform/bootstrap show "$HOME/.config/istpetdev/private/bootstrap.tfplan"
bash scripts/ops/bootstrap-state.sh apply
bash scripts/ops/bootstrap-state.sh migrate
```

Solo el primer arranque usa `backend_override.tf` ignorado: backend local apuntando a `private/bootstrap.tfstate`. `migrate` retira ese override, migra al backend S3 y compara recursos/outputs con la copia local. Terraform puede asignar nueva lineage/serial al backend destino; comprobar también un plan sin cambios. No volver a preparar como stack vacío. El bootstrap tiene `prevent_destroy`; se retira únicamente al cierre autorizado, después del stack dev y exportación privada.

## Plan y despliegue dev

Copiar `terraform.tfvars.example` a un archivo privado y cargar presupuesto/correo, expiración, subject OIDC verificado y asignaciones MFA aprobadas. Nunca incluir contraseñas en tfvars. El bloqueo nativo usa objetos `.tflock`; los permisos de desarrolladores no dan acceso al bucket state ni a KMS.

```bash
umask 077
export AWS_PROFILE=istpetdev-admin AWS_REGION=us-east-1 AWS_DEFAULT_REGION=us-east-1 AWS_PAGER=''
private_dir="$HOME/.config/istpetdev/private"
export TF_DATA_DIR="$private_dir/terraform-dev"
terraform -chdir=infra/terraform/environments/dev init -backend-config="$private_dir/backend-dev.hcl"
terraform -chdir=infra/terraform/environments/dev plan -var-file="$private_dir/dev.tfvars.json" -out="$private_dir/dev.tfplan"
terraform -chdir=infra/terraform/environments/dev show "$private_dir/dev.tfplan"
terraform -chdir=infra/terraform/environments/dev apply "$private_dir/dev.tfplan"
terraform -chdir=infra/terraform/environments/dev output -json > "$private_dir/dev-outputs.json"
bash scripts/ops/export-dev-config.sh --outputs "$private_dir/dev-outputs.json" --output-dir "$private_dir"
```

Ante un apply parcial, conservar state, corregir el bloqueo, generar/revisar **un plan nuevo**. No reutilizar un plan aplicado, crear duplicados a mano ni borrar state. Un `FreeTierRestrictionError` no se resuelve con más privilegios IAM.

El diseño original usa siete días de backups continuos RDS. La cuenta Free rechazó ese valor. La alternativa `free_plan_backup_workaround=true`, solo tras aceptación explícita, usa **un día PITR + snapshots diarios siete días** con AWS Backup (08:00 UTC / 03:00 Ecuador), sin continuidad de siete días. AWS Backup selecciona exclusivamente este RDS y avisa de jobs fallidos/expirados/abortados. El almacenamiento de snapshots consume créditos; verificar el primer job y una restauración. Ver el estado de aceptación/aplicación en el registro de despliegue.

## Bases, túnel y backend local

```bash
# Terminal A: túnel; no necesita editar DNS para psql.
bash scripts/ops/dev-tunnel.sh --profile istpetdev-admin --config "$HOME/.config/istpetdev/private/dev-access.json"
# Terminal B: bootstrap administrativo; SQL por stdin y passwords en archivos temporales 0600.
bash scripts/base_datos/bootstrap-dev.sh --profile istpetdev-admin \
  --config "$HOME/.config/istpetdev/private/dev-access.json" \
  --outputs "$HOME/.config/istpetdev/private/dev-outputs.json"
```

El documento SSM del proyecto fija el endpoint RDS/5432; solo permite elegir puerto local. No autorizar `AWS-StartPortForwardingSessionToRemoteHost` genérico a compañeros: permitiría otros destinos. El nodo resuelve el endpoint en AWS; la CA oficial RDS se descarga por HTTPS y se valida. Si falta o es inválida, los scripts fallan: no reducen TLS.

`psql` usa host real + `PGHOSTADDR=127.0.0.1` y `verify-full`, sin modificar hosts. **Npgsql requiere** que el endpoint DNS real se resuelva a `127.0.0.1` localmente: añadir una línea `127.0.0.1 <endpoint-real-RDS>` a `/etc/hosts` mediante el editor del sistema, conservar el resto y retirar solo esa línea al finalizar. No publicar el endpoint como localhost en el secreto ni modificar DNS AWS. En contenedores, definir una ruta explícita al túnel del host y validarla; el loopback interno pertenece al contenedor.

El administrador distribuye `dev-access.json` y la asignación dev_XX por canal privado; no distribuye `dev-outputs.json`, state ni credenciales. Cada integrante abre su propio túnel con su perfil y ejecuta:

```bash
bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_02 --database personal \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
npm run dev:api
```

Sustituir dev_02 por la asignación. `--database integration` carga la identidad individual para la base compartida; `--mode file` escribe `~/.config/istpetdev/private/dev-env.sh` 0600 para `source` explícito. Las conexiones usan VerifyFull y pool máximo diez. El script verifica acceso antes de guardar y pasa JSON a user-secrets por stdin. No modificar Angular para cargar secretos. `--migrator` es solo para la tarea de migración; luego volver a cargar app. No ejecutar API con master/migrador.

`bootstrap` conserva bases/datos/passwords al repetirse; reusa secretos y valida que coincidan con host/rol/base. Un rol sin secreto o una contraseña desincronizada detiene el proceso; recuperar la versión correcta o rotar de forma coordinada, no generar una sustitución silenciosa. App tiene DML, migrador DDL en su base; backup de integración solo lectura; PUBLIC no puede conectar a bases personales/integración ni crear en schema public. Revoca acceso a las bases de mantenimiento del RDS dedicado.

## Habilitar integrantes después de MFA

`team-roster.json` privado relaciona dev_01..05 con usuarios IAM existentes. Bryan conserva administración; no se presenta esa identidad como usuario dev aislado.

```bash
python3 scripts/ops/prepare-team.py --profile istpetdev-admin \
  --roster "$private_dir/team-roster.json" --vars "$private_dir/dev.tfvars.json"
# Plan/apply dev de nuevo. Cada usuario propuesto se comprueba otra vez por API.
bash scripts/ops/render-dev-policies.sh --outputs "$private_dir/dev-outputs.json" --output-dir "$private_dir/dev-policies"
```

Solo se añade la política individual a quienes tienen MFA. Se conservan los usuarios/grupos/políticas existentes; auditar permisos heredados para detectar concesiones más amplias. El acceso a secretos/S3/túnel requiere contexto de sesión MFA, ARN exactos de sus tres secretos y prefijo de evidencia propio; Resume/Terminate solo `${aws:username}-*`. Nada de master, integración migrador/backup, state, otros integrantes o producción. Si una sesión vieja no satisface MFA, cerrar y autenticar de nuevo; no retirar la condición para evitar un 403.

Comprobación **con la sesión real de cada integrante**, nunca con el administrador:

```bash
python3 scripts/ops/verify-member.py --profile istpetdev-dev --member dev_02 \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
```

Repetir con dos identidades distintas y registrar solo resultados. El simulador IAM ayuda a revisar policies, pero no sustituye este ensayo. Al retirar a alguien: quitar acceso IAM, terminar sus sesiones SSM, deshabilitar sus roles PostgreSQL/terminar conexiones activas y rotar credenciales compartidas que pudo conocer. Retirar GetSecretValue no invalida passwords previamente copiados.

## Migraciones, backups y recuperación

```bash
dotnet tool restore
dotnet build
python3 scripts/base_datos/database-task.py migrate --profile istpetdev-admin \
  --config "$private_dir/dev-access.json" --outputs "$private_dir/dev-outputs.json" --member integration
```

El script genera primero `pg_dump -Fc` con cliente **PostgreSQL 16**, comprueba catálogo/checksum, sube a S3 con SHA-256 y verifica la versión remota antes de DDL. Usa Docker `postgres:16-alpine` para evitar generar un dump de versión mayor incompatible. El migrador ejecuta SQL EF idempotente bajo advisory lock; no hay `EnsureCreated`, migraciones al arrancar ni downgrade automático. Aplicar también en las bases personales con `--member dev_XX` durante el bootstrap inicial. La integración recibe solo migraciones coordinadas desde develop.

```bash
python3 scripts/base_datos/database-task.py backup --profile istpetdev-admin \
  --config "$private_dir/dev-access.json" --outputs "$private_dir/dev-outputs.json"
python3 scripts/base_datos/database-task.py restore-check --profile istpetdev-admin \
  --config "$private_dir/dev-access.json" --outputs "$private_dir/dev-outputs.json" \
  --backup "$private_dir/backups/<archivo>.dump"
```

Coordinar una ventana sin escrituras para el ensayo: se comparan hashes/conteos de las tablas antes/después del dump. Restore-check crea una base aleatoria `istpetdev_restorecheck_*`, importa, compara todas las tablas/PostGIS y elimina **solo su base de prueba**. Guarda tiempos en `.restore.json`; no restaura sobre integración. El dump portable no guarda propietarios/ACL: una recuperación promovida requiere reaplicar los roles/grants canónicos antes de conceder tráfico. PITR/snapshot de instancia se restaura a **otra instancia privada** y se valida antes de cambiar endpoint/secretos; incluye roles de instancia. Nunca rebobinar operaciones aceptadas sin reconciliación.

Dumps S3: prefijos daily siete días/weekly 28 días, incluidas versiones no actuales. El script carga weekly los domingos. No hay un servicio demo encendido: su ejecución nocturna debe integrarse en el host/CI de demo del compañero cuando exista. Dev tiene backups RDS y, si fue aceptado, snapshots AWS Backup programados independientes del portátil. Antes de cada cambio destructivo realizar además snapshot o dump restaurado.

## Verificación reproducible

```bash
terraform fmt -check -recursive infra/terraform
terraform -chdir=infra/terraform/bootstrap init -backend=false
terraform -chdir=infra/terraform/bootstrap validate
terraform -chdir=infra/terraform/environments/dev init -backend=false
terraform -chdir=infra/terraform/environments/dev validate
dotnet build
dotnet list package --vulnerable --include-transitive
python3 scripts/base_datos/test-bootstrap.py
python3 scripts/ops/verify-storage.py --profile istpetdev-admin --outputs "$private_dir/dev-outputs.json"
```

Para validar offline usar un TF_DATA_DIR temporal distinto del usado para operar el backend. El ensayo Docker usa certificados/secretos efímeros, dos bootstraps, TLS/hostname, aislamiento, DML/DDL, EF dos veces, API readiness/401, dump/restauración y hashes. Elimina únicamente su contenedor/volumen y su base temporal. Los ensayos locales no acreditan conectividad RDS ni sesiones del equipo.

`verify-storage.py` revisa TLS, cifrado, ownership, bloqueo público y versionado; recupera una versión de un objeto sintético tras sobrescritura/borrado y limpia exclusivamente esas versiones. Recupera privadamente una versión identificada del state, sin modificar el state activo. Readiness de API exige conexión y migraciones; liveness permanece independiente de AWS.

## OIDC y despliegue del compañero

El proveedor GitHub se reutiliza si existe. Consultar `gh api repos/bryancito1090/istpetdev-platform/actions/oidc/customization/sub` antes de construir trust: el repositorio usa IDs inmutables. La configuración exige subject exacto y `aud=sts.amazonaws.com`, sin wildcard. El rol demo está **preparado sin permisos operativos** hasta que exista su SG/host; no concede Terraform ni secretos al runner.

El compañero conserva workflows/Compose/GHCR/SSH-SCP y el dominio institucional. Consultar [demo-release-contract.md](demo-release-contract.md). No se publica un deploy automático ni se enciende Perfil A/B por un push. No hay claves AWS estáticas ni secretos de integraciones en GitHub. `https://n8n.bryan-bano.com/` permanece externo y sin modificaciones; Bryan carga sus credenciales por el mecanismo seguro.

## Referencias

- [Login CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html), [MFA](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_mfa_enable_virtual.html).
- [Backend S3](https://developer.hashicorp.com/terraform/language/backend/s3), [override del backend](https://developer.hashicorp.com/terraform/language/files/override).
- [Retención RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_WorkingWithAutomatedBackups.BackupRetention.html), [snapshots AWS Backup](https://docs.aws.amazon.com/aws-backup/latest/devguide/rds-backup.html).
- [TLS Npgsql](https://www.npgsql.org/doc/security.html), [RDS trust store](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html).
