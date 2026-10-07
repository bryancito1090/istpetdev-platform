output "db_instance_id" {
  description = "The ID of the RDS instance"
  value       = aws_db_instance.postgres.id
}

output "db_instance_arn" {
  description = "The ARN of the RDS instance"
  value       = aws_db_instance.postgres.arn
}

output "endpoint" {
  description = "Connection endpoint of the RDS instance (host:port)"
  value       = aws_db_instance.postgres.endpoint
}

output "address" {
  description = "Hostname address of the RDS instance"
  value       = aws_db_instance.postgres.address
}

output "port" {
  description = "Port of the RDS instance"
  value       = aws_db_instance.postgres.port
}

output "master_user_secret_arn" {
  description = "ARN of the Secrets Manager secret storing the master user password"
  value       = try(aws_db_instance.postgres.master_user_secret[0].secret_arn, null)
}
