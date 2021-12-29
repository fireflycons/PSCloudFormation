namespace Firefly.PSCloudFormation.Terraform.CloudFormationParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Abstractions;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Extension methods for CloudFormation intrinsics in <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.Intrinsics.Functions.html">Firefly.CloudFormationParser</see>.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    // ReSharper disable once UnusedMember.Global
    internal static class IntrinsicRenderingExtensions
    {
        /// <summary>
        /// Maps supported pseudo-parameters by name to <see cref="DataSourceReference"/>.
        /// </summary>
        private static readonly Dictionary<string, Reference> PseudoParameterToDataBlock =
            new Dictionary<string, Reference>
                {
                    { "AWS::Region", new DataSourceReference("aws_region", "current", "name", true) },
                    { "AWS::AccountId", new DataSourceReference("aws_caller_identity", "current", "account_id", true) },
                    { "AWS::Partition", new DataSourceReference("aws_partition", "partition", "partition", true) },
                    { "AWS::URLSuffix", new DataSourceReference("aws_partition", "url_suffix", "dns_suffix", true) }
                };

        /// <summary>
        /// Renders this intrinsic to a <see cref="Reference"/> that can be embedded into the in-memory state file.
        /// </summary>
        /// <param name="self">The intrinsic being rendered.</param>
        /// <param name="template">Reference to CloudFormation template.</param>
        /// <param name="resource">Resource referenced by the intrinsic.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="Reference"/> or <c>null</c> if a reference cannot be created.</returns>
        /// <exception cref="System.ArgumentNullException">self cannot be null</exception>
        public static Reference Render(
            this IIntrinsic self,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Render(template, resource, inputs, -1);
        }

        /// <summary>
        /// Renders this intrinsic to a <see cref="Reference"/> that can be embedded into the in-memory state file.
        /// </summary>
        /// <param name="self">The intrinsic being rendered.</param>
        /// <param name="template">Reference to CloudFormation template.</param>
        /// <param name="resource">Resource referenced by the intrinsic.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="index">The index to add as an indexer on the generated reference.</param>
        /// <returns>A <see cref="Reference"/> or <c>null</c> if a reference cannot be created.</returns>
        /// <exception cref="System.ArgumentNullException">self cannot be null</exception>
        public static Reference Render(
            this IIntrinsic self,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs,
            int index)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            Reference reference;

            switch (self)
            {
                case Base64Intrinsic base64Intrinsic:

                    reference = Render(base64Intrinsic, template, resource, inputs);
                    break;

                case CidrIntrinsic cidrIntrinsic:

                    reference = Render(cidrIntrinsic, template, resource, inputs, index);
                    break;

                case FindInMapIntrinsic findInMapIntrinsic:

                    reference = Render(findInMapIntrinsic, template, resource, inputs, index);
                    break;

                case GetAttIntrinsic getAttIntrinsic:

                    reference = Render(getAttIntrinsic, template, resource);
                    break;

                case GetAZsIntrinsic getAZsIntrinsic:

                    reference = Render(getAZsIntrinsic, inputs, index);
                    break;

                case ImportValueIntrinsic importValueIntrinsic:

                    reference = Render(importValueIntrinsic, template, inputs);
                    break;

                case JoinIntrinsic joinIntrinsic:

                    reference = Render(joinIntrinsic, template, resource, inputs);
                    break;

                case RefIntrinsic refIntrinsic:

                    reference = Render(refIntrinsic, template, resource, index);
                    break;

                case SelectIntrinsic selectIntrinsic:

                    reference = Render(selectIntrinsic, template, resource, inputs);
                    break;

                case SplitIntrinsic splitIntrinsic:

                    reference = Render(splitIntrinsic, template, resource, inputs);
                    break;

                case SubIntrinsic subIntrinsic:

                    reference = Render(subIntrinsic, template, resource, inputs);
                    break;

                default:

                    throw new InvalidOperationException($"No renderer for '{self.TagName}'.");
            }

            if (reference != null)
            {
                return reference;
            }

            throw new InvalidOperationException($"Failed rendering '{self.TagName}'.");
        }

        /// <summary>
        /// Renders the specified base64 intrinsic to a <c>base64encode</c> function call.
        /// </summary>
        /// <param name="base64Intrinsic">The base64 intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="FunctionReference"/> to an HCL base64encode() expression.</returns>
        private static Reference Render(
            Base64Intrinsic base64Intrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            object argument;

            if (base64Intrinsic.ValueToEncode is IIntrinsic intrinsic)
            {
                argument = Render(intrinsic, template, resource, inputs).ToJConstructor();
            }
            else
            {
                argument = base64Intrinsic.ValueToEncode.ToString();
            }

            return new FunctionReference("base64encode", new[] { argument });
        }

        /// <summary>
        /// Renders the specified <c>!Cidr</c> intrinsic to a <c>cidrsubnets</c> reference, optionally with indexer.
        /// </summary>
        /// <param name="cidrIntrinsic">The cidr intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="index">The index.</param>
        /// <returns>A <see cref="FunctionReference"/></returns>
        private static Reference Render(
            CidrIntrinsic cidrIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs,
            int index)
        {
            var arguments = new List<object>
                                {
                                    RenderObject(cidrIntrinsic.IpBlock, template, resource, inputs)
                                };

            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < cidrIntrinsic.Count; ++i)
            {
                // CidrBits is currently int. May need to be intrinsic
                arguments.Add(cidrIntrinsic.CidrBits);
            }

            return new FunctionReference("cidrsubnets", arguments, index);
        }

        /// <summary>
        /// Renders a <c>!FindInMap</c> to a lookup on <c>locals.mappings</c>
        /// </summary>
        /// <param name="findInMapIntrinsic">The <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.Intrinsics.Functions.FindInMapIntrinsic.html"><c>FindInMap</c></see> intrinsic to render.</param>
        /// <param name="template">The <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.ITemplate.html">imported template</see>.</param>
        /// <param name="resource">The related new terraform resource where this map lookup TODO might need to be all imported resources</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="index">Index of second level key item to return, when this is an array</param>
        /// <returns>A <see cref="MapReference"/>.</returns>
        private static Reference Render(
            FindInMapIntrinsic findInMapIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs,
            int index)
        {
            var sb = new StringBuilder();
            var mapParts = new Stack<string>();
            mapParts.Push("local");
            mapParts.Push("mappings");

            foreach (var property in new[]
                                         {
                                             findInMapIntrinsic.MapName, findInMapIntrinsic.TopLevelKey,
                                             findInMapIntrinsic.SecondLevelKey
                                         })
            {
                switch (property)
                {
                    case string s:

                        sb.Append($".{s}");
                        break;

                    case RefIntrinsic refIntrinsic:

                        sb.Append($"[{refIntrinsic.Render(template, resource, inputs)}]");
                        break;

                    case IIntrinsic intrinsic:

                        throw new InvalidOperationException($"Intrinsic \"{intrinsic.TagName}\" cannot be resolved.");
                }
            }

            if (index > -1)
            {
                sb.Append($"[{index}]");
            }

            return new MapReference(sb.ToString());
        }

        /// <summary>
        /// Renders the specified GetAZs intrinsic to a data source reference
        /// </summary>
        /// <param name="getAZsIntrinsic">The GetAZs intrinsic.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <param name="index">The index.</param>
        /// <returns>A <see cref="DataSourceReference"/> to <c>aws_availability_zones</c></returns>
        // ReSharper disable once UnusedParameter.Local - there for clarity
        private static Reference Render(GetAZsIntrinsic getAZsIntrinsic, IList<InputVariable> inputs, int index)
        {
            // Add an entry to parameters so it gets emitted
            if (!inputs.Any(i => i.IsDataSource && i.Address == "aws_availability_zones.available"))
            {
                inputs.Add(
                    new DataSourceInput(
                        "aws_availability_zones",
                        "available",
                        new Dictionary<string, string> { { "state", "available" } }));
            }

            // This is only going to work against the provider's region
            return new DataSourceReference("aws_availability_zones", "available", $"names[{index}]", false);
        }

        /// <summary>
        /// Renders the specified import value intrinsic.
        /// </summary>
        /// <param name="importValueIntrinsic">The import value intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="DataSourceReference"/> to <c>aws_cloudformation_export</c></returns>
        // ReSharper disable once SuggestBaseTypeForParameter - want this to be explicit.
        private static Reference Render(ImportValueIntrinsic importValueIntrinsic, ITemplate template, IList<InputVariable> inputs)
        {
            var exportName = importValueIntrinsic.Evaluate(template).ToString();
            var dataSourceAddress = $"aws_cloudformation_export.{exportName}";

            // Add an entry to parameters so it gets emitted
            if (!inputs.Any(i => i.IsDataSource && i.Address == dataSourceAddress))
            {
                inputs.Add(
                    new DataSourceInput(
                        "aws_cloudformation_export",
                        exportName,
                        new Dictionary<string, string> { { "name ", exportName } }));
            }

            return new DataSourceReference("aws_cloudformation_export", exportName, "value", false);
        }

        /// <summary>
        /// Renders the specified reference intrinsic.
        /// </summary>
        /// <param name="refIntrinsic">The reference intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="index">The index.</param>
        /// <returns>A <see cref="Reference"/> derivative according to what is being referenced.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Pseudo parameter \"{pseudo}\" cannot be referenced by terraform.
        /// or
        /// Reference \"{refIntrinsic.Reference}\" cannot be resolved.
        /// </exception>
        private static Reference Render(
            RefIntrinsic refIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            int index)
        {
            switch (refIntrinsic.Reference)
            {
                case string pseudo when pseudo.StartsWith("AWS::"):

                    if (PseudoParameterToDataBlock.ContainsKey(pseudo))
                    {
                        return PseudoParameterToDataBlock[pseudo];
                    }

                    throw new InvalidOperationException(
                        $"Pseudo parameter \"{pseudo}\" cannot be referenced by terraform.");

                default:

                    var param = template.Parameters.FirstOrDefault(p => p.Name == refIntrinsic.Reference);

                    if (param != null)
                    {
                        if (param.IsSsmParameter)
                        {
                            return new DataSourceReference("aws_ssm_parameter", refIntrinsic.Reference, "value", true);
                        }

                        return index < 0
                                   ? new InputVariableReference(refIntrinsic.Reference)
                                   : new InputVariableReference(refIntrinsic.Reference, index);
                    }

                    if (refIntrinsic.ExtraData is IntrinsicInfo intrinsicInfo)
                    {
                        resource = intrinsicInfo.TargetResource;
                    }

                    if (resource != null && template.Resources.Any(r => r.Name == resource.LogicalId))
                    {
                        return new DirectReference(resource.Address);
                    }

                    throw new InvalidOperationException($"Reference \"{refIntrinsic.Reference}\" cannot be resolved.");
            }
        }

        /// <summary>
        /// Renders the specified select intrinsic.
        /// </summary>
        /// <param name="selectIntrinsic">The select intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="Reference"/> derivative according to what is being selected, with selection indexer.</returns>
        private static Reference Render(
            SelectIntrinsic selectIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            if (selectIntrinsic.Items.Count == 1 && selectIntrinsic.Items[0] is IIntrinsic intrinsic)
            {
                return intrinsic.Render(template, resource, inputs, selectIntrinsic.Index);
            }

            return null;
        }

        /// <summary>
        /// Renders the specified GetAtt intrinsic.
        /// </summary>
        /// <param name="getAttIntrinsic">The GetAtt intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <returns>An <see cref="IndirectReference"/> to an attribute on another resource.</returns>
        private static Reference Render(GetAttIntrinsic getAttIntrinsic, ITemplate template, ResourceMapping resource)
        {
            string attributeName;

            if (getAttIntrinsic.AttributeName is IIntrinsic)
            {
                if (!(getAttIntrinsic.AttributeName is RefIntrinsic refIntrinsic))
                {
                    // Only !Ref allowed here
                    return null;
                }

                attributeName = refIntrinsic.Evaluate(template).ToString();
            }
            else
            {
                attributeName = getAttIntrinsic.AttributeName.ToString();
            }

            // This GetAtt refers to a nested stack which may or may not have been imported as a terraform module.
            if (attributeName.StartsWith(TerraformExporterConstants.StackOutputQualifier))
            {
                var referencedOutput =
                    attributeName.Substring(TerraformExporterConstants.StackOutputQualifier.Length);

                if (resource.Module?.LogicalId == getAttIntrinsic.LogicalId && resource.Module.Outputs.Any(o => o.OutputKey == referencedOutput))
                {
                    // This reference is to a module output
                    return new ModuleReference($"{resource.Module.Name}.{referencedOutput}");
                }

                // The reference is to an aws_cloudformation_stack as the use did not elect to import nested stacks.
                // Just lowercase "Outputs." which will then resolve to the terraform resource.
                attributeName = attributeName.Replace(
                    TerraformExporterConstants.StackOutputQualifier,
                    TerraformExporterConstants.StackOutputQualifier.ToLowerInvariant());
            }
            else
            {
                var traits = ResourceTraitsCollection.Get(resource.TerraformType);

                attributeName = traits.AttributeMap.ContainsKey(attributeName)
                                    ? traits.AttributeMap[attributeName]
                                    : attributeName.CamelCaseToSnakeCase();
            }

            if (getAttIntrinsic.ExtraData is IntrinsicInfo intrinsicInfo)
            {
                resource = intrinsicInfo.TargetResource;
            }

            return template.Resources.Any(r => r.Name == resource.LogicalId)
                       ? new IndirectReference($"{resource.Address}.{attributeName}")
                       : null;
        }

        /// <summary>
        /// Renders the specified join intrinsic.
        /// </summary>
        /// <param name="joinIntrinsic">The join intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="FunctionReference"/> to an HCL join() expression.</returns>
        private static Reference Render(
            JoinIntrinsic joinIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            // Build up a join() function reference
            var joinArguments = new List<object> { joinIntrinsic.Separator };
            var joinList = new List<object>();

            foreach (var item in joinIntrinsic.Items)
            {
                switch (item)
                {
                    case IIntrinsic nestedIntrinsic:

                        joinList.Add(Render(nestedIntrinsic, template, resource, inputs).ToJConstructor());
                        break;

                    default:

                        // join() is a string function - all args are therefore string
                        joinList.Add(item.ToString());
                        break;
                }
            }

            joinArguments.Add(joinList);

            return new FunctionReference("join", joinArguments);
        }

        /// <summary>
        /// Renders the specified split intrinsic.
        /// </summary>
        /// <param name="splitIntrinsic">The split intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="FunctionReference"/> to the <c>split()</c> built-in.</returns>
        private static Reference Render(
            SplitIntrinsic splitIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            var splitArguments = new List<object> { splitIntrinsic.Delimiter };

            switch (splitIntrinsic.Source)
            {
                case IIntrinsic intrinsic:

                    splitArguments.Add(Render(intrinsic, template, resource, inputs).ToJConstructor());
                    break;

                default:

                    splitArguments.Add(splitIntrinsic.Source.ToString());
                    break;
            }

            return new FunctionReference("split", splitArguments);
        }

        /// <summary>
        /// Renders the specified sub intrinsic.
        /// </summary>
        /// <param name="subIntrinsic">The sub intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>A <see cref="InterpolationReference"/> for the interpolated string to insert.</returns>
        private static Reference Render(
            SubIntrinsic subIntrinsic,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            // Build up an interpolated string as the replacement
            // Start with the !Sub intrinsic expression.
            var expression = subIntrinsic.Expression;

            var replacements = new Dictionary<string, string>();

            // Go through any intrinsics associated with this !Sub
            foreach (var nestedIntrinsic in subIntrinsic.ImplicitReferences.Cast<IReferenceIntrinsic>())
            {
                // Try to render to an HCL expression
                var reference = Render(nestedIntrinsic, template, resource, inputs);

                if (reference == null)
                {
                    return null;
                }

                replacements.Add(nestedIntrinsic.ReferencedObject(template), reference.ReferenceExpression);
            }

            foreach (var substitution in subIntrinsic.Substitutions)
            {
                string replacement;

                if (substitution.Value is IIntrinsic intrinsic)
                {
                    replacement = Render(intrinsic, template, resource, inputs).ReferenceExpression;
                }
                else
                {
                    replacement = substitution.Value.ToString();
                }

                replacements.Add(substitution.Key, replacement);
            }

            expression = replacements.Aggregate(
                expression,
                (current, replacement) => current.Replace($"${{{replacement.Key}}}", $"${{{replacement.Value}}}"));

            // Add interpolation modification.
            return new InterpolationReference(expression);
        }

        /// <summary>
        /// Renders the object.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="template">The template.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="inputs">The list of input variables and data sources.</param>
        /// <returns>Object value which is a JConstructor if the input is an intrinsic, else the string value of the input.</returns>
        private static object RenderObject(
            object value,
            ITemplate template,
            ResourceMapping resource,
            IList<InputVariable> inputs)
        {
            if (value is IIntrinsic intrinsic)
            {
                return Render(intrinsic, template, resource, inputs).ToJConstructor();
            }

            return value.ToString();
        }
    }
}