namespace Firefly.CloudFormation.Model
{
    using System;

    /// <summary>
    /// Specifies location of an input for CloudFormation operations.
    /// </summary>
    [Flags]
    public enum InputFileSource
    {
        /// <summary>
        /// The location passed to the resolver was null or empty
        /// </summary>
        None = 0,

        /// <summary>
        /// Local file
        /// </summary>
        File = 1,

        /// <summary>
        /// Raw string
        /// </summary>
        String = 2,

        /// <summary>
        /// User supplied S3 location
        /// </summary>
        S3 = 4,

        /// <summary>
        /// Use previous template (applies to templates only)
        /// </summary>
        UsePreviousTemplate = 8,

        /// <summary>
        /// Local file or string, but needs to be uploaded first.
        /// </summary>
        Oversize = 16
    }
}