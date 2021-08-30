namespace Firefly.PSCloudFormation.Terraform
{
    using System.Diagnostics;

    [DebuggerDisplay("{Address}: {PhysicalId}")]
    internal class ResourceImport
    {
        public string Address { get; set; }

        public string AwsAddress { get; set; }

        public string PhysicalId { get; set; }
    }
}