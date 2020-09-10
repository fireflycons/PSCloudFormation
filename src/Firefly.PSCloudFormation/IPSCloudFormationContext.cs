namespace Firefly.PSCloudFormation
{
    using Firefly.CloudFormation;

    /// <summary>
    /// Extends <see cref="ICloudFormationContext"/> to add custom endpoint support for S3 and STS
    /// </summary>
    // ReSharper disable StyleCop.SA1600
    // ReSharper disable InconsistentNaming
    public interface IPSCloudFormationContext : ICloudFormationContext
    {
        /// <summary>
        /// Gets or sets the custom S3 endpoint URL. If unset, then AWS default is used.
        /// </summary>
        /// <value>
        /// The s3 endpoint URL.
        /// </value>
        string S3EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the custom STS endpoint URL. If unset, then AWS default is used.
        /// </summary>
        /// <value>
        /// The STS endpoint URL.
        /// </value>
        // ReSharper disable once StyleCop.SA1600
        string STSEndpointUrl { get; set; }

        /// <summary>
        /// <para>
        /// Gets or sets the timestamp generator.
        /// </para>
        /// <para>
        /// Timestamp generator is used in the naming of oversize objects when uploaded to S3.
        /// It is provided on this interface purely to facilitate unit testing. You should leave
        /// this null.
        /// </para>
        /// </summary>
        /// <value>
        /// The timestamp generator.
        /// </value>
        ITimestampGenerator TimestampGenerator { get; set; }
    }
}