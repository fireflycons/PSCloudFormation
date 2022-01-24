namespace Firefly.PSCloudFormation.Terraform.Importers.VPC
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/network_acl_rule#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class NetworkAclEntryImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkAclEntryImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public NetworkAclEntryImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::EC2::NetworkAcl";

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

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (dependency.DependencyType)
            {
                case DependencyType.Resource:

                    var ruleNumber = dependency.ReferringTemplateObject.GetResourcePropertyValue("RuleNumber");
                    var protocol = dependency.ReferringTemplateObject.GetResourcePropertyValue("Protocol");
                    var egress = dependency.ReferringTemplateObject.GetResourcePropertyValue("Egress")?.ToString()
                        .ToLowerInvariant();

                    return $"{dependency.Resource.PhysicalId}:{ruleNumber}:{protocol}:{egress}";

                default:

                    return null;
            }
        }
    }
}