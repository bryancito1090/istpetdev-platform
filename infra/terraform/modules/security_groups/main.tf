# 1. Security Group for EC2 Access / Application Host (Perfil A Reforzado)
resource "aws_security_group" "ec2_access" {
  name        = "istpetdev-ec2-access-sg-${var.environment}"
  description = "Security group for EC2 host - inbound HTTP/HTTPS/SSH for demo and egress for SSM/RDS"
  vpc_id      = var.vpc_id

  dynamic "ingress" {
    for_each = var.enable_web_ingress ? [80, 443] : []
    content {
      description = "Allow inbound web traffic on port ${ingress.value} for reverse proxy and Cloudflare"
      from_port   = ingress.value
      to_port     = ingress.value
      protocol    = "tcp"
      cidr_blocks = ["0.0.0.0/0"]
    }
  }

  dynamic "ingress" {
    for_each = length(var.allowed_ssh_cidrs) > 0 ? [22] : []
    content {
      description = "Allow inbound SSH for deployment automation"
      from_port   = 22
      to_port     = 22
      protocol    = "tcp"
      cidr_blocks = var.allowed_ssh_cidrs
    }
  }

  # Outbound: HTTPS (443) for Systems Manager agent communication and outbound package verification
  egress {
    description = "HTTPS outbound for SSM Agent and AWS APIs"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "DNS UDP outbound"
    from_port   = 53
    to_port     = 53
    protocol    = "udp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "DNS TCP outbound"
    from_port   = 53
    to_port     = 53
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    description = "PostgreSQL outbound to RDS private security group"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["10.42.0.0/16"]
  }

  tags = merge(var.tags, {
    Name = "istpetdev-ec2-access-sg-${var.environment}"
  })
}

# 2. Security Group for RDS PostgreSQL
# Requirement: Port 5432 ONLY from the EC2 Access Node Security Group
resource "aws_security_group" "rds" {
  name        = "istpetdev-rds-sg-${var.environment}"
  description = "Security group for RDS PostgreSQL - port 5432 only from access node SG"
  vpc_id      = var.vpc_id

  ingress {
    description     = "PostgreSQL port 5432 exclusively from EC2 access node SG"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.ec2_access.id]
  }

  tags = merge(var.tags, {
    Name = "istpetdev-rds-sg-${var.environment}"
  })
}
