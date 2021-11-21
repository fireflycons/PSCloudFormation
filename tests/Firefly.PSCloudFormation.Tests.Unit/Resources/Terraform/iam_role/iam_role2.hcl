resource "aws_iam_role" "FlowLogRole" {
  assume_role_policy = jsonencode(
    {
      Version = "2012-10-17"
      Statement = [
        {
          Effect = "Allow"
          Principal = {
            Service = "vpc-flow-logs.amazonaws.com"
          }
          Action = "sts:AssumeRole"
        }
      ]
    }
  )
  inline_policy {
    name = "FlowLogRolePolicy"
    policy = jsonencode(
      {
        Version = "2012-10-17"
        Statement = [
          {
            Action = [
              "logs:CreateLogGroup",
              "logs:CreateLogStream",
              "logs:PutLogEvents",
              "logs:DescribeLogGroups",
              "logs:DescribeLogStreams",
            ]
            Resource = "*"
            Effect = "Allow"
          }
        ]
      }
    )
  }
  max_session_duration = 3600
  name = "fc-basestack-Vpc-AGGQ9RLPLCGZ-FlowLogRole-1RLSS63H1V2UL"
  path = "/"
}
