locals {
  members         = [for n in range(1, 6) : format("dev_%02d", n)]
  db_secret_names = toset(concat(flatten([for m in local.members : [m, "${m}/migrator", "integration/${m}"]]), ["integration/migrator", "integration/backup"]))
}

# Only metadata in Terraform. Passwords are written by bootstrap-dev via a private file.
resource "aws_secretsmanager_secret" "database" {
  for_each                = local.db_secret_names
  name                    = "/istpetdev/dev/db/${each.key}"
  recovery_window_in_days = 7
}

# Fixed remote endpoint prevents forwarding to IMDS or arbitrary hosts/ports.
resource "aws_ssm_document" "database_tunnel" {
  name            = "istpetdev-dev-postgres-tunnel"
  document_type   = "Session"
  document_format = "JSON"
  content = jsonencode({
    schemaVersion = "1.0"
    description   = "IstpetDev dev PostgreSQL tunnel only"
    sessionType   = "Port"
    parameters = {
      localPortNumber = { type = "String", default = "15432", allowedPattern = "^([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$" }
    }
    properties = { host = module.rds.address, portNumber = "5432", localPortNumber = "{{ localPortNumber }}", type = "LocalPortForwarding" }
  })
}

data "external" "mfa" {
  for_each = var.team_users
  program  = ["python3", "${path.module}/../../../../scripts/ops/check-mfa.py"]
  query    = { username = each.value }
}

resource "aws_iam_user_policy" "developer" {
  for_each = var.team_users
  user     = each.value
  name     = "IstpetDevIndividualDevAccess"
  lifecycle {
    precondition {
      condition     = data.external.mfa[each.key].result.enabled == "true"
      error_message = "MFA enrollment is required before granting project data access."
    }
  }
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      { Sid = "TunnelNode", Effect = "Allow", Action = "ssm:StartSession", Resource = module.ec2_access.instance_arn,
      Condition = { Bool = { "ssm:SessionDocumentAccessCheck" = "true", "aws:MultiFactorAuthPresent" = "true" } } },
      { Sid = "TunnelDocument", Effect = "Allow", Action = "ssm:StartSession", Resource = aws_ssm_document.database_tunnel.arn,
      Condition = { Bool = { "aws:MultiFactorAuthPresent" = "true" } } },
      { Sid = "OwnSessions", Effect = "Allow", Action = ["ssm:ResumeSession", "ssm:TerminateSession"],
      Resource = "arn:aws:ssm:${var.aws_region}:${data.aws_caller_identity.current.account_id}:session/$${aws:username}-*" },
      { Sid      = "AssignedSecrets", Effect = "Allow", Action = ["secretsmanager:GetSecretValue", "secretsmanager:DescribeSecret"],
        Resource = [for k in [each.key, "${each.key}/migrator", "integration/${each.key}"] : aws_secretsmanager_secret.database[k].arn],
      Condition = { Bool = { "aws:MultiFactorAuthPresent" = "true" } } },
      { Sid      = "OwnEvidence", Effect = "Allow", Action = ["s3:GetObject", "s3:GetObjectVersion", "s3:PutObject", "s3:AbortMultipartUpload"],
        Resource = "${module.s3_evidence.bucket_arn}/${each.key}/*",
      Condition = { Bool = { "aws:MultiFactorAuthPresent" = "true" } } },
      { Sid = "ListOwnEvidence", Effect = "Allow", Action = "s3:ListBucket", Resource = module.s3_evidence.bucket_arn,
      Condition = { StringLike = { "s3:prefix" = ["${each.key}/*"] }, Bool = { "aws:MultiFactorAuthPresent" = "true" } } }
    ]
  })
}

resource "aws_iam_user_policy_attachment" "developer_login" {
  for_each   = var.team_users
  user       = each.value
  policy_arn = "arn:aws:iam::aws:policy/SignInLocalDevelopmentAccess"
}

resource "aws_iam_openid_connect_provider" "github" {
  count          = var.github_oidc_provider_arn == null ? 1 : 0
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
}

# Prepared identity only. Demo SG permissions belong to the later, manual demo stack.
resource "aws_iam_role" "github_demo" {
  name                 = "istpetdev-github-demo"
  max_session_duration = 3600
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow", Action = "sts:AssumeRoleWithWebIdentity"
      Principal = { Federated = var.github_oidc_provider_arn != null ? var.github_oidc_provider_arn : aws_iam_openid_connect_provider.github[0].arn }
      Condition = { StringEquals = { "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com", "token.actions.githubusercontent.com:sub" = var.github_oidc_subject } }
    }]
  })
}

output "database_secret_arns" { value = { for k, s in aws_secretsmanager_secret.database : k => s.arn } }
output "ssm_tunnel_document" { value = aws_ssm_document.database_tunnel.name }
output "developer_policies" { value = { for k, p in aws_iam_user_policy.developer : k => jsondecode(p.policy) } }
output "github_demo_role_arn" { value = aws_iam_role.github_demo.arn }
