namespace Firefly.PSCloudFormation.Terraform.Importers.ElasticLoadBalancingV2
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/lb_listener_certificate#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class ListenerCertificateImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListenerCertificateImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public ListenerCertificateImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::ElasticLoadBalancingV2::Listener";

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

                    // TODO: return multiple tf resources as can be more than one cert here.
                    var certificate = this.AwsResource.GetResourcePropertyValue("Certificates.0.CertificateArn");
                    return $"{dependency.Resource.PhysicalId}_{certificate}";


                default:

                    return null;
            }
        }
    }
}