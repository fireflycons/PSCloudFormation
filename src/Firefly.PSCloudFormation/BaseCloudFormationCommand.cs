﻿namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// Contains parameters common to all commands that work with CloudFormation stacks.
    /// </summary>
    public abstract class BaseCloudFormationCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// Gets or sets the client request token.
        /// <para type="description">
        /// A unique identifier for this CreateStack request. Specify this token if you plan to retry requests so that AWS CloudFormation knows that you're not attempting to create a stack with the same name.
        /// You might retry CreateStack requests to ensure that AWS CloudFormation successfully received them.
        /// All events triggered by a given stack operation are assigned the same client request token, which you can use to track operations.
        /// For example, if you execute a CreateStack operation with the token token1, then all the StackEvents generated by that operation will have ClientRequestToken set as token1.
        /// In the console, stack operations display the client request token on the Events tab. Stack operations that are initiated from the console use the token format Console-StackOperation-ID,
        /// which helps you easily identify the stack operation . For example, if you create a stack using the console, each stack event would be assigned the same token in the following format: <c>Console-CreateStack-7f59c3cf-00d2-40c7-b2ff-e75db0987002</c>.
        /// </para>
        /// </summary>
        /// <value>
        /// The client request token.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ClientRequestToken { get; set; }

        /// <summary>
        /// Gets or sets the force.
        /// <para type="description">
        /// This parameter overrides confirmation prompts to force the cmdlet to continue its operation. This parameter should always be used with caution.
        /// </para>
        /// </summary>
        /// <value>
        /// The force.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets or sets the pass thru.
        /// <para type="description">
        /// If this is set, then the operation returns immediately after submitting the request to CloudFormation.
        /// If not set, then the operation is followed to completion, with stack events being output to the console.
        /// </para>
        /// </summary>
        /// <value>
        /// The pass thru.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Gets or sets the role arn.
        /// <para type="description">
        /// The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes to create the stack.
        /// AWS CloudFormation uses the role's credentials to make calls on your behalf. AWS CloudFormation always uses this role for all future operations on the stack.
        /// As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it.
        /// Ensure that the role grants least privilege.If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack.
        /// If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.
        /// </para>
        /// </summary>
        /// <value>
        /// The role arn.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once StyleCop.SA1650
        public string RoleARN { get; set; }

        /// <summary>
        /// Gets or sets the name of the stack.
        /// <para type="description">
        /// The name that is associated with the stack. The name must be unique in the Region in which you are creating the stack.A stack name can contain only alphanumeric characters (case sensitive) and hyphens.
        /// It must start with an alphabetic character and cannot be longer than 128 characters.
        /// </para>
        /// </summary>
        /// <value>
        /// The name of the stack.
        /// </value>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        [ValidatePattern(@"[A-Za-z][A-Za-z0-9\-]{0,127}")]
        public string StackName { get; set; }

        /// <summary>
        /// Asks a yes/no question.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="message">The message.</param>
        /// <param name="defaultResponse">The default response.</param>
        /// <param name="helpYes">Help message for Yes response</param>
        /// <param name="helpNo">Help message for No response</param>
        /// <returns>User choice</returns>
        protected ChoiceResponse AskYesNo(
            string caption,
            string message,
            ChoiceResponse defaultResponse,
            string helpYes,
            string helpNo)
        {
            return (ChoiceResponse)this.Host.UI.PromptForChoice(
                caption,
                message,
                new Collection<ChoiceDescription>
                    {
                        new ChoiceDescription($"&{ChoiceResponse.Yes}", helpYes),
                        new ChoiceDescription($"&{ChoiceResponse.No}", helpNo)
                    },
                (int)defaultResponse);
        }

        /// <summary>
        /// Gets the builder for <see cref="CloudFormationRunner"/> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>Builder for <see cref="CloudFormationRunner"/>.</returns>
        protected virtual CloudFormationBuilder GetBuilder()
        {
            return CloudFormationRunner.Builder(this.CreateCloudFormationContext(), this.StackName)
                .WithClientToken(this.ClientRequestToken).WithRoleArn(this.RoleARN)
                .WithFollowOperation(!this.PassThru);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>Output to place into pipeline.</returns>
        protected abstract Task<object> OnProcessRecord();

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var task = this.OnProcessRecord();

            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                this.ThrowExecutionError(ex.Message, this, ex);
                return;
            }

            if (task.Result != null)
            {
                var stackResult = (CloudFormationResult)task.Result;
                this.WriteObject(stackResult);
            }
        }

        /// <summary>
        /// Callback method for delete stack if retain resources were given, but stack is not DELETE_FAILED
        /// </summary>
        /// <returns><c>true</c> if delete should proceed; else <c>false</c></returns>
        protected bool AcceptDeleteWithNoRetainResource()
        {
            return this.AskYesNo(
                       "Resources to retain were given, but stack is not DELETE_FAILED. All resources will be deleted.",
                       "Continue?",
                       ChoiceResponse.No,
                       "Continue with delete.",
                       "Cancel operation.") == ChoiceResponse.Yes;
        }

        /// <summary>
        /// Callback method for delete stack.
        /// </summary>
        /// <returns><c>true</c> if delete should proceed; else <c>false</c></returns>
        protected bool AcceptDeleteStack()
        {
            if (this.Force)
            {
                return true;
            }

            return this.AskYesNo(
                       $"Delete {this.StackName} now?",
                       null,
                       ChoiceResponse.No,
                       "Delete now.",
                       "Cancel operation.") == ChoiceResponse.Yes;
        }
    }
}