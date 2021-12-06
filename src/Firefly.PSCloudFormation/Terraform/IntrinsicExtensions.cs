namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.Hcl;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Extension methods for CloudFormation intrinsics in <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.Intrinsics.Functions.html">Firefly.CloudFormationParser</see>.
    /// </summary>
    internal static class IntrinsicExtensions
    {
        private static readonly Dictionary<string, Reference> PseudoParmeterToDataBlock =
            new Dictionary<string, Reference>
                {
                    { "AWS::Region", new DataSourceReference("aws_region", "current", "name") },
                    { "AWS::AccountId", new DataSourceReference("aws_caller_identity", "current", "account_id") },
                    { "AWS::Partition", new DataSourceReference("aws_partition", "partition", "partition") },
                    { "AWS::URLSuffix", new DataSourceReference("aws_partition", "url_suffix", "dns_suffix") }
                };


        /// <summary>
        /// Renders this intrinsic to a <see cref="Reference"/> that can be embedded into the in-memory state file.
        /// </summary>
        /// <param name="self">The intrinsic being rendered.</param>
        /// <param name="template">Reference to CloudFormation template.</param>
        /// <param name="resource">Resource referenced by the intrinsic.</param>
        /// <returns>A <see cref="Reference"/> or <c>null</c> if a reference cannot be created.</returns>
        /// <exception cref="System.ArgumentNullException">self cannot be null</exception>
        public static Reference Render(this IIntrinsic self, ITemplate template, ImportedResource resource)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return self.Render(template, resource, -1);
        }

        /// <summary>
        /// Renders this intrinsic to a <see cref="Reference"/> that can be embedded into the in-memory state file.
        /// </summary>
        /// <param name="self">The intrinsic being rendered.</param>
        /// <param name="template">Reference to CloudFormation template.</param>
        /// <param name="resource">Resource referenced by the intrinsic.</param>
        /// <param name="index">The index to add as an indexer on the generated reference.</param>
        /// <returns>A <see cref="Reference"/> or <c>null</c> if a reference cannot be created.</returns>
        /// <exception cref="System.ArgumentNullException">self cannot be null</exception>
        public static Reference Render(
            this IIntrinsic self,
            ITemplate template,
            ImportedResource resource,
            int index)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            switch (self)
            {
                case FindInMapIntrinsic findInMapIntrinsic:

                    return Render(findInMapIntrinsic, template, resource, index);

                case GetAZsIntrinsic getAZsIntrinsic:

                    return Render(getAZsIntrinsic, template, resource, index);

                case RefIntrinsic refIntrinsic:

                    return Render(refIntrinsic, template, resource, index);

                case SelectIntrinsic selectIntrinsic:

                    return Render(selectIntrinsic, template, resource);

                case GetAttIntrinsic getAttIntrinsic:

                    return Render(getAttIntrinsic, template, resource);
            }

            return null;
        }

        /// <summary>
        /// Renders a <c>!FindInMap</c> to a lookup on <c>locals.mappings</c>
        /// </summary>
        /// <param name="findInMapIntrinsic">The <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.Intrinsics.Functions.FindInMapIntrinsic.html"><c>FindInMap</c></see> intrinsic to render.</param>
        /// <param name="template">The <see href="https://fireflycons.github.io/Firefly.CloudFormationParser/api/Firefly.CloudFormationParser.ITemplate.html">imported template</see>.</param>
        /// <param name="resource">The related new terraform resource where this map lookup TODO might need to be all imported resources</param>
        /// <param name="index">Index of second level key item to return, when this is an array</param>
        /// <returns>A <see cref="MapReference"/>.</returns>
        private static Reference Render(
            FindInMapIntrinsic findInMapIntrinsic,
            ITemplate template,
            ImportedResource resource,
            int index)
        {
            var sb = new StringBuilder();
            var mapParts = new Stack<string>();
            mapParts.Push("local");
            mapParts.Push("mappings");

            foreach (var property in new[] { findInMapIntrinsic.MapName, findInMapIntrinsic.TopLevelKey, findInMapIntrinsic.SecondLevelKey })
            {
                switch (property)
                {
                    case string s:

                        sb.Append($".{s}");
                        break;

                    case RefIntrinsic refIntrinsic:

                        sb.Append($"[{refIntrinsic.Render(template, resource)}]");
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

        private static Reference Render(
            GetAZsIntrinsic getAZsIntrinsic,
            ITemplate template,
            ImportedResource resource,
            int index)
        {
            // This is only going to work against the provider's region
            return new DataSourceReference("aws_availability_zones", "available", $"names[{index}]");
        }

        private static Reference Render(RefIntrinsic refIntrinsic, ITemplate template, ImportedResource resource, int index)
        {
            switch (refIntrinsic.Reference)
            {
                case string pseudo when pseudo.StartsWith("AWS::"):

                    if (PseudoParmeterToDataBlock.ContainsKey(pseudo))
                    {
                        return PseudoParmeterToDataBlock[pseudo];
                    }

                    throw new InvalidOperationException(
                        $"Pseudo parameter \"{pseudo}\" cannot be referenced by terraform.");

                default:

                    var param = template.Parameters.FirstOrDefault(p => p.Name == refIntrinsic.Reference);

                    if (param != null)
                    {
                        if (param.IsSsmParameter)
                        {
                            return new DataSourceReference("aws_ssm_parameter", refIntrinsic.Reference, "value");
                        }

                        return index < 0
                                   ? new InputVariableReference(refIntrinsic.Reference)
                                   : new InputVariableReference(refIntrinsic.Reference, index);
                    }

                    if (resource != null && template.Resources.Any(r => r.Name == resource.LogicalId))
                    {
                        return new DirectReference(resource.Address);
                    }

                    throw new InvalidOperationException($"Reference \"{refIntrinsic.Reference}\" cannot be resolved.");
            }
        }

        private static Reference Render(SelectIntrinsic selectIntrinsic, ITemplate template, ImportedResource resource)
        {
            if (selectIntrinsic.Items.Count == 1 && selectIntrinsic.Items[0] is IIntrinsic intrinsic)
            {
                switch (intrinsic)
                {
                    case RefIntrinsic refIntrinsic:

                        return Render(refIntrinsic, template, resource, selectIntrinsic.Index);

                    case GetAZsIntrinsic azsIntrinsic:

                        return Render(azsIntrinsic, template, resource, selectIntrinsic.Index);

                    case FindInMapIntrinsic findInMapIntrinsic:

                        return Render(findInMapIntrinsic, template, resource, selectIntrinsic.Index);
                }
            }

            return null;
        }

        public static Reference Render(GetAttIntrinsic getAttIntrinsic, ITemplate template, ImportedResource resource)
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

            var traits = ResourceTraitsCollection.Get(resource.TerraformType);

            attributeName = traits.AttributeMap.ContainsKey(attributeName)
                                ? traits.AttributeMap[attributeName]
                                : attributeName.CamelCaseToSnakeCase();

            if (template.Resources.Any(r => r.Name == resource.LogicalId))
            {
                return new IndirectReference($"{resource.Address}.{attributeName}");
            }

            return null;
        }
    }
}