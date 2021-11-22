namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation.Host;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.GraphObjects;
    using Firefly.PSCloudFormation.Terraform.Importers.ApiGateway;
    using Firefly.PSCloudFormation.Terraform.Importers.ApiGatewayV2;
    using Firefly.PSCloudFormation.Terraform.Importers.ApplicationAutoScaling;
    using Firefly.PSCloudFormation.Terraform.Importers.Cognito;
    using Firefly.PSCloudFormation.Terraform.Importers.ECS;
    using Firefly.PSCloudFormation.Terraform.Importers.ElasticLoadBalancingV2;
    using Firefly.PSCloudFormation.Terraform.Importers.IAM;
    using Firefly.PSCloudFormation.Terraform.Importers.Lambda;
    using Firefly.PSCloudFormation.Terraform.Importers.RDS;
    using Firefly.PSCloudFormation.Terraform.Importers.Route53;
    using Firefly.PSCloudFormation.Terraform.Importers.VPC;

    using ICSharpCode.SharpZipLib.Tar;

    using QuikGraph;

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
                                                                                     //{
                                                                                     //    "aws_iam_policy",
                                                                                     //    typeof(
                                                                                     //        IAMManagedPolicyImporter)
                                                                                     //},
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
                                                                                         typeof(
                                                                                             ApiGatewayBasePathMappingImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_request_validator",
                                                                                         typeof(
                                                                                             ApiGatewayApiDependencyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_usage_plan_key",
                                                                                         typeof(
                                                                                             ApiGatewayUsagePlanKeyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_stage",
                                                                                         typeof(
                                                                                             ApiGatewayApiDependencyImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_resource",
                                                                                         typeof(
                                                                                             ApiGatewayResourceImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_model",
                                                                                         typeof(
                                                                                             ApiGatewayResourceImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_api_gateway_method",
                                                                                         typeof(
                                                                                             ApiGatewayMethodImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_apigatewayv2_stage",
                                                                                         typeof(
                                                                                             ApiGatewayV2StageImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_route",
                                                                                         typeof(RouteImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_route_table_association",
                                                                                         typeof(
                                                                                             RouteTableAssociationImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_network_acl_rule",
                                                                                         typeof(
                                                                                             NetworkAclEntryImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_lb_listener_certificate",
                                                                                         typeof(ListenerCertificateImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_db_option_group",
                                                                                         typeof(DBOptionGroupImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_ecs_service",
                                                                                         typeof(ECSServiceImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_appautoscaling_target",
                                                                                         typeof(AASServiceScalableTargetImporter)
                                                                                     },
                                                                                     {
                                                                                         "aws_appautoscaling_policy",
                                                                                         typeof(AASAutoScalingPolicyImporter)
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

            this.AwsResource =
                this.TerraformSettings.Template.Resources.First(r => r.Name == this.ImportSettings.Resource.LogicalId);

            // ReSharper disable once VirtualMemberCallInConstructor - ReferencedAwsResource is initialized pre-construction with =>
            var dependencies = string.IsNullOrEmpty(this.ReferencedAwsResource)
                                   ? new List<TaggedEdge<IVertex, EdgeDetail>>()
                                   : this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                                           e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                                                && e.Source.TemplateObject is IResource && e.Tag != null
                                                && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                                           d => ((IResource)d.Source.TemplateObject).Type == this.ReferencedAwsResource)
                                       .ToList();

        }

        /// <summary>
        /// Gets the AWS resource parsed from the template that represents the resource being imported.
        /// </summary>
        /// <value>
        /// The aw resource.
        /// </value>
        protected IResource AwsResource { get; }

        /// <summary>
        /// Gets the referenced AWS resource, e.g. a AWS::Lambda::Permission references a AWS::Lambda::Function, so the function type goes here.
        /// </summary>
        /// <value>
        /// The referenced AWS resource.
        /// </value>
        protected abstract string ReferencedAwsResource { get; }

        /// <summary>
        /// Gets the path to the AWS resource property that should be evaluated to get a reference to another resource 
        /// </summary>
        /// <value>
        /// The referencing property path.
        /// </value>
        protected abstract string ReferencingPropertyPath { get; }

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
        public static bool RequiresResourceImporter(string terraformResourceType)
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
        /// Gets the resource referenced by the resource being processed based on the dependency graph edges for the resource type identified by <see cref="ReferencedAwsResource"/>.
        /// </summary>
        /// <returns>List of matching resource graph edges</returns>
        protected ResourceDependency GetResourceDependency()
        {
            return this.GetResourceDependency(this.ReferencedAwsResource);
        }

        /// <summary>
        /// Gets the resource referenced by the resource being processed based on the dependency graph edges for the resource type identified by <see cref="ReferencedAwsResource"/>.
        /// </summary>
        /// <param name="awsResourceType">Resource type to find graph edges for.</param>
        /// <returns>List of matching resource graph edges</returns>
        protected ResourceDependency GetResourceDependency(string awsResourceType)
        {
            // !Ref dependency
            var dependencies = this.GetDependencies(awsResourceType);

            if (dependencies == null)
            {
                return null;
            }

            switch (dependencies.Count)
            {
                case 1:
                    {
                        var referencedTemplateObject = (IResource)dependencies.First().Source.TemplateObject;
                        var referringTemplateObject = (IResource)dependencies.First().Target.TemplateObject;

                        this.LogInformation($"Auto-selected {referencedTemplateObject.Type} \"{referencedTemplateObject.Name}\" based on dependency graph.");

                        var referencedId = this.ImportSettings.ResourcesToImport
                            .First(rr => rr.AwsType == referencedTemplateObject.Type && rr.LogicalId == referencedTemplateObject.Name);

                        return new ResourceDependency(referencedId, referringTemplateObject, referencedTemplateObject);
                    }

                case 0 when string.IsNullOrEmpty(this.ReferencingPropertyPath):

                    this.LogError(
                        $"Cannot find related {awsResourceType} for {this.ImportSettings.Resource.LogicalId}.");

                    return null;

                case 0:
                    {
                        var thisTemplateResource =
                            this.TerraformSettings.Template.Resources.First(
                                r => r.Name == this.ImportSettings.Resource.LogicalId);

                        // Could be an imported value or a property or mapping reference.
                        if (thisTemplateResource.GetResourcePropertyValue(this.ReferencingPropertyPath) is string importName)
                        {
                            var importEvaluation = this.TerraformSettings.StackExports.FirstOrDefault(e => e.Name == importName)
                                ?.Value;

                            if (importEvaluation != null)
                            {
                                this.LogWarning(
                                    $"Resource \"{this.ImportSettings.Resource.LogicalId}\" references a resource imported from another stack.");

                                return new ResourceDependency(importEvaluation);
                            }

                            return new ResourceDependency(importName);
                        }

                        this.LogError(
                            $"Cannot find related {awsResourceType} for {this.ImportSettings.Resource.LogicalId}.");
                        break;
                    }
            }

            // More than one dependency
            this.LogError(
                $"Cannot find related {awsResourceType} for {this.ImportSettings.Resource.LogicalId}. Multiple possibilities found");

            return null;
        }

        protected string GetThisResourcePropertyValue(string propertyPath)
        {
            var thisResource =
                this.TerraformSettings.Template.Resources.First(
                    r => r.Name == this.ImportSettings.Resource.LogicalId);

            return thisResource.GetResourcePropertyValue(propertyPath)?.ToString();
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
            this.ImportSettings.Errors.Add($"ERROR: {message}");
        }

        /// <summary>
        /// Issue an information message
        /// </summary>
        /// <param name="message">The message.</param>
        protected void LogInformation(string message)
        {
            this.ImportSettings.Logger.LogInformation(message);
        }

        /// <summary>
        /// Gets the graph edge list of dependencies.
        /// </summary>
        /// <param name="awsResourceType">Type of the AWS resource.</param>
        /// <returns>List of matching graph edges</returns>
        private List<TaggedEdge<IVertex, EdgeDetail>> GetDependencies(string awsResourceType)
        {
            // !Ref dependency
            var dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                     && e.Source.TemplateObject is IResource && e.Tag != null
                     && e.Tag.ReferenceType == ReferenceType.DirectReference).Where(
                d => ((IResource)d.Source.TemplateObject).Type == awsResourceType).ToList();

            if (dependencies.Count > 1)
            {
                // More than one dependency
                this.LogError(
                    $"Cannot find related {awsResourceType} for {this.ImportSettings.Resource.LogicalId}. Multiple possibilities found");

                return null;
            }

            switch (dependencies.Count)
            {
                case 0:
                    {
                        // !GetAtt dependency
                        dependencies = this.TerraformSettings.Template.DependencyGraph.Edges.Where(
                            e => e.Target.TemplateObject.Name == this.ImportSettings.Resource.LogicalId
                                 && e.Source.TemplateObject is IResource && e.Tag != null
                                 && e.Tag.ReferenceType == ReferenceType.AttributeReference).Where(
                            d => ((IResource)d.Source.TemplateObject).Type == awsResourceType).ToList();

                        if (dependencies.Count > 1)
                        {
                            // More than one dependency
                            this.LogError(
                                $"Cannot find related {awsResourceType} for {this.ImportSettings.Resource.LogicalId}. Multiple possibilities found");

                            return null;
                        }

                        break;
                    }

                case 1:
                    return dependencies;
            }

            return dependencies;
        }
    }
}