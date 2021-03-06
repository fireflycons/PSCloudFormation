﻿namespace Firefly.PSCloudFormation.Commands
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;

    using Firefly.CloudFormation.Model;
    using Firefly.PSCloudFormation.AbstractCommands;

    /// <summary>
    /// <para type="synopsis">Deletes, then recreates a stack.</para>
    /// <para type="description">
    /// Deletes a specified stack, then recreates it from the supplied template.
    /// You may want to do this if a previous create attempt failed leaving the stack in ROLLBACK_COMPLETE, then you fix the template.
    /// The call does not return until the stack recreation has completed unless -PassThru is present,
    /// in which case it returns immediately after the creation of the new stack is initiated and you can check the status of the stack via the DescribeStacks API
    /// Stack events for this template and any nested stacks are output to the console.
    /// </para>
    /// <para>
    /// Valid arguments for this cmdlet are a combination of those for <c>Remove-PSCFNStack</c> and <c>New-PSCFNStack</c>.
    /// </para>
    /// <para type="link" uri="https://fireflycons.github.io/PSCloudFormation/articles/s3-usage.html">PSCloudFormation private S3 bucket</para>
    /// </summary>
    /// <example>
    /// <code>Reset-PSCFNStack -StackName "my-stack" -TemplateBody "{TEMPLATE CONTENT HERE}" -PK1 PV1 -PK2 PV2 -DisableRollback</code>
    /// <para>
    /// Deletes then creates a new stack with the specified name and follows the output until the operation completes.
    /// The template is parsed from the supplied content with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If creation of the stack fails, it will not be rolled back.
    /// </para>
    /// </example>
    /// <seealso cref="BaseCloudFormationCommand" />
    [Cmdlet(VerbsCommon.Reset, "PSCFNStack")]
    [OutputType(typeof(CloudFormationResult))]
    // ReSharper disable once UnusedMember.Global
    public class ResetStackCommand : StackOutputsCommand, IRemoveStackArguments, INewStackArguments
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
        [Parameter(ValueFromPipelineByPropertyName = true)]
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
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter EnableTerminationProtection { get; set; }

        /// <summary>
        /// Gets or sets the on failure.
        /// <para type="description">
        /// Determines what action will be taken if stack creation fails.
        /// This must be one of: DO_NOTHING, ROLLBACK, or DELETE. You can specify either <c>OnFailure</c> or <c>DisableRollback</c>, but not both.Default: <c>ROLLBACK</c></para>
        /// </summary>
        /// <value>
        /// The on failure.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
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
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int TimeoutInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the retain resource.
        /// <para type="description">
        /// For stacks in the <c>DELETE_FAILED</c> state, a list of resource logical IDs that are associated with the resources you want to retain.
        /// During deletion, AWS CloudFormation deletes the stack but does not delete the retained resources.
        /// Retaining resources is useful when you cannot delete a resource, such as a non-empty S3 bucket, but you want to delete the stack.
        /// </para>
        /// </summary>
        /// <value>
        /// The retain resource.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("RetainResources")]
        public string[] RetainResource { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected override StackOperation StackOperation { get; } = StackOperation.Create;

        /// <summary>
        /// Gets or sets the select.
        /// <para type="description">
        /// Use the -Select parameter to control the cmdlet output. The cmdlet doesn't have a return value by default.
        /// Specifying 'arn' will return the stack's ARN.
        /// Specifying 'result' will return the stack operation result.
        /// Specifying 'outputs' will return any stack outputs as a hashtable.
        /// Specifying '*' will return a hash table containing a key for each of the preceding named outputs.
        /// Specifying -Select '^ParameterName' will result in the cmdlet returning the selected cmdlet parameter value. Note that not all parameters are available, e.g. credential parameters.
        /// </para>
        /// </summary>
        /// <value>
        /// The select.
        /// </value>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public override string Select { get; set; }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            await base.OnProcessRecord();

            // We have to wait for the delete, irrespective of -Wait setting
            using (var runner = this.GetBuilder().WithRetainResource(this.RetainResource).WithFollowOperation()
                .Build())
            {
                var result = await runner.DeleteStackAsync(this.AcceptDeleteWithNoRetainResource, this.AcceptDeleteStack);

                if (result.StackOperationResult == StackOperationResult.NoChange)
                {
                    // Stop here as user cancelled delete.
                    return result;
                }
            }

            // Now recreate
            using (var runner = this.GetBuilder()
                .WithDisableRollback(this.DisableRollback)
                .WithOnFailure(this.OnFailure)
                .WithTerminationProtection(this.EnableTerminationProtection)
                .WithFollowOperation(!this.PassThru)
                .WithTimeoutInMinutes(this.TimeoutInMinutes)
                .Build())
            {
                return await runner.CreateStackAsync();
            }
        }
    }
}