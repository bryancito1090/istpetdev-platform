output "ec2_access_security_group_id" {
  description = "The ID of the EC2 access security group"
  value       = aws_security_group.ec2_access.id
}

output "rds_security_group_id" {
  description = "The ID of the RDS security group"
  value       = aws_security_group.rds.id
}
