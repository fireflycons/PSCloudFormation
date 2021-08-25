﻿namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Contains parameters common to all commands that work with CloudFormation stacks.
    /// </summary>
    public abstract class BaseCloudFormationCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// An empty object array used as second parameter to <see cref="PropertyInfo.GetValue(object, object[])"/>
        /// </summary>
        private static readonly object[] EmptyObjectArray = new object[0];

        /// <summary>
        /// Gets or sets the select delegate.
        /// </summary>
        /// <value>
        /// The select delegate.
        /// </value>
        protected Func<object> SelectDelegate { get; set; }

        /// <summary>
        /// Gets or sets the output object.
        /// </summary>
        /// <value>
        /// The output object.
        /// </value>
        protected object OutputObject { get; set; }

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
        [SuppressParameterSelect]
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
        [SuppressParameterSelect]
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
        /// Gets or sets the select.
        /// </summary>
        /// <value>
        /// The select.
        /// </value>
        public abstract string Select { get; set; }

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
        /// Gets the working directory for use by packaging.
        /// </summary>
        /// <value>
        /// The working directory.
        /// </value>
        internal WorkingDirectory WorkingDirectory { get; private set; }

        /// <summary>
        /// Gets or sets the stack's arn.
        /// </summary>
        /// <value>
        /// The arn.
        /// </value>
        [SelectableOutputProperty]
        protected string Arn { get; set; }

        /// <summary>
        /// Gets or sets the result of the CloudFormation run.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        [SelectableOutputProperty]
        protected CloudFormationResult Result { get; set; }

        /// <summary>
        /// Gets or sets the resolved changeset detail.
        /// </summary>
        /// <value>
        /// The resolved changeset detail.
        /// </value>
        protected string ResolvedChangesetDetail { get; set; }

        /// <summary>
        /// Gets or sets the resolved resources to import.
        /// </summary>
        /// <value>
        /// The resolved resources to import.
        /// </value>
        protected string ResolvedResourcesToImport { get; set; }

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
        /// Cancels the update task.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>0</c> if the task exited normally, <c>1</c> if it was cancelled by the token, or <c>2</c> if user aborted stack update</returns>
        protected virtual Task<object> CancelUpdateTask(CancellationToken cancellationToken)
        {
            // ReSharper disable once MethodSupportsCancellation
            return Task.Run(
                () => (object)0);
        }

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            const int StackOperationTask = 0;
            const int UpdateCancelTask = 1;

            if (this.ParameterWasBound(nameof(this.Select)))
            {
                this.SelectDelegate = this.CreateSelectDelegate(this.Select) ??
                            throw new ArgumentException("Invalid value for -Select parameter.", nameof(this.Select));
                
                if (this.PassThru.IsPresent)
                {
                    throw new ArgumentException("-PassThru cannot be used when -Select is specified.", nameof(this.Select));
                }
            }
            else if (this.PassThru.IsPresent)
            {
                this.SelectDelegate = () => this.Arn;
            }

            base.ProcessRecord();

            using (this.WorkingDirectory = new WorkingDirectory(this.Logger))
            {
                var tokenSource = new CancellationTokenSource();
                var tasks = new Task[] { this.OnProcessRecord(), this.CancelUpdateTask(tokenSource.Token) };
                var stackOperation = (Task<object>)tasks[StackOperationTask];

                try
                {
                    var completedTask = Task.WaitAny(tasks);

                    if (completedTask == StackOperationTask)
                    {
                        // Stack operation is now complete, so kill CancelUpdateTask if it is still running
                        if ((int)tasks[UpdateCancelTask].Status <= 4)
                        {
                            tokenSource.Cancel();
                        }
                    }
                    else
                    {
                        // Stack operation still running, wait it out.
                        // ReSharper disable once MethodSupportsCancellation
                        stackOperation.Wait();
                    }

                    // The Wait() wont throw, so check for contained exception and throw it so it's processed by ThrowExecutionError
                    if (stackOperation.IsFaulted)
                    {
                        var ex = stackOperation.Exception;

                        if (ex != null)
                        {
                            // ReSharper disable once PossibleNullReferenceException - by definition an AggregateException will contain an inner exception
                            throw ex.InnerException;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.ThrowExecutionError(ex.Message, this, ex);
                    return;
                }

                // Handle output according to -Select
                this.Result = (CloudFormationResult)stackOperation.Result;
                this.Arn = this.Result.StackArn;
                this.AfterOnProcessRecord();

                this.OutputObject = this.SelectDelegate?.Invoke();

                if (this.OutputObject != null)
                {
                    this.WriteObject(this.OutputObject);
                }
            }
        }

        /// <summary>
        /// Perform any post stack processing, such as retrieving values for select properties
        /// </summary>
        protected virtual void AfterOnProcessRecord()
        {
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

        /// <summary>
        /// Creates a delegate to retrieve output requested by -Select
        /// </summary>
        /// <param name="selectString">The select string.</param>
        /// <returns>Delegate to call at end of cmdlet processing to retrieve output value</returns>
        protected Func<object> CreateSelectDelegate(string selectString)
        {
            switch (selectString)
            {
                case null:
                case "":
                    return null;

                case "*":
                    return () =>
                        {
                            // Compile all visible output properties to hashtable
                            var result = new Hashtable();

                            foreach (var property in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                                p => p.GetCustomAttributes<SelectableOutputPropertyAttribute>().Any()))
                            {
                                result.Add(property.Name, property.GetValue(this, EmptyObjectArray));
                            }

                            return result;
                        };

                case var s when s.StartsWith("^"):
                    {
                        // An input parameter value is requested
                        var parameterName = selectString.Substring(1);

                        PropertyInfo selectedProperty = null;
                        foreach (var property in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(
                                p => p.GetCustomAttributes<ParameterAttribute>().Any()
                                     && !p.GetCustomAttributes<SuppressParameterSelectAttribute>().Any()
                                     && p.PropertyType != typeof(SwitchParameter)))
                        {
                            if (property.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                            {
                                selectedProperty = property;
                                break;
                            }

                            if (property.GetCustomAttributes<AliasAttribute>(false)
                                .SelectMany(attribute => attribute.AliasNames).Any(
                                    attributeAlias => attributeAlias.Equals(
                                        parameterName,
                                        StringComparison.OrdinalIgnoreCase)))
                            {
                                selectedProperty = property;
                            }

                            if (selectedProperty != null)
                            {
                                break;
                            }
                        }

                        var getter = selectedProperty?.GetGetMethod();
                        
                        if (getter == null)
                        {
                            return null;
                        }

                        return () => getter.Invoke(this, EmptyObjectArray);
                    }

                default:
                    {
                        // An output property is requested
                        var requestedOutputProperty = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                            .FirstOrDefault(
                                p => p.GetCustomAttributes<SelectableOutputPropertyAttribute>().Any()
                                     && string.Compare(p.Name, selectString, StringComparison.OrdinalIgnoreCase) == 0);

                        if (requestedOutputProperty == null)
                        {
                            return null;
                        }

                        var getter = requestedOutputProperty.GetGetMethod(true);
                        
                        if (getter == null)
                        {
                            return null;
                        }

                        return () => getter.Invoke(this, EmptyObjectArray);
                    }
            }
        }
    }
}