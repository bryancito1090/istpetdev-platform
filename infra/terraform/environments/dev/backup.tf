# Optional alternative for the account Free plan. Never represents seven days of PITR.
resource "aws_backup_vault" "dev" {
  count         = var.free_plan_backup_workaround ? 1 : 0
  name          = "istpetdev-dev"
  force_destroy = false
}

resource "aws_iam_role" "backup" {
  count = var.free_plan_backup_workaround ? 1 : 0
  name  = "istpetdev-dev-backup"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow", Action = "sts:AssumeRole", Principal = { Service = "backup.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy" "backup" {
  count = var.free_plan_backup_workaround ? 1 : 0
  name  = "IstpetDevRDSSnapshots"
  role  = aws_iam_role.backup[0].id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      { Effect = "Allow", Action = ["rds:DescribeDBInstances", "rds:DescribeDBSnapshots"], Resource = "*",
      Condition = { StringEquals = { "aws:RequestedRegion" = var.aws_region } } },
      { Effect = "Allow", Action = ["rds:CreateDBSnapshot", "rds:CopyDBSnapshot", "rds:AddTagsToResource", "rds:ListTagsForResource"],
      Resource = [module.rds.db_instance_arn, "arn:aws:rds:${var.aws_region}:${data.aws_caller_identity.current.account_id}:snapshot:awsbackup:*", "arn:aws:rds:${var.aws_region}:${data.aws_caller_identity.current.account_id}:snapshot:rds:istpetdev-rds-dev-*"] },
      { Effect   = "Allow", Action = ["rds:DeleteDBSnapshot", "rds:ModifyDBSnapshotAttribute"],
        Resource = "arn:aws:rds:${var.aws_region}:${data.aws_caller_identity.current.account_id}:snapshot:awsbackup:*",
      Condition = { StringEquals = { "aws:ResourceTag/Project" = "IstpetDev" } } },
      { Effect = "Allow", Action = ["kms:DescribeKey", "kms:Decrypt", "kms:GenerateDataKey", "kms:CreateGrant"], Resource = module.rds.kms_key_arn,
      Condition = { StringEquals = { "kms:ViaService" = "rds.${var.aws_region}.amazonaws.com" } } }
    ]
  })
}

resource "aws_backup_plan" "dev" {
  count = var.free_plan_backup_workaround ? 1 : 0
  name  = "istpetdev-dev-seven-daily-snapshots"
  rule {
    rule_name                = "daily-seven-days"
    target_vault_name        = aws_backup_vault.dev[0].name
    schedule                 = "cron(0 8 * * ? *)"
    start_window             = 60
    completion_window        = 180
    enable_continuous_backup = false
    lifecycle { delete_after = 7 }
    recovery_point_tags = local.common_tags
  }
}

resource "aws_backup_selection" "dev" {
  count        = var.free_plan_backup_workaround ? 1 : 0
  name         = "istpetdev-rds-dev-only"
  plan_id      = aws_backup_plan.dev[0].id
  iam_role_arn = aws_iam_role.backup[0].arn
  resources    = [module.rds.db_instance_arn]
  depends_on   = [aws_iam_role_policy.backup]
}

resource "aws_cloudwatch_event_rule" "backup_failure" {
  count = var.free_plan_backup_workaround ? 1 : 0
  name  = "istpetdev-dev-backup-failure"
  event_pattern = jsonencode({
    source = ["aws.backup"], "detail-type" = ["Backup Job State Change"]
    detail = { state = ["FAILED", "ABORTED", "EXPIRED"], backupVaultName = [aws_backup_vault.dev[0].name] }
  })
}

resource "aws_cloudwatch_event_target" "backup_failure" {
  count = var.free_plan_backup_workaround ? 1 : 0
  rule  = aws_cloudwatch_event_rule.backup_failure[0].name
  arn   = aws_sns_topic.operations.arn
}

resource "aws_sns_topic_policy" "operations" {
  arn = aws_sns_topic.operations.arn
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = concat([
      { Sid = "Owner", Effect = "Allow", Principal = { AWS = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root" }, Action = ["SNS:GetTopicAttributes", "SNS:SetTopicAttributes", "SNS:AddPermission", "SNS:RemovePermission", "SNS:DeleteTopic", "SNS:Subscribe", "SNS:ListSubscriptionsByTopic", "SNS:Publish", "SNS:Receive"], Resource = aws_sns_topic.operations.arn },
      { Sid = "OperationalServices", Effect = "Allow", Principal = { Service = ["cloudwatch.amazonaws.com", "rds.amazonaws.com"] }, Action = "SNS:Publish", Resource = aws_sns_topic.operations.arn,
      Condition = { StringEquals = { "aws:SourceAccount" = data.aws_caller_identity.current.account_id } } }
      ], var.free_plan_backup_workaround ? [
      { Sid = "BackupFailure", Effect = "Allow", Principal = { Service = "events.amazonaws.com" }, Action = "SNS:Publish", Resource = aws_sns_topic.operations.arn,
      Condition = { ArnEquals = { "aws:SourceArn" = aws_cloudwatch_event_rule.backup_failure[0].arn } } }
    ] : [])
  })
}

output "backup_vault_name" { value = try(aws_backup_vault.dev[0].name, null) }
output "backup_role_arn" { value = try(aws_iam_role.backup[0].arn, null) }
