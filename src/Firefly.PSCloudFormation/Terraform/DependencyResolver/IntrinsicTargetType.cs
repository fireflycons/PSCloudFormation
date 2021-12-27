namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    /// <summary>
    /// Specifies the target of an intrinsic
    /// </summary>
    internal enum IntrinsicTargetType
    {
        /// <summary>
        /// The target is as yet unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// An input variable to the current module.
        /// </summary>
        Input,

        /// <summary>
        /// An output of a module imported by the current module.
        /// </summary>
        Output,

        /// <summary>
        /// Another resource within the current module.
        /// </summary>
        Resource,

        /// <summary>
        /// Another module imported to the current module.
        /// </summary>
        Module
    }
}