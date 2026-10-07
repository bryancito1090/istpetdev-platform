resource "aws_db_subnet_group" "rds" {
  name        = "istpetdev-rds-subnet-group-${var.environment}"
  description = "DB subnet group for private RDS instance covering two AZs"
  subnet_ids  = var.subnet_ids

  tags = merge(var.tags, {
    Name = "istpetdev-rds-subnet-group-${var.environment}"
  })
}

resource "aws_db_parameter_group" "pg16" {
  name        = "istpetdev-pg16-params-${var.environment}"
  family      = "postgres16"
  description = "Custom parameter group enforcing SSL in transit"

  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }

  tags = var.tags
}

resource "aws_db_instance" "postgres" {
  identifier                  = "istpetdev-rds-${var.environment}"
  engine                      = "postgres"
  engine_version              = "16"
  instance_class              = var.instance_class
  allocated_storage           = var.allocated_storage
  max_allocated_storage       = var.max_allocated_storage
  storage_type                = "gp3"
  storage_encrypted           = true
  publicly_accessible         = false
  multi_az                    = false
  db_subnet_group_name        = aws_db_subnet_group.rds.name
  parameter_group_name        = aws_db_parameter_group.pg16.name
  vpc_security_group_ids      = [var.security_group_id]
  auto_minor_version_upgrade  = true
  allow_major_version_upgrade = false
  backup_retention_period     = 7
  backup_window               = "03:00-04:00"
  maintenance_window          = "Sun:04:30-Sun:05:30"
  deletion_protection         = true
  skip_final_snapshot         = false
  final_snapshot_identifier   = "istpetdev-rds-${var.environment}-final-snapshot"

  # Master User Credentials managed by AWS Secrets Manager natively
  username                    = "istpetadmin"
  manage_master_user_password = true

  tags = merge(var.tags, {
    Name = "istpetdev-rds-${var.environment}"
  })
}
