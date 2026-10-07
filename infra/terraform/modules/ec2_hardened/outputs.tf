output "instance_id" {
  description = "The ID of the EC2 access node"
  value       = aws_instance.access_node.id
}

output "instance_arn" {
  description = "The ARN of the EC2 access node"
  value       = aws_instance.access_node.arn
}

output "public_ip" {
  description = "Public IP of the access node (for outbound egress verification)"
  value       = aws_instance.access_node.public_ip
}

output "iam_role_arn" {
  description = "The ARN of the IAM role attached to the access node"
  value       = aws_iam_role.ssm_node.arn
}
