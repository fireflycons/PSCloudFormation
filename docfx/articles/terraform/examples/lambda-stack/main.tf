terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
    zipper = {
      source = "ArthurHlt/zipper"
    }
  }
}

provider "aws" {
  region = "eu-west-1"
}

data "aws_availability_zones" "available" {
  state = "available"
}

resource "zipper_file" "LambdaFunction_deployment_package" {
  source      = "lambda/LambdaFunction/index.py"
  output_path = "lambda/LambdaFunction/LambdaFunction_deployment_package.zip"
}

resource "aws_cloudwatch_event_rule" "LambdaFunctionSheduledEvent" {
  event_bus_name      = "default"
  is_enabled          = true
  name                = "test-lambda-LambdaFunctionSheduledEvent-QLL75RMPW5EC"
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
  max_session_duration = 3600
  name                 = "test-lambda-LambdaFunctionRole-13BZVJKAQAW76"
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
  filename                       = zipper_file.LambdaFunction_deployment_package.output_path
  function_name                  = "test-lambda-LambdaFunction-sRfgz1lPwRH2"
  handler                        = "index.handler"
  memory_size                    = 128
  package_type                   = "Zip"
  reserved_concurrent_executions = -1
  role                           = aws_iam_role.LambdaFunctionRole.arn
  runtime                        = "python3.7"
  source_code_hash               = zipper_file.LambdaFunction_deployment_package.output_sha
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
}

resource "aws_s3_bucket" "S3Bucket" {
  bucket = "test-lambda-s3bucket-1qfp4kvowpamf"
}

