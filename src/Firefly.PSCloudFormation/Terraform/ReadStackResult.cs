﻿namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// Entity returned by <see cref="TerraformExporter.ReadStackAsync"/>
    /// </summary>
    internal class ReadStackResult
    {
        /// <summary>
        /// Gets or sets the account identifier.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        public IReadOnlyCollection<CloudFormationResource> Resources { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>
        /// The template.
        /// </value>
        public ITemplate Template { get; set; }
    }
}