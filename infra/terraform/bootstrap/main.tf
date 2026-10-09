terraform {
  required_version = ">= 1.10.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.50"
    }
  }
  # First apply uses a generated, ignored backend_override.tf pointing outside Git.
  # Afterwards migrate that state to bootstrap/terraform.tfstate in this bucket.
  backend "s3" {}
}

provider "aws" {
  region = "us-east-1"
  default_tags {
    tags = { Project = "IstpetDev", Environment = "bootstrap", ManagedBy = "Terraform", ExpiresAt = var.expires_at }
  }
}

variable "expires_at" {
  type = string
  validation {
    condition     = can(formatdate("YYYY-MM-DD", "${var.expires_at}T00:00:00Z"))
    error_message = "Use an agreed date in YYYY-MM-DD format."
  }
}

data "aws_caller_identity" "current" {}
data "aws_kms_key" "state" { key_id = "alias/istpetdev-tfstate" }

resource "aws_s3_bucket" "state" {
  bucket        = "istpetdev-tfstate-${data.aws_caller_identity.current.account_id}-us-east-1"
  force_destroy = false
  lifecycle {
    prevent_destroy = true
    precondition {
      condition     = data.aws_kms_key.state.enabled && data.aws_kms_key.state.key_spec == "SYMMETRIC_DEFAULT"
      error_message = "The existing state KMS key must be enabled and symmetric."
    }
  }
}

resource "aws_s3_bucket_public_access_block" "state" {
  bucket                  = aws_s3_bucket.state.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "state" {
  bucket = aws_s3_bucket.state.id
  rule { object_ownership = "BucketOwnerEnforced" }
}

resource "aws_s3_bucket_versioning" "state" {
  bucket = aws_s3_bucket.state.id
  versioning_configuration { status = "Enabled" }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "state" {
  bucket = aws_s3_bucket.state.id
  rule {
    bucket_key_enabled = true
    apply_server_side_encryption_by_default {
      sse_algorithm     = "aws:kms"
      kms_master_key_id = data.aws_kms_key.state.arn
    }
  }
}

resource "aws_s3_bucket_policy" "state" {
  bucket = aws_s3_bucket.state.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "RequireTLS", Effect = "Deny", Principal = "*", Action = "s3:*"
      Resource  = [aws_s3_bucket.state.arn, "${aws_s3_bucket.state.arn}/*"]
      Condition = { Bool = { "aws:SecureTransport" = "false" } }
    }]
  })
  depends_on = [aws_s3_bucket_public_access_block.state]
}

output "bucket" { value = aws_s3_bucket.state.id }
output "kms_key_arn" { value = data.aws_kms_key.state.arn }
