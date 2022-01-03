terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }
}

provider "aws" {
  region = "eu-west-1"
}

variable "AvailabilityZones" {
  type        = list(string)
  description = "The list of Availability Zones to use for the subnets in the VPC. Select 2 AZs."
  default = [
    "",
  ]
}

variable "EnvironmentType" {
  type        = string
  description = "Specify the Environment type of the stack."
  default     = "Test"
  validation {
    condition     = contains(["Dev", "Test", "Prod"], var.EnvironmentType)
    error_message = "Specify either Dev, Test or Prod."
  }
}

variable "PublicSubnet1Cidr" {
  type        = string
  description = "The CIDR block for the public subnet located in Availability Zone 1."
  default     = "10.0.0.0/24"
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.PublicSubnet1Cidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "PublicSubnet2Cidr" {
  type        = string
  description = "The CIDR block for the public subnet located in Availability Zone 2."
  default     = "10.0.1.0/24"
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.PublicSubnet2Cidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "VPCCidr" {
  type        = string
  description = "The CIDR block for the VPC."
  default     = "10.0.0.0/16"
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.VPCCidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "VPCName" {
  type        = string
  description = "The name of the VPC."
  default     = "cfn-workshop-vpc"
}

