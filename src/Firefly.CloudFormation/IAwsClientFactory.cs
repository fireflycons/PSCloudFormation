namespace Firefly.CloudFormation
{
    using Amazon.CloudFormation;

    /// <summary>
    /// Factory interface for the AWS clients used in this library
    /// </summary>
    public interface IAwsClientFactory
    {
        /// <summary>
        /// <para>
        /// Creates a CloudFormation client.
        /// </para>
        /// </summary>
        /// <returns>A CloudFormation client.</returns>
        IAmazonCloudFormation CreateCloudFormationClient();
    }
}