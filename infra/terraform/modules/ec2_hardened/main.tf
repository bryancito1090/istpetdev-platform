data "aws_ami" "ubuntu_24_04" {
  most_recent = true
  owners      = ["099720109477"] # Canonical

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd*/ubuntu-noble-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# IAM Role for SSM managed instance
resource "aws_iam_role" "ssm_node" {
  name = "istpetdev-ssm-access-node-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = merge(var.tags, {
    Name = "istpetdev-ssm-role-${var.environment}"
  })
}

resource "aws_iam_role_policy_attachment" "ssm_core" {
  role       = aws_iam_role.ssm_node.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_instance_profile" "ssm_profile" {
  name = "istpetdev-ssm-instance-profile-${var.environment}"
  role = aws_iam_role.ssm_node.name

  tags = var.tags
}

# EC2 Access Node
resource "aws_instance" "access_node" {
  ami                         = data.aws_ami.ubuntu_24_04.id
  instance_type               = var.instance_type
  subnet_id                   = var.subnet_id
  vpc_security_group_ids      = [var.security_group_id]
  iam_instance_profile        = aws_iam_instance_profile.ssm_profile.name
  associate_public_ip_address = true

  user_data                   = <<-SCRIPT
    #!/bin/bash
    set -euo pipefail
    systemctl enable --now snap.amazon-ssm-agent.amazon-ssm-agent.service
  SCRIPT
  user_data_replace_on_change = true

  # Enforce IMDSv2 strictly
  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 1
  }

  # Root volume: gp3 encrypted
  root_block_device {
    volume_type           = "gp3"
    volume_size           = 20
    encrypted             = true
    delete_on_termination = true
    tags = merge(var.tags, {
      Name = "istpetdev-access-node-root-${var.environment}"
    })
  }

  tags = merge(var.tags, {
    Name = "istpetdev-access-node-${var.environment}"
    Role = "SSMAccessTunnel"
  })
}
