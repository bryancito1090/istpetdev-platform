# Guía de Aprovisionamiento y Operación AWS Dev — IstpetDev

Esta guía documenta los pasos para que el **Administrador de Infraestructura (Bryan)** configure el bootstrap persistente de Terraform, ejecute el aprovisionamiento de la nube y realice el bootstrap de las bases de datos y permisos del equipo.

---

## 1. Arquitectura del Entorno Dev en AWS

El entorno dev (`infra/terraform/environments/dev`) declara los siguientes recursos en la región `us-east-1`:

| Recurso | Tipo / Tamaño | Configuración y Seguridad |
|---|---|---|
| **VPC** | `10.42.0.0/16` | Dedicada, 2 AZs (`us-east-1a`, `us-east-1b`), IGW. **Sin NAT Gateway**. |
| **Subredes Públicas** | 2x `/24` (`10.42.0.0/24`, `10.42.1.0/24`) | Alojan el nodo EC2 de acceso con IP pública para egress. |
| **Subredes de Datos** | 2x `/24` (`10.42.20.0/24`, `10.42.21.0/24`) | DB Subnet Group privado de Amazon RDS. |
| **Nodo EC2 de Acceso** | `t3.micro` Ubuntu 24.04 LTS | **Sin reglas inbound** (Strict Default Deny). Egress HTTPS a SSM. Rol SSM e IMDSv2 obligado. |
| **Amazon RDS** | PostgreSQL 16 Single-AZ (`db.t4g.medium`) | Privado (`publicly_accessible=false`), gp3 20 GiB (auto-escalable a 100 GiB), cifrado en reposo, `rds.force_ssl=1`, retención de backups 7 días, deletion protection. |
| **Master Password RDS** | AWS Secrets Manager | Administrado nativamente por RDS (`manage_master_user_password=true`). Sin contraseñas en código Terraform. |
| **Security Group RDS** | TCP 5432 | **Permitido exclusivamente** desde el SG del nodo EC2 de acceso. |
| **S3 Evidencias** | `istpetdev-evidence-dev-<ACCOUNT_ID>-us-east-1` | Privado, Block Public Access total, versionado habilitado, cifrado SSE-S3 y política que deniega transporte no TLS. |

---

## 2. Paso Previo: Bootstrap Persistente del Backend State (Por Consola AWS)

Para desacoplar el estado de Terraform de los ciclos de vida y posibles `destroy` del stack de aplicaciones, el bucket de estado y su clave KMS se crean **una única vez** manualmente en la consola de AWS:

### 2.1 Crear Clave Simétrica KMS
1. En [AWS KMS (us-east-1)](https://console.aws.amazon.com/kms/):
   - **Create key** -> **Symmetric** -> **Encrypt and decrypt**.
   - Alias: `istpetdev-tfstate`.
   - Administrador y usuario de la clave: seleccionar el rol administrativo SSO de Bryan (`IstpetDevInfraAdmin`).
   - Guardar el ARN resultante (ej. `arn:aws:kms:us-east-1:<ACCOUNT_ID>:key/<KEY_ID>`).

### 2.2 Crear Bucket S3 para Terraform State
1. En [Amazon S3](https://console.aws.amazon.com/s3/):
   - **Create bucket**: Nombre `istpetdev-tfstate-<ACCOUNT_ID>-us-east-1` en región `us-east-1`.
   - **Object Ownership**: Bucket owner enforced (ACLs disabled).
   - **Block Public Access**: Activado al 100%.
   - **Bucket Versioning**: **Enable** (obligatorio).
   - **Default encryption**: SSE-KMS con la clave `istpetdev-tfstate` creada en el paso anterior.
   - **Create bucket**.

### 2.3 Aplicar Política de Enlace Seguro (TLS) al Bucket de State
En **Permissions -> Bucket policy -> Edit**, pegue la siguiente política oficial sustituyendo `<BUCKET_NAME>`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "EnforceTLSRequestsOnly",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:*",
      "Resource": [
        "arn:aws:s3:::<BUCKET_NAME>",
        "arn:aws:s3:::<BUCKET_NAME>/*"
      ],
      "Condition": {
        "Bool": {
          "aws:SecureTransport": "false"
        }
      }
    }
  ]
}
```

### 2.4 Crear Archivo Privado de Configuración Backend
En la máquina del Administrador, cree el archivo privado fuera del repositorio Git:

```bash
mkdir -p "$HOME/.config/istpetdev/private"
chmod 700 "$HOME/.config/istpetdev/private"
cat << 'EOF' > "$HOME/.config/istpetdev/private/backend-dev.hcl"
bucket       = "istpetdev-tfstate-<ACCOUNT_ID>-us-east-1"
key          = "dev/terraform.tfstate"
region       = "us-east-1"
encrypt      = true
kms_key_id   = "arn:aws:kms:us-east-1:<ACCOUNT_ID>:key/<KEY_ID>"
use_lockfile = true
EOF
chmod 600 "$HOME/.config/istpetdev/private/backend-dev.hcl"
```

---

## 3. Aprovisionamiento con Terraform Dev

Una vez autenticado con el perfil administrativo de AWS (`aws sso login --profile istpetdev-admin`):

```bash
cd /home/bryan/Projects/ISTPETDEV
export AWS_PROFILE=istpetdev-admin
export AWS_REGION=us-east-1

