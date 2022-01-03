namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Constants and declarations used in several locations
    /// </summary>
    internal static class TerraformExporterConstants
    {
        /// <summary>
        /// AWS type name of nested stack
        /// </summary>
        public const string AwsCloudFormationStack = "AWS::CloudFormation::Stack";

        /// <summary>
        /// AWS type name for lambda
        /// </summary>
        public const string AwsLambdaFunction = "AWS::Lambda::Function";

        /// <summary>
        /// Property on a lambda where template-embedded code is found
        /// </summary>
        public const string LambdaZipFile = "Code.ZipFile";

        /// <summary>
        /// Qualifier on a <c>!GetAtt</c> for a nested stack output reference.
        /// </summary>
        public const string StackOutputQualifier = "Outputs.";

        /// <summary>
        /// Name of the main script file
        /// </summary>
        public const string MainScriptFile = "main.tf";

        /// <summary>
        /// Name of the file declaring imported modules
        /// </summary>
        public const string ModulesFile = "module_imports.tf";

        /// <summary>
        /// Name of the variable values file
        /// </summary>
        public const string VarsFile = "terraform.tfvars";

        /// <summary>
        /// These resources have no direct terraform representation.
        /// They are merged into the resources that depend on them
        /// when the dependent resource is imported.
        /// </summary>
        public static readonly List<string> MergedResources = new List<string>
                                                                  {
                                                                      "AWS::CloudFront::CloudFrontOriginAccessIdentity",
                                                                      "AWS::IAM::Policy",
                                                                      "AWS::EC2::SecurityGroupIngress",
                                                                      "AWS::EC2::SecurityGroupEgress",
                                                                      "AWS::EC2::VPCGatewayAttachment",
                                                                      "AWS::EC2::SubnetNetworkAclAssociation",
                                                                      "AWS::EC2::NetworkAclEntry"
                                                                  };

        /// <summary>
        /// These resources are currently not supported for import.
        /// </summary>
        public static readonly List<string> UnsupportedResources = new List<string> { "AWS::ApiGateway::Deployment" };

        /// <summary>
        /// Combination of merged and unsupported resources.
        /// </summary>
        public static readonly List<string> IgnoredResources = MergedResources.Concat(UnsupportedResources).ToList();
    }
}