namespace Firefly.PSCloudFormation.Terraform.Importers.ApplicationAutoScaling
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/appautoscaling_policy#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    // ReSharper disable once InconsistentNaming
    internal class AASAutoScalingPolicyImporter : AbstractAASImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AASAutoScalingPolicyImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public AASAutoScalingPolicyImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::ApplicationAutoScaling::ScalableTarget";

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

                    var aasTarget = this.GetAASTarget(dependency.Resource);
                    return $"{aasTarget}/{this.ImportSettings.Resource.LogicalId}";

                default:

                    return null;
            }
        }
    }
}