variable "subnet_ids" {
  type        = list(string)
  description = "List of private subnet IDs for the RDS DB subnet group (at least 2 AZs)"
}

variable "security_group_id" {
  type        = string
  description = "Security group ID for RDS"
}

variable "instance_class" {
  type        = string
  description = "The database instance class"
  default     = "db.t4g.medium"
}

variable "allocated_storage" {
  type        = number
  description = "Allocated storage in GB"
  default     = 20
}

variable "max_allocated_storage" {
  type        = number
  description = "Max allocated storage for autoscaling in GB"
  default     = 100
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
