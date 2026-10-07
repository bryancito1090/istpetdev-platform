output "vpc_id" {
  description = "The ID of the VPC"
  value       = module.vpc.vpc_id
}

output "public_subnet_ids" {
  description = "IDs of the public subnets"
  value       = module.vpc.public_subnet_ids
}

output "private_data_subnet_ids" {
  description = "IDs of the private data subnets for RDS"
  value       = module.vpc.private_data_subnet_ids
}

output "ec2_access_node_id" {
  description = "Instance ID of the EC2 access node"
  value       = module.ec2_access.instance_id
}

output "ec2_access_node_public_ip" {
  description = "Public IP of the EC2 access node"
  value       = module.ec2_access.public_ip
}

output "ec2_access_security_group_id" {
  description = "Security group ID of the EC2 access node"
  value       = module.security_groups.ec2_access_security_group_id
}

output "rds_instance_id" {
  description = "Identifier of the RDS instance"
  value       = module.rds.db_instance_id
}

output "rds_endpoint" {
  description = "Connection endpoint of the RDS instance (host:port)"
  value       = module.rds.endpoint
}

output "rds_address" {
  description = "DNS hostname address of the RDS instance"
  value       = module.rds.address
}

output "rds_port" {
  description = "Listening port of the RDS instance"
  value       = module.rds.port
}

output "rds_security_group_id" {
  description = "Security group ID of the RDS instance"
  value       = module.security_groups.rds_security_group_id
}

output "rds_master_user_secret_arn" {
  description = "ARN of the Secrets Manager secret for the RDS master user password"
  value       = module.rds.master_user_secret_arn
}

output "evidence_bucket_name" {
  description = "Name of the S3 evidence bucket"
  value       = module.s3_evidence.bucket_id
}

output "evidence_bucket_arn" {
  description = "ARN of the S3 evidence bucket"
  value       = module.s3_evidence.bucket_arn
}
