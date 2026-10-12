variable "vpc_id" {
  type        = string
  description = "The ID of the VPC"
}

variable "environment" {
  type        = string
  description = "Environment name (e.g. dev)"
  default     = "dev"
}

variable "enable_web_ingress" {
  type        = bool
  description = "Habilitar puertos HTTP (80) y HTTPS (443) para el proxy web de produccion/demo"
  default     = true
}

variable "allowed_ssh_cidrs" {
  type        = list(string)
  description = "Lista de bloques CIDR autorizados para acceso SSH administrativo (puerto 22)"
  default     = ["0.0.0.0/0"]
}

variable "tags" {
  type        = map(string)
  description = "Common tags"
  default     = {}
}
