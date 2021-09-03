namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Host;

    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Base class for classes that attempt interactively to fix import issues
    /// </summary>
    internal abstract class ResourceImporter
    {
        /// <summary>
        /// The resource importers
        /// </summary>
        private static readonly Dictionary<string, Type> ResourceImporters =
            new Dictionary<string, Type> { { "aws_lambda_permission", typeof(LambdaPermissionImporter) } };

        /// <summary>
        /// The UI
        /// </summary>
        private readonly IUserInterface ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceImporter"/> class.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        protected ResourceImporter(
            string resourceName,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport)
        {
            this.ResourceName = resourceName;
            this.ui = ui;
            this.ResourcesToImport = resourcesToImport;
        }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        protected string ResourceName { get; }

        /// <summary>
        /// Gets the resources to import.
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        protected IList<ResourceImport> ResourcesToImport { get; }

        /// <summary>
        /// Factory to create a resource importer for given resource type.
        /// </summary>
        /// <param name="terraformResourceType">Type of the terraform resource.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <returns>A <see cref="ResourceImporter"/> derivative, or <c>null</c> if none found.</returns>
        public static ResourceImporter Create(
            string terraformResourceType,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport)
        {
            if (!ResourceImporters.ContainsKey(terraformResourceType))
            {
                return null;
            }

            return (ResourceImporter)Activator.CreateInstance(
                ResourceImporters[terraformResourceType],
                terraformResourceType,
                ui,
                resourcesToImport);
        }

        /// <summary>
        /// Gets the import identifier.
        /// </summary>
        /// <param name="caption">The caption for the interactive session.</param>
        /// <param name="message">The message for the interactive session.</param>
        /// <returns>The resource selected by the user, else <c>null</c> if cancelled.</returns>
        public abstract string GetImportId(string caption, string message);

        /// <summary>
        /// Interactively selects the resource.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="message">The message.</param>
        /// <param name="selections">The selections.</param>
        /// <returns>Selected resource; else -1 if none selected.</returns>
        protected int SelectResource(string caption, string message, IList<string> selections)
        {
            const string Labels = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var choices = new Collection<ChoiceDescription> { new ChoiceDescription("&0 Skip Resource") };

            for (var i = 0; i < Labels.Length - 1 && i < selections.Count; ++i)
            {
                choices.Add(new ChoiceDescription($"&{Labels[i + 1]} {selections[i]}"));
            }

            var selection = this.ui.PromptForChoice(caption, message, choices, 0);

            if (selection == 0)
            {
                return -1;
            }

            return selection - 1;
        }
    }
}