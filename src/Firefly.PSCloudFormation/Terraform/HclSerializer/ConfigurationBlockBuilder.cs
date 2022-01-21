namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Text;

    /// <summary>
    /// Builds terraform and provider configuration blocks
    /// </summary>
    internal class ConfigurationBlockBuilder
    {
        /// <summary>
        /// The AWS region
        /// </summary>
        private string awsRegion;

        /// <summary>
        /// The stack name tag
        /// </summary>
        private string stackNameTag;

        /// <summary>
        /// True if <c>ArthurHlt/zipper</c> should be included
        /// </summary>
        private bool withZipper;

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns>HCL for terraform and provider configuration blocks.</returns>
        /// <exception cref="System.InvalidOperationException">Must set region for AWS provider</exception>
        public string Build()
        {
            if (this.awsRegion == null)
            {
                throw new InvalidOperationException("Must set region for AWS provider");
            }

            var block = new StringBuilder();

            block.AppendLine("terraform {")
                .AppendLine("  required_providers {")
                .AppendLine("    aws = {")
                .AppendLine("      source = \"hashicorp/aws\"")
                .AppendLine("    }");

            if (this.withZipper)
            {
                block.AppendLine("    zipper = {")
                    .AppendLine("      source = \"ArthurHlt/zipper\"")
                    .AppendLine("    }");
            }

            block.AppendLine("  }")
                .AppendLine("}")
                .AppendLine()
                .AppendLine("provider \"aws\" {")
                .AppendLine($"  region = \"{this.awsRegion}\"");

            if (this.stackNameTag != null)
            {
                block.AppendLine("  default_tags {")
                    .AppendLine("    tags = {")
                    .AppendLine($"      \"terraform:stack_name\" = \"{this.stackNameTag}\"")
                    .AppendLine("    }")
                    .AppendLine("  }");
            }

            block.AppendLine("}").AppendLine();

            return block.ToString();
        }

        /// <summary>
        /// Add a default tag to all AWS resources. Useful for creating a resource group.
        /// </summary>
        /// <param name="stackName">Name of the stack. Passing <c>null</c> will suppress default tag.</param>
        /// <returns><see langword="this" /></returns>
        public ConfigurationBlockBuilder WithDefaultTag(string stackName)
        {
            this.stackNameTag = stackName;
            return this;
        }

        /// <summary>
        /// Sets the region for the AWS provider (required)
        /// </summary>
        /// <param name="region">The region.</param>
        /// <returns><see langword="this" /></returns>
        public ConfigurationBlockBuilder WithRegion(string region)
        {
            this.awsRegion = region;
            return this;
        }

        /// <summary>
        /// Adds the zip file provider
        /// </summary>
        /// <param name="enabled">if set to <c>true</c> the zip provider is included..</param>
        /// <returns><see langword="this" /></returns>
        public ConfigurationBlockBuilder WithZipper(bool enabled = true)
        {
            this.withZipper = enabled;
            return this;
        }
    }
}