namespace Firefly.PSCloudFormation.Tests.Unit
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    public class TerraformAwsSchemaFixture
    {
        /// <summary>
        /// Gets the EC2 instance schema which is fairly complex.
        /// </summary>
        /// <value>
        /// The EC2 instance schema.
        /// </value>
        internal ResourceSchema Ec2Instance => AwsSchema.GetResourceSchema("aws_instance");

        /// <summary>
        /// Gets the Kinesis Analytics V2 Application schema which is deeply nested.
        /// </summary>
        /// <value>
        /// The kinesis anylitics v2 application.
        /// </value>
        internal ResourceSchema KinesisAnalyticsV2Application =>
            AwsSchema.GetResourceSchema("aws_kinesisanalyticsv2_application");

        /// <summary>
        /// Gets the security group schema which has a block type that should be rendered as a list..
        /// </summary>
        /// <value>
        /// The security group.
        /// </value>
        internal ResourceSchema SecurityGroup => AwsSchema.GetResourceSchema("aws_security_group");
    }
}