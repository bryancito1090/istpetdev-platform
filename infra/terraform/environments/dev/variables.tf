variable "aws_region" {
  type        = string
  description = "AWS region for development environment"
  default     = "us-east-1"
}

variable "environment" {
  type        = string
  description = "Environment identifier"
  default     = "dev"
}

variable "vpc_cidr" {
  type        = string
  description = "CIDR block for the dedicated dev VPC"
  default     = "10.42.0.0/16"
}

variable "public_subnets" {
  type        = list(string)
  description = "Public subnet CIDRs"
  default     = ["10.42.0.0/24", "10.42.1.0/24"]
}

variable "private_data_subnets" {
  type        = list(string)
  description = "Private data subnet CIDRs for RDS"
  default     = ["10.42.20.0/24", "10.42.21.0/24"]
}

variable "availability_zones" {
  type        = list(string)
  description = "AZs for high availability across subnets"
  default     = ["us-east-1a", "us-east-1b"]
}

variable "ec2_instance_type" {
  type        = string
  description = "Instance type for the EC2 access node"
  default     = "t3.micro"
}

variable "rds_instance_class" {
  type        = string
  description = "RDS instance class"
  default     = "db.t3.micro"
}
variable "expires_at" {
  type        = string
  description = "Agreed hackathon expiry date, metadata only; never triggers deletion."
  validation {
    condition     = can(formatdate("YYYY-MM-DD", "${var.expires_at}T00:00:00Z"))
    error_message = "Use YYYY-MM-DD."
  }
}

variable "hackathon_closed" {
  type        = bool
  default     = false
  description = "Set only after explicit event closure and verified external export."
}

variable "free_plan_backup_workaround" {
  type        = bool
  default     = false
  description = "Explicitly accepted alternative: one day PITR plus daily AWS Backup snapshots retained seven days."
}

variable "team_users" {
  type        = map(string)
  default     = {}
  description = "Private mapping dev_01..05 to existing IAM usernames; MFA checked live before grants."
  validation {
    condition     = alltrue([for member, user in var.team_users : can(regex("^dev_0[1-5]$", member)) && can(regex("^istpetdev-[A-Za-z0-9+=,.@_-]+$", user))]) && length(distinct(values(var.team_users))) == length(var.team_users)
    error_message = "Assign each existing project IAM user to one dev_01..05 only."
  }
}

variable "monthly_budget_usd" {
  type        = number
  description = "Gross monthly account budget agreed by Bryan (credits excluded)."
  validation {
    condition     = var.monthly_budget_usd > 0
    error_message = "Budget must be positive."
  }
}

variable "alert_emails" {
  type        = set(string)
  description = "Authorized budget/operational alert recipients. SNS subscription needs confirmation."
  validation {
    condition     = length(var.alert_emails) > 0 && alltrue([for e in var.alert_emails : can(regex("^[^@ ]+@[^@ ]+\\.[^@ ]+$", e))])
    error_message = "Provide at least one valid recipient."
  }
}

variable "github_oidc_provider_arn" {
  type        = string
  default     = null
  description = "Reuse the account's existing GitHub OIDC provider if inventory finds one."
}

variable "github_oidc_subject" {
  type        = string
  description = "Exact subject confirmed from GitHub API; no wildcard."
  validation {
    condition     = startswith(var.github_oidc_subject, "repo:bryancito1090") && !strcontains(var.github_oidc_subject, "*") && !strcontains(var.github_oidc_subject, "?")
    error_message = "Use the verified exact repository/environment or main branch subject."
  }
}

variable "app_public_origin" {
  type        = string
  description = "Origen HTTPS público de la web; la PWA vive en /campo/ (spec 013)"
  default     = "https://istpetdev.doicela.dev"
  validation {
    condition     = can(regex("^https://[a-z0-9.-]+$", var.app_public_origin))
    error_message = "Debe ser https://dominio sin barra final."
  }
}
