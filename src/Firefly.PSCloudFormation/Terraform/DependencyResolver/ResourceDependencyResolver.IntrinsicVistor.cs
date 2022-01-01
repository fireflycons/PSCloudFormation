namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.TemplateObjects.Traversal;
    using Firefly.CloudFormationParser.Utils;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json.Linq;

    /// <content>
    /// This part handles a visit to the parsed CloudFormation resource, gathering intrinsic functions
    /// that need to be expressed in HCL as functions or references.
    /// </content>
    internal partial class ResourceDependencyResolver
    {
        /// <summary>
        /// Special case for !If. We don't process it directly, instead handing off to the first function in the
        /// branch selected by prevailing conditions.
        /// </summary>
        /// <seealso cref="IntrinsicInfo" />
        public class IfIntrinsicInfo : IntrinsicInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IfIntrinsicInfo"/> class.
            /// </summary>
            /// <param name="propertyPath">The property path.</param>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="resourceMapping">Summary info of the resource targeted by this intrinsic.</param>
            /// <param name="evaluation">The evaluation.</param>
            public IfIntrinsicInfo(
                PropertyPath propertyPath,
                IIntrinsic intrinsic,
                ResourceMapping resourceMapping,
                object evaluation)
                : base(propertyPath, intrinsic, resourceMapping, evaluation)
            {
            }

            /// <inheritdoc />
            public override object Evaluation =>
                this.NestedIntrinsics.Any() ? this.NestedIntrinsics.First().Evaluation : this.InitialEvaluation;

            /// <inheritdoc />
            public override IIntrinsic Intrinsic =>
                this.NestedIntrinsics.Any() ? this.NestedIntrinsics.First().Intrinsic : this.intrinsic;

            /// <inheritdoc />
            public override ResourceMapping TargetResource =>
                this.NestedIntrinsics.Any() ? this.NestedIntrinsics.First().TargetResource : this.targetResource;
        }

        /// <summary>
        /// Visits the properties of a CloudFormation resource extracting intrinsic functions
        /// we need for dependency resolution
        /// </summary>
        /// <seealso cref="TemplateObjectVisitor{TContext}" />
        private class IntrinsicVisitor : TemplateObjectVisitor<IntrinsicVisitorContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IntrinsicVisitor"/> class.
            /// </summary>
            /// <param name="template">The parsed CloudFormation template.</param>
            public IntrinsicVisitor(ITemplate template)
                : base(template)
            {
            }

            /// <summary>
            /// Visits the specified intrinsic and dispatches to visit handler for each distinct subclass of <see cref="T:Firefly.CloudFormationParser.Intrinsics.IIntrinsic" />.
            /// </summary>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="context">The context.</param>
            protected override void Visit(IIntrinsic intrinsic, IntrinsicVisitorContext context)
            {
                context.EnterIntrinsic(intrinsic, this.Path);
                base.Visit(intrinsic, context);
                context.ExitIntrinsic();
            }
        }

        /// <summary>
        /// Context object used when visiting a CloudFormation resource
        /// </summary>
        private class IntrinsicVisitorContext : ITemplateObjectVisitorContext<IntrinsicVisitorContext>
        {
            /// <summary>
            /// Reference to the current cloud formation resource being visited.
            /// </summary>
            private readonly IResource currentCloudFormationResource;

            /// <summary>
            /// Stack associated with <see cref="currentIntrinsicInfo"/>
            /// </summary>
            private readonly Stack<IntrinsicInfo> intrinsicInfos = new Stack<IntrinsicInfo>();

            /// <summary>
            /// The current module being processed.
            /// </summary>
            private readonly ModuleInfo module;

            /// <summary>
            /// The settings
            /// </summary>
            private readonly ITerraformExportSettings settings;

            /// <summary>
            /// Reference to parsed CLoudFormation template.
            /// </summary>
            private readonly ITemplate template;

            /// <summary>
            /// Reference to the exporter's warnings collection so warnings can be added to it.
            /// </summary>
            private readonly IList<string> warnings;

            /// <summary>
            /// The intrinsic whose properties are currently being examined
            /// </summary>
            private IntrinsicInfo currentIntrinsicInfo;

            /// <summary>
            /// Stores any <see cref="DependencyResolutionWarning"/> that may be thrown from inside a recursion of an intrinsic.
            /// When the recursion ends, this indicates not to store information about the parent intrinsic and to warn the user
            /// that a reference cannot be generated. 
            /// </summary>
            private DependencyResolutionWarning lastWarning;

            /// <summary>
            /// Path within CloudFormation resource of the parent intrinsic of a set of nested intrinsic.
            /// </summary>
            private PropertyPath parentIntrinsicPath;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntrinsicVisitorContext"/> class.
            /// </summary>
            /// <param name="settings">Main settings object.</param>
            /// <param name="terraformResources">All imported terraform resources.</param>
            /// <param name="inputs">The list of input variables and data sources.</param>
            /// <param name="resource">The CLoudFormation resource being visited.</param>
            /// <param name="warnings">The warnings collection.</param>
            /// <param name="module">The current module being processed.</param>
            public IntrinsicVisitorContext(
                ITerraformExportSettings settings,
                IReadOnlyCollection<StateFileResourceDeclaration> terraformResources,
                IList<InputVariable> inputs,
                IResource resource,
                IList<string> warnings,
                ModuleInfo module)
            {
                this.module = module;
                this.settings = settings;
                this.currentCloudFormationResource = resource;
                this.warnings = warnings;
                this.TerraformResources = terraformResources;
                this.Inputs = inputs;
                this.template = settings.Template;
            }

            /// <summary>
            /// Gets the reference locations.
            /// </summary>
            /// <value>
            /// The reference locations.
            /// </value>
            public List<IntrinsicInfo> ReferenceLocations { get; } = new List<IntrinsicInfo>();

            /// <summary>
            /// Gets all CloudFormation resources read from stack
            /// </summary>
            private IEnumerable<CloudFormationResource> CloudFormationResources => this.settings.Resources;

            /// <summary>
            /// Gets all current CloudFormation parameters with values expressed as terraform input variables.
            /// </summary>
            private IList<InputVariable> Inputs { get; }

            /// <summary>
            /// Gets all terraform resources read from state file
            /// </summary>
            private IReadOnlyCollection<StateFileResourceDeclaration> TerraformResources { get; }

            /// <summary>
            /// Begin processing an intrinsic.
            /// </summary>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="currentPath">The current path.</param>
            public void EnterIntrinsic(IIntrinsic intrinsic, PropertyPath currentPath)
            {
                if (this.lastWarning != null)
                {
                    // Something that can't be resolved has been found,
                    // so don't process any more here.
                    return;
                }

                var clonedPath = currentPath.Clone();

                try
                {
                    if (this.parentIntrinsicPath == null)
                    {
                        // This is a "top level" intrinsic associated directly with a resource attribute.
                        if (this.currentCloudFormationResource.Type == "AWS::Lambda::Permission"
                            && currentPath.Path == "FunctionName" && intrinsic is RefIntrinsic)
                        {
                            // !! NASTY KLUDGE ALERT !!
                            // AWS treats this property as a !Ref which is lambda function's name, but terraform actually wants the ARN here.
                            // If I find more cases like this, then I'll put something into resource traits
                            intrinsic = new GetAttIntrinsic(
                                intrinsic.GetReferencedObjects(this.template).First(),
                                "Arn");
                        }

                        this.intrinsicInfos.Push(this.currentIntrinsicInfo);
                        this.parentIntrinsicPath = clonedPath;
                        this.currentIntrinsicInfo = this.GetIntrinsicInfo(intrinsic, currentPath);
                    }
                    else
                    {
                        // We have descended the graph to member intrinsic
                        this.intrinsicInfos.Push(this.currentIntrinsicInfo);
                        var intrinsicInfo = this.GetIntrinsicInfo(intrinsic, currentPath);
                        this.currentIntrinsicInfo.NestedIntrinsics.Add(intrinsicInfo);

                        this.currentIntrinsicInfo = intrinsicInfo;
                    }
                }
                catch (DependencyResolutionWarning w)
                {
                    this.lastWarning = w;
                }
            }

            /// <summary>
            /// End processing an intrinsic.
            /// </summary>
            public void ExitIntrinsic()
            {
                var lastIntrinsicInfo = this.currentIntrinsicInfo;

                this.currentIntrinsicInfo = this.intrinsicInfos.Pop();

                if (this.currentIntrinsicInfo != null)
                {
                    return;
                }

                // Recursion has returned to the "top level" intrinsic, so store it.
                if (this.lastWarning == null)
                {
                    this.ReferenceLocations.Add(lastIntrinsicInfo);
                }
                else
                {
                    // Process the warning
                    this.warnings.Add(this.lastWarning.Message);
                    this.lastWarning = null;
                }

                this.parentIntrinsicPath = null;
            }

            /// <summary>
            /// Gets the next context for an item in a list.
            /// </summary>
            /// <param name="index">Index in current list</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
            public IntrinsicVisitorContext Next(int index)
            {
                return this;
            }

            /// <summary>
            /// Gets the next context for an entry in a dictionary
            /// </summary>
            /// <param name="name">Name of property.</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
            public IntrinsicVisitorContext Next(string name)
            {
                return this;
            }

            /// <summary>
            /// Gets the related information required to create <see cref="Reference"/> objects from the specified intrinsic.
            /// </summary>
            /// <param name="intrinsic">The intrinsic.</param>
            /// <param name="currentPath">Location within CloudFormation resource of this intrinsic</param>
            /// <returns>An <see cref="IntrinsicInfo"/> object.</returns>
            /// <exception cref="Amazon.CloudFormation.Model.InvalidOperationException">Can't currently describe {intrinsic.TagName}</exception>
            private IntrinsicInfo GetIntrinsicInfo(IIntrinsic intrinsic, PropertyPath currentPath)
            {
                switch (intrinsic.Type)
                {
                    case IntrinsicType.If:

                        return new IfIntrinsicInfo(currentPath, intrinsic, null, intrinsic.Evaluate(this.template));

                    case IntrinsicType.Ref:

                        return this.ProcessRef((RefIntrinsic)intrinsic, currentPath);

                    case IntrinsicType.GetAtt:

                        return this.ProcessGetAtt((GetAttIntrinsic)intrinsic, currentPath);

                    case IntrinsicType.Base64:
                    case IntrinsicType.Cidr:
                    case IntrinsicType.FindInMap:
                    case IntrinsicType.Join:
                    case IntrinsicType.Select:
                    case IntrinsicType.Split:
                    case IntrinsicType.Sub:

                        return new IntrinsicInfo(currentPath, intrinsic, null, intrinsic.Evaluate(this.template));

                    case IntrinsicType.ImportValue:

                        return this.ProcessImportValue((ImportValueIntrinsic)intrinsic, currentPath);

                    default:

                        throw new UnreferenceableIntrinsicWarning(
                            intrinsic,
                            this.currentCloudFormationResource,
                            currentPath);
                }
            }

            /// <summary>
            /// Creates an <see cref="IntrinsicInfo"/> for a <c>!GetAtt</c> intrinsic.
            /// </summary>
            /// <param name="getAttIntrinsic">The <c>!GetAtt</c> intrinsic.</param>
            /// <param name="currentPath">The current path.</param>
            /// <returns>An <see cref="IntrinsicInfo"/></returns>
            private IntrinsicInfo ProcessGetAtt(GetAttIntrinsic getAttIntrinsic, PropertyPath currentPath)
            {
                object evaluation;

                // Logical name of the resource being referenced by this !GetAtt
                var (referencedResourceName, attribute) =
                    (Tuple<string, string>)getAttIntrinsic.Evaluate(this.template);

                // Is the reference to a nested stack module?
                var referencedModule =
                    this.module.NestedModules.FirstOrDefault(m => m.LogicalId == referencedResourceName);

                if (referencedModule != null)
                {
                    var targetModuleSummary = new ResourceMapping
                                                  {
                                                      AwsType = TerraformExporterConstants.AwsCloudFormationStack,
                                                      LogicalId = referencedModule.LogicalId,
                                                      PhysicalId = referencedModule.Name,
                                                      Module = referencedModule
                                                  };

                    var parts = attribute.Split('.');
                    evaluation = referencedModule.Outputs.Where(o => o.OutputKey == parts[1])
                        .Select(o => o.OutputValue).SingleOrDefault();

                    return new IntrinsicInfo(currentPath, getAttIntrinsic, targetModuleSummary, evaluation);
                }

                // State file instance of the resource being referenced by this !GetAtt
                var referencedResource = this.TerraformResources.FirstOrDefault(r => r.Name == referencedResourceName)
                    ?.Instances.First();

                if (referencedResource == null)
                {
                    // If not found, then reference is to a resource that couldn't be imported eg. a custom resource.
                    throw new UnsupportedResourceWarning(
                        getAttIntrinsic,
                        this.currentCloudFormationResource,
                        currentPath);
                }

                // CloudFormation instance of the resource being referenced by this !GetAtt
                var cloudFormationResource =
                    this.CloudFormationResources.First(r => r.LogicalResourceId == getAttIntrinsic.LogicalId);

                // Summary of the resource to which this !GetAtt refers to
                var targetResourceSummary = new ResourceMapping
                                                {
                                                    AwsType = cloudFormationResource.ResourceType,
                                                    LogicalId = cloudFormationResource.LogicalResourceId,
                                                    PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                    TerraformType = this.TerraformResources.First(
                                                        tr => tr.Name == cloudFormationResource.LogicalResourceId).Type
                                                };

                // Now attempt to match up the CloudFormation resource attribute name with the corresponding terraform one
                // and get the current value from state.
                // First, look up the attribute map
                var traits = ResourceTraitsCollection.Get(referencedResource.Parent.Type);

                if (traits.AttributeMap.ContainsKey(attribute))
                {
                    var token = referencedResource.Attributes[traits.AttributeMap[attribute]];
                    evaluation = GetEvaluation(token);
                }
                else if (attribute.StartsWith(TerraformExporterConstants.StackOutputQualifier))
                {
                    // Nested stack output reference
                    var token = referencedResource.Attributes.SelectToken(
                        attribute.Replace(
                            TerraformExporterConstants.StackOutputQualifier,
                            TerraformExporterConstants.StackOutputQualifier.ToLowerInvariant()));
                    evaluation = GetEvaluation(token);
                }
                else
                {
                    var result = getAttIntrinsic.GetTargetValue(this.template, referencedResource);

                    if (result.Success)
                    {
                        evaluation = result.Value;
                    }
                    else
                    {
                        throw new UnreferenceableIntrinsicWarning(
                            getAttIntrinsic,
                            cloudFormationResource.TemplateResource,
                            currentPath);
                    }
                }

                return new IntrinsicInfo(currentPath, getAttIntrinsic, targetResourceSummary, evaluation);

                object GetEvaluation(JToken token)
                {
                    if (token is JValue jv)
                    {
                        switch (jv.Type)
                        {
                            case JTokenType.String:

                                evaluation = jv.Value<string>();
                                break;

                            case JTokenType.Integer:
                            case JTokenType.Float:

                                evaluation = jv.Value<double>();
                                break;

                            case JTokenType.Boolean:

                                evaluation = jv.Value<bool>();
                                break;

                            default:

                                throw new InvalidOperationException(
                                    $"Unexpected JValue type: {jv.Type} while processing {getAttIntrinsic}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Unexpected JToken type: {token.Type} while processing {getAttIntrinsic}");
                    }

                    return evaluation;
                }
            }

            /// <summary>
            /// Creates an <see cref="IntrinsicInfo"/> for an ImportValue intrinsic.
            /// </summary>
            /// <param name="importValueIntrinsic">The import value intrinsic.</param>
            /// <param name="currentPath">The current path.</param>
            /// <returns>An <see cref="IntrinsicInfo"/></returns>
            private IntrinsicInfo ProcessImportValue(
                ImportValueIntrinsic importValueIntrinsic,
                PropertyPath currentPath)
            {
                // Evaluation will be the name of the export
                var target = importValueIntrinsic.Evaluate(this.template).ToString();
                var export = this.settings.StackExports.FirstOrDefault(e => e.Name == target);

                if (export == null)
                {
                    throw new MissingExportWarning(
                        importValueIntrinsic,
                        this.currentCloudFormationResource,
                        currentPath);
                }

                return new IntrinsicInfo(currentPath, importValueIntrinsic, null, export.Value);
            }

            /// <summary>
            /// Creates an <see cref="IntrinsicInfo"/> for a Ref intrinsic.
            /// </summary>
            /// <param name="refIntrinsic">The reference intrinsic.</param>
            /// <param name="currentPath">The current path.</param>
            /// <returns>An <see cref="IntrinsicInfo"/></returns>
            private IntrinsicInfo ProcessRef(RefIntrinsic refIntrinsic, PropertyPath currentPath)
            {
                object evaluation;
                var target = refIntrinsic.Reference;

                var param = this.Inputs.FirstOrDefault(i => i.Name == target);

                if (param != null)
                {
                    evaluation = param.IsScalar ? (object)param.ScalarIdentity : param.ListIdentity;
                    return new IntrinsicInfo(currentPath.Clone(), refIntrinsic, null, evaluation);
                }

                if (target.StartsWith("AWS::"))
                {
                    // An unsupported AWS pseudo parameter like AWS::StackName etc.
                    throw new UnsupportedPseudoParameterWarning(
                        refIntrinsic,
                        this.currentCloudFormationResource,
                        currentPath);
                }

                var cloudFormationResource = this.CloudFormationResources
                    .Where(r => TerraformExporterConstants.IgnoredResources.All(ir => ir != r.ResourceType))
                    .FirstOrDefault(r => r.LogicalResourceId == target);

                if (cloudFormationResource == null)
                {
                    // If not found, then reference is to a resource that couldn't be imported eg. a custom resource
                    // or a known unsupported type.
                    throw new UnsupportedResourceWarning(refIntrinsic, this.currentCloudFormationResource, currentPath);
                }

                var targetResourceSummary = new ResourceMapping
                                                {
                                                    AwsType = cloudFormationResource.ResourceType,
                                                    LogicalId = cloudFormationResource.LogicalResourceId,
                                                    PhysicalId = cloudFormationResource.PhysicalResourceId,
                                                    TerraformType = this.TerraformResources.First(
                                                        tr => tr.Name == cloudFormationResource.LogicalResourceId).Type
                                                };

                evaluation = cloudFormationResource.PhysicalResourceId;

                return new IntrinsicInfo(currentPath, refIntrinsic, targetResourceSummary, evaluation);
            }
        }
    }
}