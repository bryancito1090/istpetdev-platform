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
  default     = "db.t4g.medium"
}
