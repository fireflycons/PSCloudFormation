namespace Firefly.CloudFormation.S3
{
    /// <summary>
    /// Describes whether a file to be uploaded to S3 is a template or a policy.
    /// Used in the generation of S3 key names.
    /// </summary>
    public enum UploadFileType
    {
        /// <summary>
        /// A template file
        /// </summary>
        Template,

        /// <summary>
        /// A stack policy file
        /// </summary>
        Policy
    }
}