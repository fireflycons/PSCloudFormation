variable "AvailabilityZones" {
  type        = list(string)
  description = "The list of Availability Zones to use for the subnets in the VPC."
  default     = [
    "",
  ]
}

variable "PublicSubnet1Cidr" {
  type        = string
  description = "The CIDR block for the public subnet located in Availability Zone 1."
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.PublicSubnet1Cidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "PublicSubnet2Cidr" {
  type        = string
  description = "The CIDR block for the public subnet located in Availability Zone 2."
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.PublicSubnet2Cidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "VPCCidr" {
  type        = string
  description = "The CIDR block for the VPC."
  validation {
    condition     = can(regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\\/([0-9]|[1-2][0-9]|3[0-2]))$", var.VPCCidr))
    error_message = "CIDR block parameter must be in the form x.x.x.x/16-28."
  }
}

variable "VPCName" {
  type        = string
  description = "The name of the VPC."
}

resource "aws_internet_gateway" "VPCIGW" {
  tags = {
    Name = "VPCIGW"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route" "VPCPublicSubnet1DefaultRoute" {
  destination_cidr_block = "0.0.0.0/0"
  gateway_id = aws_internet_gateway.VPCIGW.id
  route_table_id = aws_route_table.VPCPublicSubnet1RouteTable.id
}

resource "aws_route" "VPCPublicSubnet2DefaultRoute" {
  destination_cidr_block = "0.0.0.0/0"
  gateway_id = aws_internet_gateway.VPCIGW.id
  route_table_id = aws_route_table.VPCPublicSubnet2RouteTable.id
}

resource "aws_route_table" "VPCPublicSubnet1RouteTable" {
  tags = {
    Name = "PublicSubnet1"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route_table" "VPCPublicSubnet2RouteTable" {
  tags = {
    Name = "PublicSubnet2"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_route_table_association" "VPCPublicSubnet1RouteTableAssociation" {
  route_table_id = aws_route_table.VPCPublicSubnet1RouteTable.id
  subnet_id = aws_subnet.VPCPublicSubnet1.id
}

resource "aws_route_table_association" "VPCPublicSubnet2RouteTableAssociation" {
  route_table_id = aws_route_table.VPCPublicSubnet2RouteTable.id
  subnet_id = aws_subnet.VPCPublicSubnet2.id
}

resource "aws_subnet" "VPCPublicSubnet1" {
  availability_zone = var.AvailabilityZones[0]
  cidr_block = var.PublicSubnet1Cidr
  map_public_ip_on_launch = true
  tags = {
    Name = "PublicSubnet1"
    "subnet-type" = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_subnet" "VPCPublicSubnet2" {
  availability_zone = var.AvailabilityZones[1]
  cidr_block = var.PublicSubnet2Cidr
  map_public_ip_on_launch = true
  tags = {
    Name = "PublicSubnet2"
    "subnet-type" = "Public"
  }
  vpc_id = aws_vpc.VPC.id
}

resource "aws_vpc" "VPC" {
  cidr_block = var.VPCCidr
  enable_dns_hostnames = true
  enable_dns_support = true
  instance_tenancy = "default"
  tags = {
    Name = var.VPCName
  }
}

output "VpcId" {
  value = aws_vpc.VPC.id
}

output "PublicSubnet1" {
  value = aws_subnet.VPCPublicSubnet1.id
}

output "PublicSubnet2" {
  value = aws_subnet.VPCPublicSubnet2.id
}

