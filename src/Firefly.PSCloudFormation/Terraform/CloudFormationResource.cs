namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Joins a resource parsed from a template with its physical CloudFormation counterpart
    /// </summary>
    internal class CloudFormationResource : IReferencedItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationResource"/> class.
        /// </summary>
        /// <param name="templateResource">The template resource.</param>
        /// <param name="stackResource">The stack resource.</param>
        public CloudFormationResource(IResource templateResource, StackResource stackResource)
        {
            this.TemplateResource = templateResource;
            this.StackResource = stackResource;
        }

        /// <inheritdoc />
        public IList<string> ListIdentity => new List<string> { this.PhysicalResourceId };

        /// <inheritdoc />
        public string ScalarIdentity => this.PhysicalResourceId;

        /// <inheritdoc />
        public bool IsScalar => true;

        /// <summary>
        /// Gets the logical resource identifier.
        /// </summary>
        /// <value>
        /// The logical resource identifier.
        /// </value>
        public string LogicalResourceId => this.StackResource.LogicalResourceId;

        /// <summary>
        /// Gets the physical resource identifier.
        /// </summary>
        /// <value>
        /// The physical resource identifier.
        /// </value>
        public string PhysicalResourceId => this.StackResource.PhysicalResourceId;

        /// <summary>
        /// Gets the type of the resource.
        /// </summary>
        /// <value>
        /// The type of the resource.
        /// </value>
        public string ResourceType => this.StackResource.ResourceType;

        /// <summary>
        /// Gets the physical stack resource.
        /// </summary>
        /// <value>
        /// The stack resource.
        /// </value>
        public StackResource StackResource { get; }

        /// <summary>
        /// Gets the parsed template resource.
        /// </summary>
        /// <value>
        /// The template resource.
        /// </value>
        public IResource TemplateResource { get; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{this.TemplateResource.Type} - {this.TemplateResource.Name}";
        }
    }
}