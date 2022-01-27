terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
    archive = {
      source  = "hashicorp/archive"
      version = "2.2.0"
    }
  }
}

provider "aws" {
  region = "eu-west-1"
}

data "archive_file" "LambdaFunction_deployment_package" {
  type        = "zip"
  source_file = "lambda/LambdaFunction/index.py"
  output_path = "lambda/LambdaFunction/LambdaFunction_deployment_package.zip"
}

resource "aws_cloudwatch_event_rule" "LambdaFunctionSheduledEvent" {
  event_bus_name      = "default"
  is_enabled          = true
  name                = "test-lambda-LambdaFunctionSheduledEvent-11NOUEASGZWQN"
  schedule_expression = "rate(1 hour)"
}

resource "aws_iam_role" "LambdaFunctionRole" {
  assume_role_policy = jsonencode(
    {
      Version = "2012-10-17"
      Statement = [
        {
          Effect = "Allow"
          Principal = {
            Service = "lambda.amazonaws.com"
          }
          Action = "sts:AssumeRole"
        }
      ]
    }
  )

  inline_policy {
    name = "LambdaFunctionRolePolicy0"
    policy = jsonencode(
      {
        Statement = [
          {
            Action = [
              "s3:ListBucket",
            ]
            Resource = aws_s3_bucket.S3Bucket.arn
            Effect   = "Allow"
          }
        ]
      }
    )
  }
  managed_policy_arns = [
    "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
  ]
  max_session_duration = 3600
  name                 = "test-lambda-LambdaFunctionRole-15D7T84YDQ2OV"
  path                 = "/"
  tags = {
    "lambda:createdBy" = "SAM"
  }
}

resource "aws_lambda_function" "LambdaFunction" {
  architectures = [
    "x86_64",
  ]
  description                    = "Just prints to CloudWatch log"
  filename                       = data.archive_file.LambdaFunction_deployment_package.output_path
  function_name                  = "test-lambda-LambdaFunction-FyS9wmUPMO8m"
  handler                        = "index.handler"
  memory_size                    = 128
  package_type                   = "Zip"
  reserved_concurrent_executions = -1
  role                           = aws_iam_role.LambdaFunctionRole.arn
  runtime                        = "python3.7"
  source_code_hash               = data.archive_file.LambdaFunction_deployment_package.output_base64sha256
  tags = {
    "lambda:createdBy" = "SAM"
  }
  timeout = 3

  tracing_config {
    mode = "PassThrough"
  }
}

resource "aws_lambda_permission" "LambdaFunctionSheduledEventPermission" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.LambdaFunction.arn
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.LambdaFunctionSheduledEvent.arn
  statement_id  = "test-lambda-LambdaFunctionSheduledEventPermission-2FANL87ZUE68"
}

resource "aws_s3_bucket" "S3Bucket" {
  arn            = "arn:aws:s3:::test-lambda-s3bucket-tq166twr9fly"
  bucket         = "test-lambda-s3bucket-tq166twr9fly"
  hosted_zone_id = "Z1BKCTXD74EZPE"
  request_payer  = "BucketOwner"
}

