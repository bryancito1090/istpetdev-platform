variable "subnet_id" {
  type        = string
  description = "Public subnet ID where the access node is launched"
}

variable "security_group_id" {
  type        = string
  description = "Security group ID for the access node (no inbound rules)"
}

variable "instance_type" {
  type        = string
  description = "Instance type for the access node"
  default     = "t3.micro"
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
