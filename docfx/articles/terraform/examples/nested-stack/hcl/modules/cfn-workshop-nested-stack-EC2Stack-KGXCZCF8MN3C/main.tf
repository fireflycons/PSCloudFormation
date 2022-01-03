variable "EnvironmentType" {
  type        = string
  description = "Specify the Environment type of the stack."
  default     = "Test"
  validation {
    condition     = contains(["Dev", "Test", "Prod"], var.EnvironmentType)
    error_message = "Specify either Dev, Test or Prod."
  }
}

variable "SubnetId" {
  type        = string
  description = "The Subnet ID"
}

variable "VpcId" {
  type        = string
  description = "The VPC ID"
}

variable "WebServerInstanceProfile" {
  type        = string
  description = "Instance profile resource ID"
}

data "aws_ssm_parameter" "AmiID" {
  name = "/aws/service/ami-amazon-linux-latest/amzn2-ami-hvm-x86_64-gp2"
}

data "aws_region" "current" {}

locals {
  mappings = {
    EnvironmentToInstanceType = {
      Dev = {
        InstanceType = "t2.nano"
      }
      Test = {
        InstanceType = "t2.micro"
      }
      Prod = {
        InstanceType = "t2.small"
      }
    }
  }
}

resource "aws_eip" "WebServerEIP" {
  vpc = true
}

resource "aws_instance" "WebServerInstance" {
  ami = data.aws_ssm_parameter.AmiID.value
  associate_public_ip_address = true
  availability_zone = "eu-west-1a"
  capacity_reservation_specification {
    capacity_reservation_preference = "open"
  }
  cpu_core_count = 1
  cpu_threads_per_core = 1
  credit_specification {
    cpu_credits = "standard"
  }
  iam_instance_profile = var.WebServerInstanceProfile
  instance_initiated_shutdown_behavior = "stop"
  instance_type = local.mappings.EnvironmentToInstanceType[var.EnvironmentType].InstanceType
  ipv6_address_count = 0
  metadata_options {
    http_endpoint = "enabled"
    http_put_response_hop_limit = 1
    http_tokens = "optional"
  }
  private_ip = "10.0.0.9"
  root_block_device {
    delete_on_termination = true
    iops = 100
    throughput = 0
    volume_size = 8
    volume_type = "gp2"
  }
  source_dest_check = true
  subnet_id = var.SubnetId
  tags = {
    Name = join(" ", [var.EnvironmentType, "Web Server"])
  }
  tenancy = "default"
  user_data = "fd240ac505309c360967c62040d70922b2d16b06"
  vpc_security_group_ids = [
    aws_security_group.WebServerSecurityGroup.id,
  ]
}

resource "aws_security_group" "WebServerSecurityGroup" {
  description = "Enable HTTP and HTTPS access"
  egress = [
    {
      cidr_blocks = [
        "0.0.0.0/0",
      ]
      description = ""
      from_port = 443
      ipv6_cidr_blocks = []
      prefix_list_ids = []
      protocol = "tcp"
      security_groups = []
      self = false
      to_port = 443
    },
    {
      cidr_blocks = [
        "0.0.0.0/0",
      ]
      description = ""
      from_port = 80
      ipv6_cidr_blocks = []
      prefix_list_ids = []
      protocol = "tcp"
      security_groups = []
      self = false
      to_port = 80
    }
  ]
  ingress = [
    {
      cidr_blocks = [
        "0.0.0.0/0",
      ]
      description = ""
      from_port = 80
      ipv6_cidr_blocks = []
      prefix_list_ids = []
      protocol = "tcp"
      security_groups = []
      self = false
      to_port = 80
    }
  ]
  name = "cfn-workshop-nested-stack-EC2Stack-KGXCZCF8MN3C-WebServerSecurityGroup-DUEAOQJ6QX4H"
  vpc_id = var.VpcId
}

output "WebServerElasticIP" {
  value = aws_eip.WebServerEIP.id
  description = "Elastic IP associated with the web server EC2 instance"
}

