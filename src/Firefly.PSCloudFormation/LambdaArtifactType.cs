namespace Firefly.PSCloudFormation
{
    internal enum LambdaArtifactType
    {
        /// <summary>
        /// Resource passed to constructor was not a lambda
        /// </summary>
        NotLambda,

        /// <summary>
        /// Artifact is path to a code file e.g. index.py
        /// </summary>
        CodeFile,

        /// <summary>
        /// Artifact is path to a local zip file e.g. index.zip
        /// </summary>
        ZipFile,

        /// <summary>
        /// Artifact is path to a directory
        /// </summary>
        Directory,

        /// <summary>
        /// Artifact is inline code in the template
        /// </summary>
        Inline,

        /// <summary>
        /// Artifact is remote, i.e. refers to location in S3
        /// </summary>
        Remote
    }
}