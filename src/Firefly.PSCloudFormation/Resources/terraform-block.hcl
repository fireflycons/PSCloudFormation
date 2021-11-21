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
      "tf:StackName" = "AWS::StackName" # Use this to create a resource group
    }
  }
}

