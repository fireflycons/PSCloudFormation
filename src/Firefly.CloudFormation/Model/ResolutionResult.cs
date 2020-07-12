namespace Firefly.CloudFormation.Model
{
    using Firefly.CloudFormation.Resolvers;

    /// <summary>
    /// <para>
    /// Object returned by <see cref="IInputFileResolver.ResolveArtifactLocationAsync"/>
    /// </para>
    /// <para>
    /// One property will always be set, and the other null, therefore it can be used like so
    /// <code>
    /// new CreateStackRequest {
    ///    TemplateBody = result.ArtifactBody,
    ///    TemplateURL = result.ArtifactUrl
    ///    ...
    /// };
    /// </code>
    /// </para>
    /// </summary>
    public class ResolutionResult
    {
        /// <summary>
        /// Gets or sets the content of a resolved template or policy to pass to CloudFormation.
        /// </summary>
        /// <value>
        /// The request body.
        /// </value>
        public string ArtifactBody { get; set; }

        /// <summary>
        /// Gets or sets the S3 URL of a resolved template or policy to pass to CloudFormation.
        /// </summary>
        /// <value>
        /// The request URL.
        /// </value>
        public string ArtifactUrl { get; set; }
    }
}
