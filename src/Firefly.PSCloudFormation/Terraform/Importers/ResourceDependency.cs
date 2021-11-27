namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using Firefly.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Object returned by <see cref="ResourceImporter.GetResourceDependency()"/> or <see cref="ResourceImporter.GetResourceDependency(string)"/>
    /// </summary>
    internal class ResourceDependency
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDependency"/> class.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="referringTemplateObject">The parsed AWS template object relating to the resource being imported.</param>
        /// <param name="referencedTemplateObject">The parsed AWS template object relating to the referenced resource.</param>
        public ResourceDependency(ImportedResource resource, IResource referringTemplateObject, IResource referencedTemplateObject)
        {
            this.DependencyType = DependencyType.Resource;
            this.Resource = resource;
            this.ReferringTemplateObject = referringTemplateObject;
            this.ReferencedTemplateObject = referencedTemplateObject;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDependency"/> class.
        /// </summary>
        /// <param name="propertyEvaluation">The property evaluation.</param>
        public ResourceDependency(string propertyEvaluation)
        {
            this.DependencyType = DependencyType.Evaluation;
            this.PropertyPropertyEvaluation = propertyEvaluation;
        }

        /// <summary>
        /// Gets the type of the dependency.
        /// </summary>
        /// <value>
        /// The type of the dependency.
        /// </value>
        public DependencyType DependencyType { get; }

        /// <summary>
        /// Gets the property property evaluation.
        /// </summary>
        /// <value>
        /// The property property evaluation.
        /// </value>
        public string PropertyPropertyEvaluation { get; }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <value>
        /// The resource.
        /// </value>
        public ImportedResource Resource { get; }

        /// <summary>
        /// Gets the template object of the resource being referenced.
        /// </summary>
        /// <value>
        /// The template object.
        /// </value>
        public IResource ReferencedTemplateObject { get; }

        /// <summary>
        /// Gets the template object of the resource being imported.
        /// </summary>
        /// <value>
        /// The template object.
        /// </value>
        public IResource ReferringTemplateObject { get; }
    }
}