namespace Firefly.PSCloudFormation.LambdaPackaging
{
    /// <summary>
    /// Describes the source of the lambda code
    /// </summary>
    internal enum LambdaArtifactType
    {
        /// <summary>
        /// Resource passed to constructor was not a lambda.
        /// </summary>
        NotLambda,

        /// <summary>
        /// Artifact is path to a single code file e.g. <c>my_lambda.py</c>
        /// </summary>
        CodeFile,

        /// <summary>
        /// Artifact is path to a local zip file e.g. <c>my_lambda_package.zip</c>, and will be uploaded as-is.
        /// </summary>
        ZipFile,

        /// <summary>
        /// Artifact is path to a directory which contains the handler file and most likely additional dependencies.
        /// </summary>
        Directory,

        /// <summary>
        /// Artifact is inline code in the template.
        /// </summary>
        Inline,

        /// <summary>
        /// Artifact refers to location in S3.
        /// </summary>
        FromS3
    }
}