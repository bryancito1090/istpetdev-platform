module "s3_backups" {
  source      = "../../modules/s3_storage"
  bucket_name = "istpetdev-backups-dev-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  environment = var.environment
  tags        = local.common_tags
}

resource "aws_s3_bucket_cors_configuration" "evidence" {
  bucket = module.s3_evidence.bucket_id
  cors_rule {
    allowed_origins = ["http://localhost:4200", "http://localhost:8100", "https://istpetdev.doicela.dev", "https://clean.doicela.dev"]
    allowed_methods = ["GET", "HEAD", "POST", "PUT"]
    allowed_headers = ["content-type", "x-amz-checksum-sha256", "x-amz-meta-sha256"]
    expose_headers  = ["ETag", "x-amz-checksum-sha256", "x-amz-version-id"]
    max_age_seconds = 300
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "evidence" {
  bucket = module.s3_evidence.bucket_id
  rule {
    id     = "temporary-evidence"
    status = "Enabled"
    filter { prefix = "" }
    expiration { date = timeadd("${var.expires_at}T00:00:00Z", "720h") }
    noncurrent_version_expiration { noncurrent_days = 30 }
    abort_incomplete_multipart_upload { days_after_initiation = 7 }
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "backups" {
  bucket = module.s3_backups.bucket_id
  dynamic "rule" {
    for_each = { daily = 7, weekly = 28 }
    content {
      id     = rule.key
      status = "Enabled"
      filter { prefix = "${rule.key}/" }
      expiration { days = rule.value }
      noncurrent_version_expiration { noncurrent_days = rule.value }
      abort_incomplete_multipart_upload { days_after_initiation = 1 }
    }
  }
}

resource "aws_cloudwatch_log_group" "postgres" {
  for_each          = toset(["postgresql", "upgrade"])
  name              = "/aws/rds/instance/istpetdev-rds-dev/${each.key}"
  retention_in_days = 14
}

resource "aws_sns_topic" "operations" { name = "istpetdev-dev-operations" }
resource "aws_sns_topic_subscription" "operations" {
  for_each  = var.alert_emails
  topic_arn = aws_sns_topic.operations.arn
  protocol  = "email"
  endpoint  = each.value
}

resource "aws_cloudwatch_metric_alarm" "rds" {
  for_each = {
    cpu         = { metric = "CPUUtilization", threshold = 70, comparison = "GreaterThanThreshold", statistic = "Average" }
    storage     = { metric = "FreeStorageSpace", threshold = 4294967296, comparison = "LessThanThreshold", statistic = "Minimum" }
    memory      = { metric = "FreeableMemory", threshold = 322122547, comparison = "LessThanThreshold", statistic = "Minimum" }
    connections = { metric = "DatabaseConnections", threshold = 100, comparison = "GreaterThanThreshold", statistic = "Maximum" }
  }
  alarm_name          = "istpetdev-dev-rds-${each.key}"
  namespace           = "AWS/RDS"
  metric_name         = each.value.metric
  statistic           = each.value.statistic
  period              = 300
  evaluation_periods  = 2
  threshold           = each.value.threshold
  comparison_operator = each.value.comparison
  dimensions          = { DBInstanceIdentifier = module.rds.db_instance_id }
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.operations.arn]
  ok_actions          = [aws_sns_topic.operations.arn]
}

resource "aws_cloudwatch_metric_alarm" "access_node" {
  alarm_name          = "istpetdev-dev-access-node-status"
  namespace           = "AWS/EC2"
  metric_name         = "StatusCheckFailed"
  statistic           = "Maximum"
  period              = 60
  evaluation_periods  = 3
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  dimensions          = { InstanceId = module.ec2_access.instance_id }
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.operations.arn]
}

resource "aws_db_event_subscription" "operations" {
  name             = "istpetdev-dev-rds-events"
  sns_topic        = aws_sns_topic.operations.arn
  source_type      = "db-instance"
  source_ids       = [module.rds.db_instance_id]
  event_categories = ["availability", "failure", "low storage", "backup", "recovery"]
}

resource "aws_budgets_budget" "hackathon" {
  for_each     = toset(["ACTUAL", "FORECASTED"])
  name         = "istpetdev-hackathon-gross-${lower(each.key)}"
  budget_type  = "COST"
  limit_amount = tostring(var.monthly_budget_usd)
  limit_unit   = "USD"
  time_unit    = "MONTHLY"
  # Account-wide gross spend also catches untaggable IP, IAM and KMS-related use.
  cost_types {
    include_credit = false
    include_refund = false
    use_blended    = false
  }
  dynamic "notification" {
    for_each = [50, 80, 100]
    content {
      comparison_operator        = "GREATER_THAN"
      threshold                  = notification.value
      threshold_type             = "PERCENTAGE"
      notification_type          = each.key
      subscriber_email_addresses = var.alert_emails
    }
  }
}

output "backup_bucket_name" { value = module.s3_backups.bucket_id }
output "operations_topic_arn" { value = aws_sns_topic.operations.arn }
