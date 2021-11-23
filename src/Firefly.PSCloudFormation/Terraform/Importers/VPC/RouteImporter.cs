namespace Firefly.PSCloudFormation.Terraform.Importers.VPC
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/route#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class RouteImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public RouteImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::EC2::RouteTable";

        /// <inheritdoc />
        protected override string ReferencingPropertyPath => null;

        /// <inheritdoc />
        public override string GetImportId()
        {
            var dependency = this.GetResourceDependency();

            if (dependency == null)
            {
                return null;
            }

            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    var referencedId = dependency.Resource.PhysicalId;

                    var ipv4Cidr = dependency.ReferringTemplateObject.GetResourcePropertyValue("DestinationCidrBlock");
                    var ipv6Cidr = dependency.ReferringTemplateObject.GetResourcePropertyValue("DestinationIpv6CidrBlock");

                    if (ipv4Cidr is string)
                    {
                        return $"{referencedId}_{ipv4Cidr}";
                    }

                    if (ipv6Cidr is string)
                    {
                        return $"{referencedId}_{ipv6Cidr}";
                    }

                    this.LogError($"Cannot resolve destination for {this.ImportSettings.Resource.LogicalId}.");
                    return $"{dependency.Resource.PhysicalId}/{this.ImportSettings.Resource.PhysicalId}";

                default:

                    return null;
            }
        }
    }
}