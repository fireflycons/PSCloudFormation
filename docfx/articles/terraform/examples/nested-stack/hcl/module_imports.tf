module "cfn-workshop-nested-stack-EC2Stack-KGXCZCF8MN3C" {
  source                   = "./modules/cfn-workshop-nested-stack-EC2Stack-KGXCZCF8MN3C"
  EnvironmentType          = var.EnvironmentType
  VpcId                    = module.cfn-workshop-nested-stack-VpcStack-CEYRYZJTGP7X.VpcId
  SubnetId                 = module.cfn-workshop-nested-stack-VpcStack-CEYRYZJTGP7X.PublicSubnet1
  WebServerInstanceProfile = module.cfn-workshop-nested-stack-IamStack-R7URHPP7NDAS.WebServerInstanceProfile
}

module "cfn-workshop-nested-stack-IamStack-R7URHPP7NDAS" {
  source = "./modules/cfn-workshop-nested-stack-IamStack-R7URHPP7NDAS"
}

module "cfn-workshop-nested-stack-VpcStack-CEYRYZJTGP7X" {
  source            = "./modules/cfn-workshop-nested-stack-VpcStack-CEYRYZJTGP7X"
  AvailabilityZones = var.AvailabilityZones
  VPCName           = var.VPCName
  VPCCidr           = var.VPCCidr
  PublicSubnet1Cidr = var.PublicSubnet1Cidr
  PublicSubnet2Cidr = var.PublicSubnet2Cidr
}

