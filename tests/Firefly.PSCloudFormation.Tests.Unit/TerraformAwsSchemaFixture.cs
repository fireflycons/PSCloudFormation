namespace Firefly.PSCloudFormation.Tests.Unit
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    public class TerraformAwsSchemaFixture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformAwsSchemaFixture"/> class.
        /// </summary>
        public TerraformAwsSchemaFixture()
        {
            this.Schema = new AwsSchema();
        }

        /// <summary>
        /// Gets the EC2 instance schema which is fairly complex.
        /// </summary>
        /// <value>
        /// The EC2 instance schema.
        /// </value>
        internal ResourceSchema Ec2Instance => this.Schema.GetResourceSchema("aws_instance");

        /// <summary>
        /// Gets the Kinesis Analytics V2 Application schema which is deeply nested.
        /// </summary>
        /// <value>
        /// The kinesis anylitics v2 application.
        /// </value>
        internal ResourceSchema KinesisAnalyticsV2Application =>
            this.Schema.GetResourceSchema("aws_kinesisanalyticsv2_application");

        /// <summary>
        /// Gets the security group schema which has a block type that should be rendered as a list..
        /// </summary>
        /// <value>
        /// The security group.
        /// </value>
        internal ResourceSchema SecurityGroup => this.Schema.GetResourceSchema("aws_security_group");

        /// <summary>
        /// Gets the entire schema.
        /// </summary>
        /// <value>
        /// The schema.
        /// </value>
        internal AwsSchema Schema { get; }
    }
}