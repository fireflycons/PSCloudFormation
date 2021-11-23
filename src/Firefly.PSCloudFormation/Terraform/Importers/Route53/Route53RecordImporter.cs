namespace Firefly.PSCloudFormation.Terraform.Importers.Route53
{
    using System.Linq;

    using Firefly.CloudFormationParser.Intrinsics;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/route53_record#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class Route53RecordImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Route53RecordImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public Route53RecordImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => string.Empty;

        /// <inheritdoc />
        protected override string ReferencingPropertyPath { get; }

        /// <inheritdoc />
        public override string GetImportId()
        {
            var templateResource = this.TerraformSettings.Resources.First(r => r.LogicalResourceId == this.ImportSettings.Resource.LogicalId)
                .TemplateResource;

            // ReSharper disable once PossibleNullReferenceException - can't be null for this resource type.
            var typeProperty = templateResource.Properties["Type"];
            var zoneIdProperty = templateResource.Properties.ContainsKey("HostedZoneId")
                                     ? templateResource.Properties["HostedZoneId"]
                                     : null;

            if (zoneIdProperty == null)
            {
                this.LogError($"Cannot determine Zone ID for resource \"{this.ImportSettings.Resource.LogicalId}\"");
                return null;
            }

            string type;
            string zoneId;

            switch (typeProperty)
            {
                case IIntrinsic intrinsic:

                    type =  intrinsic.Evaluate(this.TerraformSettings.Template).ToString();
                    break;

                case string s:

                    type = s;
                    break;

                default:

                    this.LogError($"Invalid type {typeProperty.GetType().Name} for R53 recordset \"Type\" property.");
                    return null;
            }

            switch (zoneIdProperty)
            {
                case IIntrinsic intrinsic:

                    zoneId = intrinsic.Evaluate(this.TerraformSettings.Template).ToString();
                    break;

                case string s:

                    zoneId = s;
                    break;

                default:

                    this.LogError($"Invalid type {typeProperty.GetType().Name} for R53 recordset \"ZoneId\" property.");
                    return null;
            }

            // ZONEID_RECORDNAME_TYPE
            return $"{zoneId}_{this.ImportSettings.Resource.PhysicalId}_{type}";
        }
    }
}
