﻿namespace Firefly.PSCloudFormation.Commands
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.PSCloudFormation.AbstractCommands;

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
    /// Remove-PSCFNStack -StackName my-stack
    /// </code>
    /// <para>
    /// Deletes the specified stack
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// Remove-PSCFNStack -StackName my-stack -PassThru
    /// </code>
    /// <para>
    /// Deletes the specified stack. Don't wait for the deletion to complete.
    /// </para>
    /// </example>
    /// <example>
    /// <code>
    /// Remove-PSCFNStack -StackName my-stack -RetainResource my-bucket, my-security-group
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
    /// <seealso cref="BaseCloudFormationCommand" />
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
        /// Gets or sets the select.
        /// <para type="description">
        /// Use the -Select parameter to control the cmdlet output. The cmdlet doesn't have a return value by default.
        /// Specifying 'arn' will return the stack's ARN.
        /// Specifying 'result' will return the stack operation result.
        /// Specifying '*' will return a hash table containing a key for each of the above.
        /// Specifying -Select '^ParameterName' will result in the cmdlet returning the selected cmdlet parameter value. Note that not all parameters are available, e.g. credential parameters.
        /// </para>
        /// </summary>
        /// <value>
        /// The select.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public override string Select { get; set; }

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
                return await runner.DeleteStackAsync(this.AcceptDeleteWithNoRetainResource, this.AcceptDeleteStack);
            }
        }
    }
}