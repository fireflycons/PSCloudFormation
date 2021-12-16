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

variable "VPCName" {
  type        = string
  description = "The name of the VPC being created."
  default     = "VPC Public and Private with NAT"
}

data "aws_region" "current" {}

data "aws_availability_zones" "available" {
  state = "available"
}

locals {
  mappings = {
    SubnetConfig = {
      VPC = {
        CIDR = "10.0.0.0/16"
      }
    }
    AZRegions = {
      "ap-northeast-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "ap-northeast-2" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "ap-south-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "ap-southeast-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "ap-southeast-2" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "ca-central-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "eu-central-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "eu-west-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "eu-west-2" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "sa-east-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "us-east-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "us-east-2" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "us-west-1" = {
        AZs = [
          "a",
          "b",
        ]
      }
      "us-west-2" = {
        AZs = [
          "a",
          "b",
        ]
      }
    }
  }
}

resource "aws_eip" "ElasticIP0" {
  vpc = true
}

resource "aws_eip" "ElasticIP1" {
  vpc = true
}

resource "aws_internet_gateway" "InternetGateway" {
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-IGW"])
    Network     = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_nat_gateway" "NATGateway0" {
  allocation_id     = aws_eip.ElasticIP0.id
  connectivity_type = "public"
  subnet_id         = aws_subnet.PublicSubnet0.id
}

resource "aws_nat_gateway" "NATGateway1" {
  allocation_id     = aws_eip.ElasticIP1.id
  connectivity_type = "public"
  subnet_id         = aws_subnet.PublicSubnet1.id
}

resource "aws_network_acl" "PublicNetworkAcl" {
  egress = [
    {
      action          = "allow"
      cidr_block      = "0.0.0.0/0"
      from_port       = 0
      icmp_code       = 0
      icmp_type       = 0
      ipv6_cidr_block = ""
      protocol        = "-1"
      rule_no         = 100
      to_port         = 0
    }
  ]
  ingress = [
    {
      action          = "allow"
      cidr_block      = "0.0.0.0/0"
      from_port       = 0
      icmp_code       = 0
      icmp_type       = 0
      ipv6_cidr_block = ""
      protocol        = "-1"
      rule_no         = 100
      to_port         = 0
    }
  ]
  subnet_ids = [
    aws_subnet.PublicSubnet1.id,
    aws_subnet.PublicSubnet0.id,
  ]
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-public-nacl"])
    Network     = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route" "PrivateRouteToInternet0" {
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = aws_nat_gateway.NATGateway0.id
  route_table_id         = aws_route_table.PrivateRouteTable0.id
}

resource "aws_route" "PrivateRouteToInternet1" {
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = aws_nat_gateway.NATGateway1.id
  route_table_id         = aws_route_table.PrivateRouteTable1.id
}

resource "aws_route" "PublicRoute" {
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.InternetGateway.id
  route_table_id         = aws_route_table.PublicRouteTable.id
}

resource "aws_route_table" "PrivateRouteTable0" {
  tags = {
    Name = join("", [var.VPCName, "-private-route-table-0"])
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route_table" "PrivateRouteTable1" {
  tags = {
    Name = join("", [var.VPCName, "-private-route-table-1"])
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route_table" "PublicRouteTable" {
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-public-route-table"])
    Network     = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route_table_association" "PrivateSubnetRouteTableAssociation0" {
  route_table_id = aws_route_table.PrivateRouteTable0.id
  subnet_id      = aws_subnet.PrivateSubnet0.id
}

resource "aws_route_table_association" "PrivateSubnetRouteTableAssociation1" {
  route_table_id = aws_route_table.PrivateRouteTable1.id
  subnet_id      = aws_subnet.PrivateSubnet1.id
}

resource "aws_route_table_association" "PublicSubnetRouteTableAssociation0" {
  route_table_id = aws_route_table.PublicRouteTable.id
  subnet_id      = aws_subnet.PublicSubnet0.id
}

resource "aws_route_table_association" "PublicSubnetRouteTableAssociation1" {
  route_table_id = aws_route_table.PublicRouteTable.id
  subnet_id      = aws_subnet.PublicSubnet1.id
}

resource "aws_subnet" "PrivateSubnet0" {
  availability_zone = data.aws_availability_zones.available.names[0]
  cidr_block        = cidrsubnets(local.mappings.SubnetConfig.VPC.CIDR, 8, 8, 8, 8)[2]
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-private-", local.mappings.AZRegions[data.aws_region.current.name].AZs[0]])
    Network     = "Private"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_subnet" "PrivateSubnet1" {
  availability_zone = data.aws_availability_zones.available.names[1]
  cidr_block        = cidrsubnets(local.mappings.SubnetConfig.VPC.CIDR, 8, 8, 8, 8)[3]
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-private-", local.mappings.AZRegions[data.aws_region.current.name].AZs[1]])
    Network     = "Private"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_subnet" "PublicSubnet0" {
  availability_zone       = "${data.aws_region.current.name}${local.mappings.AZRegions[data.aws_region.current.name].AZs[0]}"
  cidr_block              = cidrsubnets(local.mappings.SubnetConfig.VPC.CIDR, 8, 8, 8, 8)[0]
  map_public_ip_on_launch = true
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-public-", local.mappings.AZRegions[data.aws_region.current.name].AZs[0]])
    Network     = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_subnet" "PublicSubnet1" {
  availability_zone       = "${data.aws_region.current.name}${local.mappings.AZRegions[data.aws_region.current.name].AZs[1]}"
  cidr_block              = cidrsubnets(local.mappings.SubnetConfig.VPC.CIDR, 8, 8, 8, 8)[1]
  map_public_ip_on_launch = true
  tags = {
    Application = var.VPCName
    Name        = join("", [var.VPCName, "-public-", local.mappings.AZRegions[data.aws_region.current.name].AZs[1]])
    Network     = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_vpc" "VPC" {
  cidr_block           = local.mappings.SubnetConfig.VPC.CIDR
  enable_dns_hostnames = true
  enable_dns_support   = true
  instance_tenancy     = "default"
  tags = {
    Application = var.VPCName
    Name        = var.VPCName
    Network     = "Public"
  }
}

output "VPCId" {
  value       = aws_vpc.VPC.id
  description = "VPCId of VPC"
}

output "PublicSubnet0" {
  value       = aws_subnet.PublicSubnet0.id
  description = "SubnetId of public subnet 0"
}

output "PublicSubnet1" {
  value       = aws_subnet.PublicSubnet1.id
  description = "SubnetId of public subnet 1"
}

output "PrivateSubnet0" {
  value       = aws_subnet.PrivateSubnet0.id
  description = "SubnetId of private subnet 0"
}

output "PrivateSubnet1" {
  value       = aws_subnet.PrivateSubnet1.id
  description = "SubnetId of private subnet 1"
}

