namespace Firefly.CloudFormation.Resolvers
{
    using System.Threading.Tasks;

    using Firefly.CloudFormation.Model;

    /// <summary>
    /// Interface that defines classes that will resolve a file that may be present in the local file system or referenced by URL
    /// </summary>
    public interface IInputFileResolver
    {
        /// <summary>
        /// Gets the artifact content. This will be <c>null</c> if the artifact is in S3.
        /// </summary>
        /// <value>
        /// The artifact content
        /// </value>
        string ArtifactContent { get; }

        /// <summary>
        /// Gets the file body - wherever it is located.
        /// </summary>
        /// <value>
        /// The file body.
        /// </value>
        string FileContent { get; }

        /// <summary>
        /// Gets the file URL.
        /// </summary>
        /// <value>
        /// The file URL. If not <c>null</c>, then this should be passed to CloudFormation
        /// </value>
        string ArtifactUrl { get; }

        /// <summary>
        /// Gets how the file should be sourced.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        InputFileSource Source { get; }

        /// <summary>
        /// Resolves and loads the given file from the specified location
        /// </summary>
        /// <param name="objectLocation">The file location.</param>
        /// <returns>The file content</returns>
        Task<string> ResolveFileAsync(string objectLocation);

        /// <summary>
        /// Resolves the given artifact location (template or policy) from text input
        /// uploading it to S3 if the object is larger than the maximum size for
        /// body text supported by the CloudFormation API.
        /// </summary>
        /// <param name="context">The context for logging.</param>
        /// <param name="objectToResolve">The object to resolve.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>Result of the resolution.</returns>
        Task<ResolutionResult> ResolveArtifactLocationAsync(
            ICloudFormationContext context,
            string objectToResolve,
            string stackName);
    }
}