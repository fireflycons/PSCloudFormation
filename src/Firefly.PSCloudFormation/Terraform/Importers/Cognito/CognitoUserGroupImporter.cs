namespace Firefly.PSCloudFormation.Terraform.Importers.Cognito
{
    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/cognito_user_group#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class CognitoUserGroupImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CognitoUserGroupImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public CognitoUserGroupImporter(IResourceImporterSettings importSettings, ITerraformExportSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => "AWS::Cognito::UserPool";

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

                    return $"{dependency.Resource.PhysicalId}/{this.ImportSettings.Resource.PhysicalId}";

                default:

                    return null;
            }
        }
    }
}