variable "vpc_id" {
  type        = string
  description = "The ID of the VPC"
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
