terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }
}

provider "aws" {
 region = "AWS::Region"
  default_tags {
    tags = {
      "terraform:stack_name" = "AWS::StackName" # Use this to create a resource group
    }
  }
}

