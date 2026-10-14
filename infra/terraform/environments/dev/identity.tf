resource "aws_cognito_user_pool" "dev" {
  name                     = "istpetdev-dev"
  deletion_protection      = var.hackathon_closed ? "INACTIVE" : "ACTIVE"
  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]
  mfa_configuration        = "ON"
  software_token_mfa_configuration { enabled = true }
  admin_create_user_config { allow_admin_create_user_only = true }
  password_policy {
    minimum_length                   = 14
    require_lowercase                = true
    require_uppercase                = true
    require_numbers                  = true
    require_symbols                  = true
    temporary_password_validity_days = 1
  }
  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }
}

resource "aws_cognito_user_pool_domain" "dev" {
  domain       = "istpetdev-dev-${data.aws_caller_identity.current.account_id}"
  user_pool_id = aws_cognito_user_pool.dev.id
}

resource "aws_cognito_resource_server" "api" {
  identifier   = "istpetdev-api"
  name         = "IstpetDev API"
  user_pool_id = aws_cognito_user_pool.dev.id
  scope {
    scope_name        = "access"
    scope_description = "Human API access; database RBAC still applies"
  }
  scope {
    scope_name        = "automation"
    scope_description = "Automation M2M API access for n8n; human decision endpoints forbidden"
  }
}

resource "aws_cognito_user_pool_client" "human" {
  for_each                             = { web = 4200, mobile = 8100 }
  name                                 = "istpetdev-dev-${each.key}"
  user_pool_id                         = aws_cognito_user_pool.dev.id
  generate_secret                      = false
  supported_identity_providers         = ["COGNITO"]
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["code"]
  allowed_oauth_scopes                 = ["openid", "email", "profile", "${aws_cognito_resource_server.api.identifier}/access"]
  callback_urls                        = ["http://localhost:${each.value}/auth/callback"]
  logout_urls                          = ["http://localhost:${each.value}"]
  explicit_auth_flows                  = []
  prevent_user_existence_errors        = "ENABLED"
  enable_token_revocation              = true
  access_token_validity                = 15
  id_token_validity                    = 15
  refresh_token_validity               = 7
  token_validity_units {
    access_token  = "minutes"
    id_token      = "minutes"
    refresh_token = "days"
  }
  refresh_token_rotation {
    feature                    = "ENABLED"
    retry_grace_period_seconds = 10
  }
}

resource "aws_cognito_user_pool_client" "automation" {
  name                                 = "istpetdev-dev-n8n"
  user_pool_id                         = aws_cognito_user_pool.dev.id
  generate_secret                      = true
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["client_credentials"]
  allowed_oauth_scopes                 = ["${aws_cognito_resource_server.api.identifier}/automation"]
  explicit_auth_flows                  = []
  enable_token_revocation              = true
  access_token_validity                = 60
  token_validity_units {
    access_token = "minutes"
  }
}

output "cognito" {
  value = {
    issuer               = "https://cognito-idp.${var.aws_region}.amazonaws.com/${aws_cognito_user_pool.dev.id}"
    domain               = "https://${aws_cognito_user_pool_domain.dev.domain}.auth.${var.aws_region}.amazoncognito.com"
    client_ids           = { for k, c in aws_cognito_user_pool_client.human : k => c.id }
    automation_client_id = aws_cognito_user_pool_client.automation.id
    scope                = "${aws_cognito_resource_server.api.identifier}/access"
    automation_scope     = "${aws_cognito_resource_server.api.identifier}/automation"
  }
}
