# Host de la app (i-0e0a979f8c0885a4f): creado a mano y fuera de Terraform (#14).
# Aquí solo viven su rol y su permiso sobre Cognito (spec 013, NFR-02). La asociación al host se hace por CLI.
variable "app_host_existing_role_name" {
  type        = string
  description = "Rama B del Step 1 de 013-T10: nombre del rol que el host ya tiene. Vacío = rama A (crear rol y perfil)."
  default     = ""
}

locals {
  create_app_host_role = var.app_host_existing_role_name == ""
}

resource "aws_iam_role" "app_host" {
  count = local.create_app_host_role ? 1 : 0
  name  = "istpetdev-app-host-dev"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [{ Effect = "Allow", Principal = { Service = "ec2.amazonaws.com" }, Action = "sts:AssumeRole" }]
  })
}

resource "aws_iam_instance_profile" "app_host" {
  count = local.create_app_host_role ? 1 : 0
  name  = "istpetdev-app-host-dev"
  role  = aws_iam_role.app_host[0].name
}

resource "aws_iam_role_policy" "app_cognito_admin" {
  name = "istpetdev-app-cognito-admin-dev"
  role = local.create_app_host_role ? aws_iam_role.app_host[0].name : var.app_host_existing_role_name
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "AccountAdministrationSpec013"
      Effect = "Allow"
      Action = [
        "cognito-idp:AdminCreateUser",
        "cognito-idp:AdminDisableUser",
        "cognito-idp:AdminEnableUser",
        "cognito-idp:AdminUserGlobalSignOut",
        "cognito-idp:AdminGetUser",
        "cognito-idp:ListUsers",
      ]
      Resource = aws_cognito_user_pool.dev.arn
    }]
  })
}

output "app_host_instance_profile" {
  value = local.create_app_host_role ? aws_iam_instance_profile.app_host[0].name : null
}
