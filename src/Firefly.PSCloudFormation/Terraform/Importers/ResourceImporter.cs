namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Host;

    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.Importers.Cognito;
    using Firefly.PSCloudFormation.Terraform.Importers.IAM;
    using Firefly.PSCloudFormation.Terraform.Importers.Lambda;
    using Firefly.PSCloudFormation.Terraform.Importers.Route53;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Base class for classes that attempt interactively to fix import issues
    /// </summary>
    internal abstract class ResourceImporter
    {
        /// <summary>
        /// The resource importers
        /// </summary>
        private static readonly Dictionary<string, Type> ResourceImporters = new Dictionary<string, Type>
                                                                                 {
                                                                                     {
                                                                                         "aws_lambda_permission",
                                                                                         typeof(
                                                                                             LambdaPermissionImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_iam_policy",
                                                                                         typeof(
                                                                                             IAMManagedPolicyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_route53_record",
                                                                                         typeof(Route53RecordImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_cognito_identity_pool_roles_attachment",
                                                                                         typeof(
                                                                                             CognitoIdentityPoolRoleAttachmentImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_cognito_user_pool_client",
                                                                                         typeof(
                                                                                             CognitoUserPoolClientImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_cognito_user_group",
                                                                                         typeof(
                                                                                             CognitoUserGroupImporter)
                                                                                     }
                                                                                 };

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceImporter"/> class.
        /// </summary>
        /// <param name="resource">Name of the resource.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        protected ResourceImporter(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            ITerraformSettings settings)
        {
            this.Resource = resource;
            this.Ui = ui;
            this.ResourcesToImport = resourcesToImport;
            this.Settings = settings;
        }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        protected ResourceImport Resource { get; }

        /// <summary>
        /// Gets the resources to import.
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        protected IList<ResourceImport> ResourcesToImport { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        protected ITerraformSettings Settings { get; }

        /// <summary>
        /// Gets the UI.
        /// </summary>
        /// <value>
        /// The UI.
        /// </value>
        protected IUserInterface Ui { get; }

        /// <summary>
        /// Factory to create a resource importer for given resource type.
        /// </summary>
        /// <param name="resource">The resource being imported.</param>
        /// <param name="ui">The UI.</param>
        /// <param name="resourcesToImport">The resources to import.</param>
        /// <param name="settings">Terraform export settings.</param>
        /// <returns>A <see cref="ResourceImporter"/> derivative, or <c>null</c> if none found.</returns>
        public static ResourceImporter Create(
            ResourceImport resource,
            IUserInterface ui,
            IList<ResourceImport> resourcesToImport,
            ITerraformSettings settings)
        {
            if (!ResourceImporters.ContainsKey(resource.TerraformType))
            {
                return null;
            }

            return (ResourceImporter)Activator.CreateInstance(
                ResourceImporters[resource.TerraformType],
                resource,
                ui,
                resourcesToImport,
                settings);
        }

        /// <summary>
        /// Determines whether the given resource requires use of a resource importer.
        /// </summary>
        /// <param name="terraformResourceType">Type of the terraform resource.</param>
        /// <returns><c>true</c> if importer is required.</returns>
        public static bool RequiresResoureImporter(string terraformResourceType)
        {
            return ResourceImporters.ContainsKey(terraformResourceType);
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

            var selection = this.Ui.PromptForChoice(caption, message, choices, 0);

            if (selection == 0)
            {
                return -1;
            }

            return selection - 1;
        }
    }
}