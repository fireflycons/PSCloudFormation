namespace Firefly.PSCloudFormation
{
    /// <summary>
    /// Supported runtimes for packaging dependencies
    /// </summary>
    internal enum LambdaRuntimeType
    {
        /// <summary>
        /// Python lambda
        /// </summary>
        Python,

        /// <summary>
        /// JavaScript lambda
        /// </summary>
        Node,

        /// <summary>
        /// Ruby lambda
        /// </summary>
        Ruby,

        /// <summary>
        /// Java lambda
        /// </summary>
        Java,

        /// <summary>
        /// Go lambda
        /// </summary>
        Go,

        /// <summary>
        /// .NET lambda
        /// </summary>
        DotNet,

        /// <summary>
        /// Custom runtime lambda
        /// </summary>
        Custom
    }
}