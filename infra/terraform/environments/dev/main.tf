terraform {
  required_version = ">= 1.10.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.50"
    }
  }

  backend "s3" {
    # Backend configuration is provided via backend-dev.hcl outside of version control:
    # terraform init -backend-config=$HOME/.config/istpetdev/private/backend-dev.hcl
    # use_lockfile = true
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "IstpetDev"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

data "aws_caller_identity" "current" {}

locals {
  common_tags = {
    Project     = "IstpetDev"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
  evidence_bucket_name = "istpetdev-evidence-${var.environment}-${try(data.aws_caller_identity.current.account_id, "account")}-${var.aws_region}"
}

# 1. VPC Module (Dedicated 10.42.0.0/16, 2 AZs, NO NAT Gateway)
module "vpc" {
  source = "../../modules/vpc"

  vpc_cidr             = var.vpc_cidr
  public_subnets       = var.public_subnets
  private_data_subnets = var.private_data_subnets
  availability_zones   = var.availability_zones
  environment          = var.environment
  tags                 = local.common_tags
}

# 2. Security Groups Module (EC2 Access SG no inbound, RDS SG allows 5432 only from EC2 Access SG)
module "security_groups" {
  source = "../../modules/security_groups"

  vpc_id      = module.vpc.vpc_id
  environment = var.environment
  tags        = local.common_tags
}

# 3. EC2 Access Node Module (t3.micro, Ubuntu 24.04, SSM role, IMDSv2, no inbound)
module "ec2_access" {
  source = "../../modules/ec2_hardened"

  subnet_id         = module.vpc.public_subnet_ids[0]
  security_group_id = module.security_groups.ec2_access_security_group_id
  instance_type     = var.ec2_instance_type
  environment       = var.environment
  tags              = local.common_tags
}

# 4. RDS PostgreSQL 16 Module (db.t4g.medium Single-AZ, gp3, force_ssl, Secrets Manager master password)
module "rds" {
  source = "../../modules/rds_postgres"

  subnet_ids        = module.vpc.private_data_subnet_ids
  security_group_id = module.security_groups.rds_security_group_id
  instance_class    = var.rds_instance_class
  environment       = var.environment
  tags              = local.common_tags
}

# 5. S3 Evidence Storage Module (Private, versioned, AES256, TLS policy)
module "s3_evidence" {
  source = "../../modules/s3_storage"

  bucket_name = local.evidence_bucket_name
  environment = var.environment
  tags        = local.common_tags
}
