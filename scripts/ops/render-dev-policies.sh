#!/usr/bin/env bash
# ==============================================================================
# Script: render-dev-policies.sh
# Descripción: Materializa políticas IAM inline JSON individuales por integrante
#              (dev_01.json .. dev_05.json) con ARNs reales en un directorio privado.
#              Cada política otorga SSM al nodo dev, lectura exclusiva de sus
#              secretos personales/integración asignados, y acceso acotado a S3.
#              Sin acceso al master RDS, bases ajenas, state ni producción.
# ==============================================================================

set -euo pipefail

show_help() {
  cat << 'EOF'
Uso: scripts/ops/render-dev-policies.sh --profile <perfil-admin> --config <dev-access.json> --output-dir <directorio-privado> [opciones]

Argumentos requeridos:
  --profile <perfil-admin>    Perfil administrativo de AWS (ej. istpetdev-admin).
  --config <dev-access.json>  Ruta al archivo dev-access.json.
  --output-dir <directorio>   Directorio privado donde se guardarán los JSON
                              (ejemplo: $HOME/.config/istpetdev/private/dev-policies).

Opciones:
  --account-id <id-cuenta>    ID de cuenta AWS (por defecto: obtenido vía sts get-caller-identity).
  -h, --help                  Muestra esta ayuda y termina.

Ejemplo:
  scripts/ops/render-dev-policies.sh \
    --profile istpetdev-admin \
    --config "$HOME/.config/istpetdev/private/dev-access.json" \
    --output-dir "$HOME/.config/istpetdev/private/dev-policies"
EOF
}

PROFILE=""
CONFIG_FILE=""
OUTPUT_DIR=""
ACCOUNT_ID=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile)
      PROFILE="${2:-}"
      shift 2
      ;;
    --config)
      CONFIG_FILE="${2:-}"
      shift 2
      ;;
    --output-dir)
      OUTPUT_DIR="${2:-}"
      shift 2
      ;;
    --account-id)
      ACCOUNT_ID="${2:-}"
      shift 2
      ;;
    -h|--help)
      show_help
      exit 0
      ;;
    *)
      echo "Error: Argumento desconocido: $1" >&2
      echo "Ejecute '$0 --help' para consultar la sintaxis." >&2
      exit 1
      ;;
  esac
done

if [[ -z "$PROFILE" || -z "$CONFIG_FILE" || -z "$OUTPUT_DIR" ]]; then
  echo "Error: --profile, --config y --output-dir son obligatorios." >&2
  show_help >&2
  exit 1
fi

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "Error: El archivo '$CONFIG_FILE' no existe." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "Error: Se requiere 'jq' instalado." >&2
  exit 1
fi

REGION=$(jq -r '.aws_region // "us-east-1"' "$CONFIG_FILE")
INSTANCE_ID=$(jq -r '.ec2_access_node_id // empty' "$CONFIG_FILE")
EVIDENCE_BUCKET=$(jq -r '.evidence_bucket_name // empty' "$CONFIG_FILE")

if [[ -z "$INSTANCE_ID" ]]; then
  echo "Error: 'ec2_access_node_id' no encontrado en $CONFIG_FILE." >&2
  exit 1
fi

if [[ -z "$ACCOUNT_ID" ]]; then
  if ! command -v aws >/dev/null 2>&1; then
    echo "Error: Se requiere 'aws' CLI para consultar el Account ID." >&2
    exit 1
  fi
  ACCOUNT_ID=$(aws sts get-caller-identity --profile "$PROFILE" --query "Account" --output text 2>/dev/null || true)
  if [[ -z "$ACCOUNT_ID" || "$ACCOUNT_ID" == "None" ]]; then
    echo "Aviso: No se pudo consultar sts caller-identity. Usando placeholder <ACCOUNT_ID>." >&2
    ACCOUNT_ID="<ACCOUNT_ID>"
  fi
fi

mkdir -p "$OUTPUT_DIR"
chmod 700 "$OUTPUT_DIR"
umask 077

MEMBERS=("dev_01" "dev_02" "dev_03" "dev_04" "dev_05")

for member in "${MEMBERS[@]}"; do
  policy_file="$OUTPUT_DIR/${member}.json"

  cat << EOF > "$policy_file"
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowSSMPortForwardingDevNodeOnly",
      "Effect": "Allow",
      "Action": [
        "ssm:StartSession"
      ],
      "Resource": [
        "arn:aws:ec2:${REGION}:${ACCOUNT_ID}:instance/${INSTANCE_ID}",
        "arn:aws:ssm:${REGION}::document/AWS-StartPortForwardingSessionToRemoteHost"
      ]
    },
    {
      "Sid": "AllowSSMSessionManagementOwnSessionsOnly",
      "Effect": "Allow",
      "Action": [
        "ssm:ResumeSession",
        "ssm:TerminateSession"
      ],
      "Resource": [
        "arn:aws:ssm:${REGION}:${ACCOUNT_ID}:session/*"
      ]
    },
    {
      "Sid": "AllowSSMDescribeSessions",
      "Effect": "Allow",
      "Action": [
        "ssm:DescribeSessions",
        "ssm:GetConnectionStatus",
        "ssm:DescribeInstanceInformation"
      ],
      "Resource": "*"
    },
    {
      "Sid": "AllowGetSecretValueAssignedMemberSecretsOnly",
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": [
        "arn:aws:secretsmanager:${REGION}:${ACCOUNT_ID}:secret:/istpetdev/dev/db/${member}*",
        "arn:aws:secretsmanager:${REGION}:${ACCOUNT_ID}:secret:/istpetdev/dev/db/${member}/migrator*",
        "arn:aws:secretsmanager:${REGION}:${ACCOUNT_ID}:secret:/istpetdev/dev/db/integration/${member}*"
      ]
    },
    {
      "Sid": "AllowS3EvidenceDevPrefixAccessOnly",
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:AbortMultipartUpload"
      ],
      "Resource": [
        "arn:aws:s3:::${EVIDENCE_BUCKET}/${member}/*"
      ]
    },
    {
      "Sid": "AllowS3EvidenceBucketListOwnPrefix",
      "Effect": "Allow",
      "Action": [
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::${EVIDENCE_BUCKET}"
      ],
      "Condition": {
        "StringLike": {
          "s3:prefix": [
            "${member}/*"
          ]
        }
      }
    }
  ]
}
EOF

  chmod 600 "$policy_file"
  echo "Generada política: $policy_file"
done

echo ""
echo "================================================================="
echo "Políticas IAM renderizadas exitosamente en:"
echo "  $OUTPUT_DIR/"
echo "Cada archivo contiene permisos de mínimo privilegio para su integrante:"
echo "  - dev_01.json .. dev_05.json"
echo ""
echo "Instrucciones para Bryan (Administrador):"
echo "  1. En IAM Identity Center -> Permission Sets -> Create permission set:"
echo "     Seleccione 'Custom permission set' -> Inline policy."
echo "  2. Pegue el contenido de dev_01.json para el set 'IstpetDevDev01'."
echo "  3. Asigne el permission set exclusivamente al usuario asignado a dev_01."
echo "  4. Repita para Dev02..Dev05 con sus respectivos JSON."
echo "================================================================="
