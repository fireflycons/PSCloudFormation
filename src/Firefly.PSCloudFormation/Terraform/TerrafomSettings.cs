namespace Firefly.PSCloudFormation.Terraform
{
    internal class TerrafomSettings : ITerraformSettings
    {
        public string AwsRegion { get; set; }

        public ITerraformRunner Runner { get; set; }

        public string WorkspaceDirectory { get; set; }
    }
}