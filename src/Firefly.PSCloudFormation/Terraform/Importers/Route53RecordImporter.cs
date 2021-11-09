using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System.Linq;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Utils;

    internal class Route53RecordImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Route53RecordImporter"/> class.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        public Route53RecordImporter(ResourceImport resource, IUserInterface ui, IList<ResourceImport> resourcesToImport, ITerraformSettings settings)
            : base(resource, ui, resourcesToImport, settings)
        {
        }

        /// <inheritdoc />
        public override string GetImportId(string caption, string message)
        {
            var tempateResource = this.Settings.Resources.First(r => r.LogicalResourceId == this.Resource.LogicalId)
                .TemplateResource;

            // ReSharper disable once PossibleNullReferenceException - can't be null for this resource type.
            var typeProperty = tempateResource.Properties["Type"];
            var zoneIdProperty = tempateResource.Properties.ContainsKey("HostedZoneId")
                                     ? tempateResource.Properties["HostedZoneId"]
                                     : null;

            if (zoneIdProperty == null)
            {
                this.Ui.Information($"Cannot determine Zone ID for resource \"{this.Resource.LogicalId}\"");
                return null;
            }

            string type;
            string zoneId;

            switch (typeProperty)
            {
                case IIntrinsic intrinsic:

                    type =  intrinsic.Evaluate(this.Settings.Template).ToString();
                    break;

                case string s:

                    type = s;
                    break;

                default:

                    this.Ui.Information($"Invalid type {typeProperty.GetType().Name} for R53 recordset \"Type\" property.");
                    return null;
            }

            switch (zoneIdProperty)
            {
                case IIntrinsic intrinsic:

                    zoneId = intrinsic.Evaluate(this.Settings.Template).ToString();
                    break;

                case string s:

                    zoneId = s;
                    break;

                default:

                    this.Ui.Information($"Invalid type {typeProperty.GetType().Name} for R53 recordset \"ZoneId\" property.");
                    return null;
            }

            // ZONEID_RECORDNAME_TYPE
            return $"{zoneId}_{Resource.PhysicalId}_{type}";
        }
    }
}
