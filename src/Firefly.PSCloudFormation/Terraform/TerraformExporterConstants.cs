namespace Firefly.PSCloudFormation.Terraform
{
    /// <summary>
    ///  String constants that are used more than once.
    /// </summary>
    internal static class TerraformExporterConstants
    {
        public const string AwsCloudFormationStack = "AWS::CloudFormation::Stack";

        public const string AwsLambdaFunction = "AWS::Lambda::Function";

        public const string LambdaZipFile = "Code.ZipFile";

        public const string StackOutputAttributeIndentifier = "Outputs.";
    }
}