[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReedExpo.Cake.AWS.CloudFormation.TestBase")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReedExpo.Cake.AWS.CloudFormation.Tests.Unit")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ReedExpo.Cake.AWS.CloudFormation.Tests.Integration")]

namespace Firefly.CloudFormation.CloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.CloudFormation.Template;
    using Firefly.CloudFormation.Utils;

    /// <summary>Class to manage all stack operations</summary>
    public partial class CloudFormationRunner : IDisposable
    {
        #region Fields

        /// <summary>Tag name to use for naming the stack</summary>
        private const string StackTagName = "Name";

        /// <summary>The stack capabilities</summary>
        private readonly IEnumerable<Capability> capabilities = new List<Capability>();

        /// <summary>if set to <c>true</c> only create change set without updating.</summary>
        private readonly bool changesetOnly;

        /// <summary>The CloudFormation client</summary>
        private readonly IAmazonCloudFormation client;

        /// <summary>
        /// The client factory
        /// </summary>
        private readonly IAwsClientFactory clientFactory;

        /// <summary>
        /// Client token 
        /// </summary>
        private readonly string clientToken;

        /// <summary>
        /// The context
        /// </summary>
        private readonly ICloudFormationContext context;

        /// <summary>Whether to delete a change set that results in no change.</summary>
        private readonly bool deleteNoopChangeSet;

        /// <summary>
        /// The disable rollback
        /// </summary>
        private readonly bool disableRollback;

        /// <summary>
        /// The imports
        /// </summary>
        private readonly string resourcesToImportLocation;

        /// <summary>If set to <c>true</c>, do not create default tags.</summary>
        private readonly bool noDefaultTags;

        /// <summary>
        /// SNS notification ARNs
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private readonly List<string> notificationARNs;

        /// <summary>
        /// The on failure
        /// </summary>
        private readonly OnFailure onFailure;

        /// <summary>The stack parameters</summary>
        private readonly IDictionary<string, string> parameters = new Dictionary<string, string>();

        /// <summary>
        /// The resource type
        /// </summary>
        private readonly List<string> resourceType;

        /// <summary>
        /// The retain resource
        /// </summary>
        private readonly List<string> retainResource;

        /// <summary>
        /// ARN of ole to assume during CF operations
        /// </summary>
        private readonly string roleArn;

        /// <summary>
        /// The rollback configuration
        /// </summary>
        private readonly RollbackConfiguration rollbackConfiguration;

        /// <summary>The stack name</summary>
        private readonly string stackName;

        /// <summary>
        /// The stack operations
        /// </summary>
        private readonly CloudFormationOperations stackOperations;

        /// <summary>
        /// The stack policy during update location
        /// </summary>
        private readonly string stackPolicyDuringUpdateLocation;

        /// <summary>
        /// The stack policy location
        /// </summary>
        private readonly string stackPolicyLocation;

        /// <summary>The tagging information</summary>
        private readonly List<Tag> tags;

        /// <summary>The template path</summary>
        private readonly string templateLocation;

        /// <summary>
        /// The termination protection
        /// </summary>
        private readonly bool terminationProtection;

        /// <summary>
        /// The timeout in minutes
        /// </summary>
        private readonly int timeoutInMinutes;

        /// <summary>
        /// The use previous template
        /// </summary>
        private readonly bool usePreviousTemplate;

        /// <summary>  How long to wait between polls for events</summary>
        private readonly int waitPollTime = 5000;

        /// <summary>
        /// The template resolver
        /// </summary>
        private IInputFileResolver templateResolver;

        /// <summary>The last event time</summary>
        private DateTime lastEventTime;

        /// <summary>
        /// The template description
        /// </summary>
        private string templateDescription;

        /// <summary>Whether to wait if an operation is in progress</summary>
        private bool waitForInProgressUpdate;

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="CloudFormationRunner"/> class.</summary>
        /// <param name="clientFactory">Factory for creating AWS service clients</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="templateLocation">Template location. Either path or URL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="capabilities">The capabilities.</param>
        /// <param name="context">The Cake context.</param>
        /// <param name="tags">Stack level tags.</param>
        /// <param name="waitForInProgressUpdate">if set to <c>true</c> [wait for in progress update].</param>
        /// <param name="deleteNoopChangeSet">if set to <c>true</c> [delete no-op change set].</param>
        /// <param name="changesetOnly">if set to <c>true</c> only create change set without updating.</param>
        /// <param name="noDefaultTags">If set to <c>true</c>, do not create default tags.</param>
        /// <param name="resourcesToImportLocation">Resources to import</param>
        /// <param name="roleArn">Role to assume</param>
        /// <param name="clientToken">Client token</param>
        /// <param name="notificationARNs">SNS notification ARNs</param>
        /// <param name="usePreviousTemplate">Whether to use existing template for update.</param>
        /// <param name="rollbackConfiguration">The rollback configuration</param>
        /// <param name="stackPolicyLocation">Location of structure containing a new stack policy body.</param>
        /// <param name="stackPolicyDuringUpdateLocation">Location of structure containing the temporary overriding stack policy body</param>
        /// <param name="resourceType">The template resource types that you have permissions to work with for this create stack action.</param>
        /// <param name="terminationProtection">Whether to enable termination protection on the specified stack.</param>
        /// <param name="onFailure">Determines what action will be taken if stack creation fails.</param>
        /// <param name="timeoutInMinutes">The amount of time that can pass before the stack status becomes CREATE_FAILED</param>
        /// <param name="disableRollback">Set to <c>true</c> to disable rollback of the stack if stack creation failed.</param>
        /// <param name="retainResource">For stacks in the DELETE_FAILED state, a list of resource logical IDs that are associated with the resources you want to retain.</param>
        /// <remarks>Constructor is private as this class implements the builder pattern. See CloudFormation.Runner.Builder.cs</remarks>
        internal CloudFormationRunner(
            IAwsClientFactory clientFactory,
            string stackName,
            string templateLocation,
            IDictionary<string, string> parameters,
            IEnumerable<Capability> capabilities,
            ICloudFormationContext context,
            List<Tag> tags,
            bool waitForInProgressUpdate,
            bool deleteNoopChangeSet,
            bool changesetOnly,
            bool noDefaultTags,
            string resourcesToImportLocation,
            string roleArn,
            string clientToken,
            // ReSharper disable once InconsistentNaming
            List<string> notificationARNs,
            bool usePreviousTemplate,
            RollbackConfiguration rollbackConfiguration,
            string stackPolicyLocation,
            string stackPolicyDuringUpdateLocation,
            List<string> resourceType,
            bool terminationProtection,
            OnFailure onFailure,
            int timeoutInMinutes,
            bool disableRollback,
            List<string> retainResource)
        {
            this.retainResource = retainResource;
            this.disableRollback = disableRollback;
            this.timeoutInMinutes = timeoutInMinutes;
            this.onFailure = onFailure;
            this.terminationProtection = terminationProtection;
            this.resourceType = resourceType;
            this.stackPolicyDuringUpdateLocation = stackPolicyDuringUpdateLocation;
            this.stackPolicyLocation = stackPolicyLocation;
            this.rollbackConfiguration = rollbackConfiguration;
            this.usePreviousTemplate = usePreviousTemplate;
            this.notificationARNs = notificationARNs;
            this.clientToken = clientToken;
            this.roleArn = roleArn;
            this.resourcesToImportLocation = resourcesToImportLocation;
            this.clientFactory = clientFactory;
            this.noDefaultTags = noDefaultTags;
            this.context = context;
            this.changesetOnly = changesetOnly;
            this.templateLocation = templateLocation;
            this.stackName = stackName ?? throw new ArgumentNullException(nameof(stackName));
            this.waitForInProgressUpdate = waitForInProgressUpdate;
            this.deleteNoopChangeSet = deleteNoopChangeSet;

            // Cheeky unit test detection
            if (context.GetType().FullName == "Castle.Proxies.ICloudFormationContextProxy")
            {
                // Don't hang around in the wait loop
                this.waitPollTime = 50;
            }

            if (capabilities != null)
            {
                this.capabilities = capabilities;
            }

            if (parameters != null)
            {
                this.parameters = parameters;
            }

            if (tags != null)
            {
                this.tags = tags;
            }

            this.client = this.clientFactory.CreateCloudFormationClient();
            this.stackOperations = new CloudFormationOperations(this.clientFactory, this.context);

            // Get parameters and description from supplied template if any
            this.templateResolver = new TemplateResolver(this.clientFactory, this.stackName, this.usePreviousTemplate);

            this.templateResolver.ResolveFileAsync(this.templateLocation).Wait();

            if (this.templateResolver.Source != InputFileSource.None)
            {
                var parser = TemplateParser.CreateParser(this.templateResolver.FileContent);

                this.templateDescription = parser.GetTemplateDescription();

                // Adds base stack name + 10 chars to each nested stack to estimate logical resource ID of each nested stack
                this.context.Logger.SetStackNameColumnWidth(
                    parser.GetNestedStackNames().Select(s => this.stackName + "-" + s.PadRight(10)).Concat(new[] { this.stackName })
                        .Max(s => s.Length));

                this.context.Logger.SetResourceNameColumnWidth(parser.GetLogicalResourceNames(this.stackName).Max(r => r.Length));
            }
        }

        #endregion

        /// <summary>
        /// Creates a builder instance for CloudFormationRunner
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>
        /// New builder instance
        /// </returns>
        public static CloudFormationBuilder Builder(ICloudFormationContext context, string stackName)
        {
            return new CloudFormationBuilder(context, stackName);
        }

        /// <summary>Creates a new stack.</summary>
        /// <returns><see cref="Task"/> object so we can await task return.</returns>
        public async Task<StackOperationResult> CreateStackAsync()
        {
            if (await this.stackOperations.StackExistsAsync(this.stackName))
            {
                throw new StackOperationException($"Stack {this.stackName} already exists");
            }

            var req = new CreateStackRequest
                          {
                              Capabilities = this.capabilities.Select(c => c.Value).ToList(),
                              ClientRequestToken = this.clientToken,
                              DisableRollback = this.disableRollback,
                              EnableTerminationProtection = this.terminationProtection,
                              NotificationARNs = this.notificationARNs,
                              OnFailure = this.onFailure,
                              Parameters =
                                  this.parameters.Select(
                                      p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList(),
                              ResourceTypes =
                                  this.resourceType != null && this.resourceType.Any() ? this.resourceType : null,
                              RollbackConfiguration = this.rollbackConfiguration,
                              RoleARN = this.roleArn,
                              StackName = this.stackName,
                              Tags = this.GetStackTags(),
                              TemplateBody = this.templateResolver.ArtifactContent,
                              TemplateURL = this.templateResolver.ArtifactUrl
                          };

            if (this.timeoutInMinutes > 0)
            {
                req.TimeoutInMinutes = this.timeoutInMinutes;
            }

            this.ResolveTemplateOrPolicyInput(
                new StackPolicyResolver(this.clientFactory),
                this.stackPolicyLocation,
                out var url,
                out var body);

            req.StackPolicyBody = body;
            req.StackPolicyURL = url;

            this.context.Logger.LogInformation($"Creating {this.GetStackNameWithDescription()}");

            var stackId = (await this.client.CreateStackAsync(req)).StackId;

            if (this.waitForInProgressUpdate)
            {
                await this.WaitStackOperationAsync(stackId, true);
                return StackOperationResult.StackCreated;
            }

            return StackOperationResult.StackCreateInProgress;
        }

        /// <summary>Deletes a stack.</summary>
        /// <returns>Operation result.</returns>
        public async Task<StackOperationResult> DeleteStackAsync()
        {
            var stack = await this.stackOperations.GetStackAsync(this.stackName);
            var operationalState = await this.stackOperations.GetStackOperationalStateAsync(stack.StackId);

            // Only permit delete if stack is in Ready state
            if (operationalState != StackOperationalState.Ready)
            {
                throw new StackOperationException(
                    stack,
                    $"Cannot delete stack: Current state: {operationalState} ({stack.StackStatus.Value})");
            }

            // Resolve template from CloudFormation to get description if any
            this.templateResolver = new TemplateResolver(this.clientFactory, this.stackName, true);
            await this.templateResolver.ResolveFileAsync(null);
            
            var parser = TemplateParser.CreateParser(this.templateResolver.FileContent);
            this.templateDescription = parser.GetTemplateDescription();
            
            // Adds base stack name + 10 chars to each nested stack to estimate logical resource ID of each nested stack
            this.context.Logger.SetStackNameColumnWidth(
                parser.GetNestedStackNames().Select(s => this.stackName + "-" + s.PadRight(10)).Concat(new[] { this.stackName })
                    .Max(s => s.Length));
            this.context.Logger.SetResourceNameColumnWidth(parser.GetLogicalResourceNames(this.stackName).Max(r => r.Length));

            this.context.Logger.LogInformation($"Deleting {this.GetStackNameWithDescription()}");

            this.lastEventTime = await this.GetMostRecentStackEvent(stack.StackId);

            await this.client.DeleteStackAsync(
                new DeleteStackRequest
                    {
                        StackName = stack.StackId,
                        ClientRequestToken = this.clientToken,
                        RoleARN = this.roleArn,
                        RetainResources = this.retainResource
                    });

            if (this.waitForInProgressUpdate)
            {
                await this.WaitStackOperationAsync(stack.StackId, true);
                return StackOperationResult.StackDeleted;
            }

            return StackOperationResult.StackDeleteInProgress;
        }

        /// <summary>
        /// Resets a stack by deleting and recreating it.
        /// </summary>
        /// <returns>Operation result.</returns>
        public async Task<StackOperationResult> ResetStackAsync()
        {
            var previousWaitSetting = this.waitForInProgressUpdate;

            // Must wait for delete, irrespective of wait setting
            this.waitForInProgressUpdate = true;

            try
            {
                // Time from which to start polling events
                this.lastEventTime = DateTime.Now;

                await this.DeleteStackAsync();
                this.waitForInProgressUpdate = previousWaitSetting;
                await this.CreateStackAsync();
                return this.waitForInProgressUpdate
                           ? StackOperationResult.StackReplaced
                           : StackOperationResult.StackCreateInProgress;
            }
            finally
            {
                this.waitForInProgressUpdate = previousWaitSetting;
            }
        }

        /// <summary>Updates a stack.</summary>
        /// <param name="confirmationFunc">A callback that should return <c>true</c> or <c>false</c> as to whether to continue with the stack update.</param>
        /// <exception cref="StackOperationException">Change set creation failed for reasons other than 'no change'</exception>
        /// <returns>Operation result.</returns>
        public async Task<StackOperationResult> UpdateStackAsync(Func<DescribeChangeSetResponse, bool> confirmationFunc)
        {
            var stack = await this.stackOperations.GetStackAsync(this.stackName);

            // Check stack state first
            var operationalState = await this.stackOperations.GetStackOperationalStateAsync(stack.StackId);

            switch (operationalState)
            {
                case StackOperationalState.Deleting:

                    throw new StackOperationException(stack, "Stack is being deleted by another process.");

                case StackOperationalState.Broken:

                    throw new StackOperationException(
                        stack,
                        $"Cannot update stack: Current state: {operationalState} ({stack.StackStatus.Value})");

                case StackOperationalState.Busy:

                    if (this.waitForInProgressUpdate)
                    {
                        // Track stack until update completes
                        // Wait for previous update to complete
                        // Time from which to start polling events
                        this.lastEventTime = DateTime.Now;

                        this.context.Logger.LogInformation(
                            "Stack {0} is currently being updated by another process",
                            stack.StackName);
                        this.context.Logger.LogInformation("Following its progress while waiting...");
                        stack = await this.WaitStackOperationAsync(stack.StackId, false);

                        if (await this.stackOperations.GetStackOperationalStateAsync(stack.StackId)
                            == StackOperationalState.Broken)
                        {
                            throw new StackOperationException(
                                stack,
                                $"Cannot update stack: Other process left stack in {stack.StackStatus.Value} state.");
                        }
                    }
                    else
                    {
                        throw new StackOperationException(stack, "Stack is being updated by another process.");
                    }

                    break;
            }

            // If we get here, stack is in Ready state
            var changeSetName = CreateChangeSetName();
            var stackParameters = this.GetStackParametersForUpdate(this.templateResolver, stack);
            var tagsToApply = this.GetStackTags();
            var resourcesToImport = await this.GetResourcesToImport();

            var changeSetRequest = new CreateChangeSetRequest
                                       {
                                           ChangeSetName = changeSetName,
                                           ChangeSetType = resourcesToImport != null ? ChangeSetType.IMPORT : ChangeSetType.UPDATE,
                                           Parameters = stackParameters,
                                           Capabilities = this.capabilities.Select(c => c.Value).ToList(),
                                           StackName = stack.StackId,
                                           ClientToken = this.clientToken,
                                           RoleARN = this.roleArn,
                                           NotificationARNs = this.notificationARNs,
                                           RollbackConfiguration = this.rollbackConfiguration,
                                           ResourceTypes = this.resourceType,
                                           Tags = tagsToApply,
                                           ResourcesToImport = resourcesToImport,
                                           TemplateBody = this.templateResolver.ArtifactContent,
                                           TemplateURL =
                                               this.usePreviousTemplate ? null : this.templateResolver.ArtifactUrl,
                                           UsePreviousTemplate = this.usePreviousTemplate,
                                       };

            this.context.Logger.LogInformation($"Creating changeset {changeSetName} for {this.GetStackNameWithDescription()}");

            var changesetArn = (await this.client.CreateChangeSetAsync(changeSetRequest)).Id;

            var stat = ChangeSetStatus.CREATE_PENDING;
            var describeChangeSetRequest = new DescribeChangeSetRequest { ChangeSetName = changesetArn };

            DescribeChangeSetResponse response = null;

            while (!(stat == ChangeSetStatus.CREATE_COMPLETE || stat == ChangeSetStatus.FAILED))
            {
                Thread.Sleep(this.waitPollTime / 2);
                response = await this.client.DescribeChangeSetAsync(describeChangeSetRequest);
                stat = response.Status;
            }

            if (stat == ChangeSetStatus.FAILED)
            {
                // ReSharper disable once PossibleNullReferenceException - we will go round the above loop at least once
                var reason = response.StatusReason;

                if (reason.StartsWith("The submitted information didn't contain changes")
                    || reason.StartsWith("No updates are to be performed"))
                {
                    this.context.Logger.LogInformation("No changes to stack were detected.");

                    if (this.deleteNoopChangeSet)
                    {
                        await this.client.DeleteChangeSetAsync(
                            new DeleteChangeSetRequest { ChangeSetName = changesetArn });

                        this.context.Logger.LogInformation($"Deleted changeset {changeSetName}");
                    }

                    return StackOperationResult.NoChange;
                }

                throw new StackOperationException($"Unable to create changeset: {reason}");
            }

            // If we get here, emit details, then apply the changeset.
            // ReSharper disable once PossibleNullReferenceException - we will go round the above loop at least once
            this.context.Logger.LogChangeset(response);

            if (this.changesetOnly)
            {
                this.context.Logger.LogInformation(
                    // ReSharper disable once PossibleNullReferenceException - 'response' cannot be null. DescribeChangeSetAsync has been called at least once to make it here.
                    $"Changeset {response.ChangeSetName} created for stack {stack.StackName}");
                this.context.Logger.LogInformation("Not updating stack since CreateChangesetOnly = true");
                return StackOperationResult.NoChange;
            }

            if (confirmationFunc != null)
            {
                // Confirm the changeset before proceeding
                if (!confirmationFunc(response))
                {
                    return StackOperationResult.NoChange;
                }
            }

            // Check nobody else has jumped in before us
            if (await this.stackOperations.GetStackOperationalStateAsync(stack.StackId) != StackOperationalState.Ready)
            {
                throw new StackOperationException(stack, "Stack is being modified by another process.");
            }

            // Time from which to start polling events
            this.lastEventTime = await this.GetMostRecentStackEvent(stack.StackId);

            this.context.Logger.LogInformation($"Updating {this.GetStackNameWithDescription()}");

            if (resourcesToImport != null)
            {
                // Have to do this by changeset
                await this.client.ExecuteChangeSetAsync(new ExecuteChangeSetRequest { ChangeSetName = changesetArn });
            }
            else
            {
                await this.client.UpdateStackAsync(this.GetUpdateRequestWithPolicyFromChangesetRequest(changeSetRequest));
            }

            if (this.waitForInProgressUpdate)
            {
                await this.WaitStackOperationAsync(stack.StackId, true);
                return StackOperationResult.StackUpdated;
            }

            return StackOperationResult.StackUpdateInProgress;
        }
    }
}