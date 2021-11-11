namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Host;

    using Firefly.PSCloudFormation.Terraform.Importers.ApiGateway;
    using Firefly.PSCloudFormation.Terraform.Importers.Cognito;
    using Firefly.PSCloudFormation.Terraform.Importers.IAM;
    using Firefly.PSCloudFormation.Terraform.Importers.Lambda;
    using Firefly.PSCloudFormation.Terraform.Importers.Route53;
    using Firefly.PSCloudFormation.Terraform.Importers.VPC;

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
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_base_path_mapping",
                                                                                         typeof(ApiGatewayBasePathMappingImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_request_validator",
                                                                                         typeof(ApiGatewayApiDependencyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_usage_plan_key",
                                                                                         typeof(ApiGatewayUsagePlanKeyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_stage",
                                                                                         typeof(ApiGatewayApiDependencyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_resource",
                                                                                         typeof(ApiGatewayResourceImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_model",
                                                                                         typeof(ApiGatewayResourceImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_method",
                                                                                         typeof(ApiGatewayMethodImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_route",
                                                                                         typeof(RouteImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_route_table_association",
                                                                                         typeof(RouteTableAssociationImporter)
                                                                                     }
                                                                                 };

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceImporter"/> class.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        protected ResourceImporter(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
        {
            this.ImportSettings = importSettings;
            this.TerraformSettings = terraformSettings;
        }

        /// <summary>
        /// Gets the import settings.
        /// </summary>
        /// <value>
        /// The import settings.
        /// </value>
        protected IResourceImporterSettings ImportSettings { get; }

        /// <summary>
        /// Gets the terraform settings.
        /// </summary>
        /// <value>
        /// The terraform settings.
        /// </value>
        protected ITerraformSettings TerraformSettings { get; }

        /// <summary>
        /// Factory to create a resource importer for given resource type.
        /// </summary>
        /// <param name="importSettings">The import settings.</param>
        /// <param name="terraformSettings">The terraform settings.</param>
        /// <returns>Appropriate subtype of <see cref="ResourceImporter"/></returns>
        public static ResourceImporter Create(
            IResourceImporterSettings importSettings,
            ITerraformSettings terraformSettings)
        {
            if (!ResourceImporters.ContainsKey(importSettings.Resource.TerraformType))
            {
                return null;
            }

            return (ResourceImporter)Activator.CreateInstance(
                ResourceImporters[importSettings.Resource.TerraformType],
                importSettings,
                terraformSettings);
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

            var selection = this.ImportSettings.Ui.PromptForChoice(caption, message, choices, 0);

            if (selection == 0)
            {
                return -1;
            }

            return selection - 1;
        }

        /// <summary>
        /// Issue a warning
        /// </summary>
        /// <param name="message">The message.</param>
        protected void LogWarning(string message)
        {
            this.ImportSettings.Logger.LogWarning(message);
            this.ImportSettings.Warnings.Add(message);
        }

        /// <summary>
        /// Issue an error
        /// </summary>
        /// <param name="message">The message.</param>
        protected void LogError(string message)
        {
            this.ImportSettings.Logger.LogError($"ERROR: {message}");
            this.ImportSettings.Warnings.Add(message);
        }

        /// <summary>
        /// Issue an information message
        /// </summary>
        /// <param name="message">The message.</param>
        protected void LogInformation(string message)
        {
            this.ImportSettings.Logger.LogInformation(message);
        }
    }
}