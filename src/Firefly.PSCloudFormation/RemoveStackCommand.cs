namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// <para type="synopsis">Calls the AWS CloudFormation DeleteStack API operation.</para>
    /// <para type="description">
    /// Deletes a specified stack.
    /// If -Wait is present on the command line, then the call does not return until the stack has deleted.
    /// Stack events for this template and any nested stacks are output to the console.
    /// If -Wait is not present, the call returns immediately after stack deletion begins. You can check the status of the stack via the DescribeStacks API.
    /// </para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.BaseCloudFormationCommand" />
    [Cmdlet(VerbsCommon.Remove, "PSCFNStack1")]
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