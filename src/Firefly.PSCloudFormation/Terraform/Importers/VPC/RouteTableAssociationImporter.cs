namespace Firefly.PSCloudFormation.Terraform.Importers.VPC
{
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;

    /// <summary>
    /// <see href="https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/route_table_association#import" />
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class RouteTableAssociationImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTableAssociationImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        public RouteTableAssociationImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
            : base(importSettings, terraformSettings)
        {
        }

        /// <inheritdoc />
        protected override string ReferencedAwsResource => string.Empty;

        /// <inhericdoc />
        public override string GetImportId(string caption, string message)
        {
            var rtbId = this.GetRouteTable();

            if (rtbId == null)
            {
                return null;
            }

            var subnetId = this.GetSubnet();

            if (subnetId != null)
            {
                return $"{subnetId}/{rtbId}";
            }

            var igwId = this.GetInternetGateway();

            if (igwId != null)
            {
                return $"{igwId}/{rtbId}";
            }

            this.LogError($"Unable to find any association for route table {rtbId}");

            return null;
        }

        private string GetInternetGateway()
        {
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                     && e.Source.TemplateObject is IResource && e.Tag != null
                     && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                d => ((IResource)d.Source.TemplateObject).Type == "AWS::EC2::InternetGateway").ToList();

            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected IGW \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                return referencedId;
            }

            return null;
        }

        private string GetRouteTable()
        {
            // Route table must exist
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                     && e.Source.TemplateObject is IResource && e.Tag != null
                     && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                d => ((IResource)d.Source.TemplateObject).Type == "AWS::EC2::RouteTable").ToList();

            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected route table \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                return referencedId;
            }

            // If we get here, then Firefly.CloudFormationParser did not correctly resolve the dependency
            // and is most likely a bug there.
            if (dependencies.Count == 0)
            {
                this.LogError(
                    $"Cannot find related route table for {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            if (dependencies.Count > 1)
            {
                this.LogError(
                    $"Multiple subnets route table to {this.ImportSettings.Resource.LogicalId}. This is probably a bug in Firefly.CloudFormationParser");
            }

            return null;
        }

        private string GetSubnet()
        {
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                     && e.Source.TemplateObject is IResource && e.Tag != null
                     && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                d => ((IResource)d.Source.TemplateObject).Type == "AWS::EC2::Subnet").ToList();

            if (dependencies.Count == 1)
            {
                var r = (IResource)dependencies.First().Source.TemplateObject;

                this.LogInformation($"Auto-selected subnet \"{r.Name}\" based on dependency graph.");

                var referencedId = this.ImportSettings.ResourcesToImport
                    .First(rr => rr.AwsType == r.Type && rr.LogicalId == r.Name).PhysicalId;

                return referencedId;
            }

            return null;
        }
    }
}