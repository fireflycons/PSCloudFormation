namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class ResourceDependency
    {
        public string TargetAttribute { get; set; }

        public bool IsArrayMember { get; set; }
    }
}