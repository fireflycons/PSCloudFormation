namespace Firefly.PSCloudFormation.Terraform.Importers
{
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Type of dependency in a <see cref="ResourceDependency"/> object
    /// </summary>
    internal enum DependencyType
    {
        /// <summary>
        /// Dependency is a <see cref="ResourceImport"/>
        /// </summary>
        Resource,

        /// <summary>
        /// Dependency is the result of the evaluation of a property, e.g. a <c>Fn::Import</c> reference.
        /// </summary>
        Evaluation
    }
}