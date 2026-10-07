variable "vpc_cidr" {
  type        = string
  description = "CIDR block for the VPC"
  default     = "10.42.0.0/16"
}

variable "public_subnets" {
  type        = list(string)
  description = "List of public subnet CIDR blocks"
  default     = ["10.42.0.0/24", "10.42.1.0/24"]
}

variable "private_data_subnets" {
  type        = list(string)
  description = "List of private data subnet CIDR blocks for RDS"
  default     = ["10.42.20.0/24", "10.42.21.0/24"]
}

variable "availability_zones" {
  type        = list(string)
  description = "List of AZ names"
  default     = ["us-east-1a", "us-east-1b"]
}

variable "environment" {
  type        = string
  description = "Environment name (e.g. dev)"
  default     = "dev"
}

variable "tags" {
  type        = map(string)
  description = "Common tags"
  default     = {}
}
