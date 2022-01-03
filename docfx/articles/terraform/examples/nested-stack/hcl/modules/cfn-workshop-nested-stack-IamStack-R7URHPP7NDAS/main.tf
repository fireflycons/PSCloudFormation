resource "aws_iam_instance_profile" "WebServerInstanceProfile" {
  name = "cfn-workshop-nested-stack-IamStack-R7URHPP7NDAS-WebServerInstanceProfile-3I4ZIXVTDCDC"
  path = "/"
  role = aws_iam_role.SSMIAMRole.id
}

resource "aws_iam_role" "SSMIAMRole" {
  assume_role_policy = jsonencode(
    {
      Version = "2008-10-17"
      Statement = [
        {
          Effect = "Allow"
          Principal = {
            Service = "ec2.amazonaws.com"
          }
          Action = "sts:AssumeRole"
        }
      ]
    }
  )
  max_session_duration = 3600
  path = "/"
}

output "WebServerInstanceProfile" {
  value = aws_iam_instance_profile.WebServerInstanceProfile.id
}