# 1. Inicializar Terraform con el backend S3 persistente
terraform -chdir=infra/terraform/environments/dev init \
  -backend-config="$HOME/.config/istpetdev/private/backend-dev.hcl"

# 2. Generar y revisar el plan de ejecución
terraform -chdir=infra/terraform/environments/dev plan \
  -out="$HOME/.config/istpetdev/private/dev.tfplan"

# 3. Aplicar los cambios en AWS
terraform -chdir=infra/terraform/environments/dev apply \
  "$HOME/.config/istpetdev/private/dev.tfplan"

# 4. Exportar los outputs en formato JSON
terraform -chdir=infra/terraform/environments/dev output -json \
  > "$HOME/.config/istpetdev/private/dev-outputs.json"
chmod 600 "$HOME/.config/istpetdev/private/dev-outputs.json"

# 5. Generar la configuración pública dev-access.json para el equipo
scripts/ops/export-dev-config.sh \
  --outputs "$HOME/.config/istpetdev/private/dev-outputs.json" \
  --output-dir "$HOME/.config/istpetdev/private"
```

---

## 4. Inicialización de Bases de Datos y Secretos (Bootstrap)

La base RDS PostgreSQL se crea limpia. Para aprovisionar las 5 bases personales y la base de integración:

### Terminal A: Abrir el Túnel de Administrador
```bash
scripts/ops/dev-tunnel.sh \
  --profile istpetdev-admin \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
```

### Terminal B: Ejecutar el Bootstrap
```bash
scripts/base_datos/bootstrap-dev.sh \
  --profile istpetdev-admin \
  --config "$HOME/.config/istpetdev/private/dev-access.json" \
  --outputs "$HOME/.config/istpetdev/private/dev-outputs.json"
```

El script realiza de manera **idempotente y segura**:
1. Conecta con el usuario `istpetadmin` obteniendo su contraseña desde AWS Secrets Manager.
2. Crea `istpetdev_dev_01` a `istpetdev_dev_05` y `istpetdev_integration`.
3. Revoca `CONNECT` de `PUBLIC` en todas las bases para aislamiento estricto.
4. Genera contraseñas criptográficas en memoria y las almacena en Secrets Manager:
   - `/istpetdev/dev/db/dev_01` .. `dev_05` (roles de aplicación `user_dev_XX`)
   - `/istpetdev/dev/db/dev_01/migrator` .. `dev_05/migrator` (roles migradores `migrator_dev_XX`)
   - `/istpetdev/dev/db/integration/dev_01` .. `dev_05` (usuarios de integración)
   - `/istpetdev/dev/db/integration/migrator` (migrador de integración)
5. Activa la extensión **PostGIS** (`CREATE EXTENSION IF NOT EXISTS postgis;`) en todas las bases.

---

## 5. Renderizar Políticas de Mínimo Privilegio (IAM Identity Center)

Ejecute el renderizador para materializar las políticas inline con los ARNs reales generados:

```bash
scripts/ops/render-dev-policies.sh \
  --profile istpetdev-admin \
  --config "$HOME/.config/istpetdev/private/dev-access.json" \
  --output-dir "$HOME/.config/istpetdev/private/dev-policies"
```

### Configuración en la Consola AWS IAM Identity Center:
1. En **Permission sets -> Create permission set -> Custom permission set -> Inline policy**:
   - Pegue el contenido de `$HOME/.config/istpetdev/private/dev-policies/dev_01.json`.
   - Nombre: `IstpetDevDev01` (sesión: 1 hora).
2. Asigne el permission set `IstpetDevDev01` exclusivamente al usuario `dev_01`.
3. Repita para `Dev02` a `Dev05` con sus respectivos archivos JSON.

---

## 6. Monitoreo y Control de Costos (AWS Budgets)

1. En **AWS Billing -> Budgets -> Create budget -> Cost budget**:
   - Tipo: Mensual.
   - Umbral de alertas: Avisos automáticos por correo al 50%, 80% y 100% del presupuesto acordado.
2. **Apagado operativo**:
   - Cuando el equipo no esté desarrollando activamente, detenga el nodo EC2 de acceso desde la consola o CLI para ahorrar horas de cómputo.
   - La instancia RDS dev se puede detener de forma coordinada (recordar que AWS reinicia instancias RDS detenidas automáticamente tras 7 días).
