namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;

    using Firefly.CloudFormation.CloudFormation;

    /// <summary>
    /// <para type="synopsis">Calls the AWS CloudFormation CreateStack API operation.</para>
    /// <para type="description">
    /// Creates a stack as specified in the template.
    /// If -Wait is present on the command line, then the call does not return until the stack has completed.
    /// Stack events for this template and any nested stacks are output to the console.
    /// If -Wait is not present, the call returns immediately after stack creation begins. You can check the status of the stack via the DescribeStacks API.
    /// </para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.StackParameterCloudFormationCommand" />
    [Cmdlet(VerbsCommon.New, "PSCFNStack1")]
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
                .WithTerminationProtection(this.EnableTerminationProtection);
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