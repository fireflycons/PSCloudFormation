namespace Firefly.PSCloudFormation.Commands
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// <para type="synopsis">Calls the AWS CloudFormation CreateStack API operation.</para>
    /// <para type="description">
    /// Creates a stack as specified in the template.
    /// The call does not return until the stack creation has completed unless -PassThru is present,
    /// in which case it returns immediately and you can check the status of the stack via the DescribeStacks API
    /// Stack events for this template and any nested stacks are output to the console.
    /// </para>
    /// <para type="link" uri="https://github.com/fireflycons/PSCloudFormation/blob/master/static/s3-usage.md">PSCloudFormation private S3 bucket</para>
    /// </summary>
    /// <example>
    /// <code>New-PSCFNStack -StackName "my-stack" -TemplateBody "{TEMPLATE CONTENT HERE}" -PK1 PV1 -PK2 PV2 -DisableRollback</code>
    /// <para>
    /// Creates a new stack with the specified name and follows the output until the operation completes.
    /// The template is parsed from the supplied content with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If creation of the stack fails, it will not be rolled back.
    /// </para>
    /// </example>
    /// <example>
    /// <code>New-PSCFNStack -StackName "my-stack" -TemplateLocation template.yaml -PK1 PV1 -PK2 PV2 -OnFailure "ROLLBACK"</code>
    /// <para>
    /// Creates a new stack with the specified name from template in given file and follows the output until the operation completes.
    /// If template is larger than 51,200 bytes it will be automatically uploaded to S3 first.
    /// The template is parsed from the given file with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If creation of the stack fails, it will be rolled back.
    /// </para>
    /// </example>
    /// <example>
    /// <code>New-PSCFNStack -StackName "my-stack" -TemplateURL s3://my-template-bucket/template.yaml -PK1 PV1 -PK2 PV2</code>
    /// <para>
    /// Creates a new stack with the specified name from template in S3 and follows the output until the operation completes.
    /// The template is obtained from the Amazon S3 URL with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If creation of the stack fails, it will be rolled back.
    /// </para>
    /// </example>
    /// <example>
    /// <code>New-PSCFNStack -StackName "my-stack" -TemplateURL https://my-template-bucket.s3.amazonaws.com/template.yaml -PK1 PV1 -PK2 PV2 -NotificationARN @( "arn1", "arn2" )</code>
    /// <para>
    /// Creates a new stack with the specified name from template in S3 and follows the output until the operation completes.
    /// The template is obtained from the Amazon S3 URL with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If creation of the stack fails, it will be rolled back. The specified notification ARNs will receive published stack-related events.
    /// </para>
    /// </example>
    /// <example>
    /// <code>New-PSCFNStack -StackName "my-stack" -TemplateURL https://my-template-bucket.s3.amazonaws.com/template.yaml -PK1 PV1 -PK2 PV2 -NotificationARN @( "arn1", "arn2" ) -PassThru</code>
    /// <para>
    /// As the above example, but the command will return immediately.
    /// </para>
    /// </example>
    /// <seealso cref="Firefly.PSCloudFormation.StackParameterCloudFormationCommand" />
    [Cmdlet(VerbsCommon.New, "PSCFNStack")]
    [OutputType(typeof(CloudFormationResult))]
    // ReSharper disable once UnusedMember.Global
    public class NewStackCommand : StackParameterCloudFormationCommand, INewStackArguments
    {
        /// <summary>
        /// Gets or sets the disable rollback.
        /// <para type="description">
        /// Set to <c>true</c> to disable rollback of the stack if stack creation failed. You can specify either DisableRollback or OnFailure, but not both.Default: <c>false</c>.
        /// </para>
        /// </summary>
        /// <value>
        /// The disable rollback.
        /// </value>
        [Parameter]
        public SwitchParameter DisableRollback { get; set; }

        /// <summary>
        /// Gets or sets the enable termination protection.
        /// <para type="description">
        /// Whether to enable termination protection on the specified stack.
        /// If a user attempts to delete a stack with termination protection enabled, the operation fails and the stack remains unchanged.
        /// Termination protection is disabled on stacks by default. For nested stacks, termination protection is set on the root stack and cannot be changed directly on the nested stack.
        /// </para>
        /// </summary>
        /// <value>
        /// The enable termination protection.
        /// </value>
        [Parameter]
        public SwitchParameter EnableTerminationProtection { get; set; }

        /// <summary>
        /// Gets or sets the on failure.
        /// <para type="description">
        /// Determines what action will be taken if stack creation fails.
        /// This must be one of: DO_NOTHING, ROLLBACK, or DELETE. You can specify either <c>OnFailure</c> or <c>DisableRollback</c>, but not both.Default: <c>ROLLBACK</c>
        /// </para>
        /// </summary>
        /// <value>
        /// The on failure.
        /// </value>
        [Parameter]
        public OnFailure OnFailure { get; set; }

        /// <summary>
        /// Gets or sets the timeout in minutes.
        /// <para type="description">
        /// The amount of time that can pass before the stack status becomes CREATE_FAILED; if <c>DisableRollback</c> is not set or is set to <c>false</c>, the stack will be rolled back.
        /// </para>
        /// </summary>
        /// <value>
        /// The timeout in minutes.
        /// </value>
        public int TimeoutInMinutes { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected override StackOperation StackOperation { get; } = StackOperation.Create;

        /// <summary>
        /// Gets the builder for <see cref="CloudFormationRunner" /> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>
        /// Builder for <see cref="CloudFormationRunner" />.
        /// </returns>
        protected override CloudFormationBuilder GetBuilder()
        {
            return base.GetBuilder()
                .WithDisableRollback(this.DisableRollback)
                .WithOnFailure(this.OnFailure)
                .WithTerminationProtection(this.EnableTerminationProtection)
                .WithTimeoutInMinutes(this.TimeoutInMinutes);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            await base.OnProcessRecord();

            using (var runner = this.GetBuilder().Build())
            {
                return await runner.CreateStackAsync();
            }
        }
    }
}