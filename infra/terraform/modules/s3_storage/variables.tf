variable "bucket_name" {
  type        = string
  description = "The name of the S3 bucket"
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
