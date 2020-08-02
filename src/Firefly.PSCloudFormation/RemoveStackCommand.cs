namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// <para type="synopsis">Calls the AWS CloudFormation DeleteStack API operation.</para>
    /// <para type="description">
    /// Deletes a specified stack.
    /// The call does not return until the stack deletion has completed unless -PassThru is present,
    /// in which case it returns immediately and you can check the status of the stack via the DescribeStacks API
    /// Stack events for this template and any nested stacks are output to the console.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// Remove-PSFNStack -StackName my-stack
    /// </code>
    /// <para>
    /// Deletes the specified stack
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// Remove-PSFNStack -StackName my-stack -PassThru
    /// </code>
    /// <para>
    /// Deletes the specified stack. Don't wait for the deletion to complete.
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// Remove-PSFNStack -StackName my-stack -RetainResource my-bucket, my-security-group
    /// </code>
    /// <para>
    /// Deletes the specified stack, retaining the specified resources.
    /// </para>
    /// <para>
    /// Note that the listed resources will only be retained if, and only if the stack is in a <c>DELETE_FAILED</c> state
    /// and the listed resources are the cause of the failure. IF the stack is not <c>DELETE_FAILED</c>, you will be asked
    /// if you want to proceed with the delete and if you answer yes, then ALL resources will be deleted.
    /// </para>
    /// </example>
    /// <seealso cref="Firefly.PSCloudFormation.BaseCloudFormationCommand" />
    [Cmdlet(VerbsCommon.Remove, "PSCFNStack")]
    [OutputType(typeof(CloudFormationResult))]
    // ReSharper disable once UnusedMember.Global
    public class RemoveStackCommand : BaseCloudFormationCommand, IRemoveStackArguments
    {
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
        /// Gets the builder for <see cref="CloudFormationRunner" /> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>
        /// Builder for <see cref="CloudFormationRunner" />.
        /// </returns>
        protected override CloudFormationBuilder GetBuilder()
        {
            var builder = base.GetBuilder();

            return builder.WithRetainResource(this.RetainResource);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            using (var runner = this.GetBuilder().Build())
            {
                if (!this.Force && this.AskYesNo(
                        $"Delete {this.StackName} now?",
                        null,
                        ChoiceResponse.No,
                        "Delete now.",
                        "Cancel operation.") == ChoiceResponse.No)
                {
                    this.Logger.LogWarning($"Delete {this.StackName} cancelled");
                    return StackOperationResult.NoChange;
                }

                return await runner.DeleteStackAsync(this.AcceptDeleteWithNoRetainResource);
            }
        }
    }
}