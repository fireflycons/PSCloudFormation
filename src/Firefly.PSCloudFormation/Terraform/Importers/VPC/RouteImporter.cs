namespace Firefly.PSCloudFormation.Terraform.Importers.VPC
{
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;

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

        public override string GetImportId(string caption, string message)
        {
            var templateResource = this.TerraformSettings.Resources
                .First(r => r.LogicalResourceId == this.ImportSettings.Resource.LogicalId).TemplateResource;

            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId && e.Source.TemplateObject is IResource
                                                                             && e.Tag != null
                                                                             && e.Tag.ReferenceType
                                                                             == ReferenceType.DirectReference).Where(
                d => ((IResource)d.Source.TemplateObject).Type == "AWS::EC2::RouteTable").ToList();

            // There should be a 1:1 relationship between attachment and pool.
            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected Route Table \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport.First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name)
                    .PhysicalId;

                var v4Cidr = templateResource.GetResourcePropertyValue("DestinationCidrBlock");
                var v6Cidr = templateResource.GetResourcePropertyValue("DestinationIpv6CidrBlock");

                if (v4Cidr is string)
                {
                    return $"{referencedId}_{v4Cidr}";
                }

                if (v6Cidr is string)
                {
                    return $"{referencedId}_{v6Cidr}";
                }

                this.LogError($"Cannot resolve destination for {this.ImportSettings.Resource.LogicalId}.");

                return null;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related Route Table for {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple Route Tables relating to {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }
    }
}