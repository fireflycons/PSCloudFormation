namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Resource importer for user to match a lambda permission with the correct lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.Importers.ResourceImporter" />
    internal class LambdaPermissionImporter : ResourceImporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaPermissionImporter"/> class.
        /// </summary>
        /// <param name="permissionId">The permission identifier.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        public LambdaPermissionImporter(
            string permissionId,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport)
            : base(permissionId, ui, resourcesToImport)
        {
        }

        /// <summary>
        /// Gets the import identifier.
        /// </summary>
        /// <param name="caption">The caption for the interactive session.</param>
        /// <param name="message">The message for the interactive session.</param>
        /// <returns>
        /// The resource selected by the user, else <c>null</c> if cancelled.
        /// </returns>
        public override string GetImportId(string caption, string message)
        {
            var lambdas = this.ResourcesToImport.Where(r => r.AwsType == "AWS::Lambda::Function");

            var selection = this.SelectResource(
                caption,
                message,
                lambdas.Select(l => l.AwsAddress).ToList());

            return selection == -1 ? null : this.ResourcesToImport[selection].PhysicalId;
        }
    }
}