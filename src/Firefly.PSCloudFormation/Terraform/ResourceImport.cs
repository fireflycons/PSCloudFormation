﻿namespace Firefly.PSCloudFormation.Terraform
{
    using System.Diagnostics;

    /// <summary>
    /// Represents a mapping between an imported AWS resource and a Terraform resource
    /// </summary>
    [DebuggerDisplay("{Address}: {PhysicalId}")]
    internal class ResourceImport
    {
        /// <summary>
        /// Gets or sets the Terraform resource address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the AWS address (logical ID).
        /// </summary>
        /// <value>
        /// The AWS address.
        /// </value>
        public string AwsAddress { get; set; }

        /// <summary>
        /// Gets or sets the physical identifier.
        /// </summary>
        /// <value>
        /// The physical identifier.
        /// </value>
        public string PhysicalId { get; set; }
    }
}