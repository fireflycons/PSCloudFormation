// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1309

namespace Firefly.CloudFormation.CloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Utils;

    /// <summary>Builder pattern implementation for CloudFormationRunner</summary>
    public class CloudFormationBuilder
    {
        /// <summary>The CloudFormation context</summary>
        private readonly ICloudFormationContext _cloudFormationContext;

        /// <summary>The stack name</summary>
        private readonly string _stackName;

        /// <summary>
        /// Whether to only create change set.
        /// </summary>
        private bool _changesetOnly;

        /// <summary>
        /// The client factory
        /// </summary>
        private IAwsClientFactory _clientFactory;

        /// <summary>
        /// The client token
        /// </summary>
        private string _clientToken;

        /// <summary>Whether to delete a change set that results in no change.</summary>
        private bool _deleteNoopChangeset = true;

        /// <summary>
        /// Resource Imports
        /// </summary>
        private string _resourceImportsLocation;

        /// <summary>
        /// The notification ARNs
        /// </summary>
        private List<string> _notificationARNs = new List<string>();

        /// <summary>
        /// The role ARN
        /// </summary>
        private string _roleARN;

        /// <summary>The stack capabilities</summary>
        private IEnumerable<Capability> _capabilities = new List<Capability>();

        /// <summary>The stack parameters</summary>
        private IDictionary<string, string> _parameters;

        /// <summary>The tagging information</summary>
        private List<Tag> _tags = new List<Tag>();

        /// <summary>The template location. Either path o URL</summary>
        private string _templateLocation;

        /// <summary>Whether to wait if an operation is in progress</summary>
        private bool _waitForInProgressUpdate;

        /// <summary>
        /// <c>true</c> to use previous template for updates.
        /// </summary>
        private bool _usePreviousTemplate;

        /// <summary>
        /// The template resource types that you have permissions to work with for this action.
        /// </summary>
        private List<string> _resourceTypes = new List<string>();

        /// <summary>
        /// The rollback configuration
        /// </summary>
        private RollbackConfiguration _rollbackConfiguration;

        /// <summary>
        /// The termination protection
        /// </summary>
        private bool _terminationProtection;

        /// <summary>
        /// The stack policy
        /// </summary>
        private string _stackPolicy;

        /// <summary>
        /// The stack policy during update
        /// </summary>
        private string _stackPolicyDuringUpdate;

        /// <summary>
        /// The on failure action
        /// </summary>
        private OnFailure _onFailure;

        /// <summary>
        /// The timeout in minutes
        /// </summary>
        private int _timeoutInMinutes;

        /// <summary>
        /// The disable rollback
        /// </summary>
        private bool _disableRollback;

        /// <summary>
        /// The resources to retain on stack delete.
        /// </summary>
        private List<string> _resourcesToRetain;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationBuilder"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="stackName">Name of the stack.</param>
        public CloudFormationBuilder(ICloudFormationContext context, string stackName)
        {
            this._cloudFormationContext = context ?? throw new ArgumentNullException(nameof(context));
            this._stackName = stackName ?? throw new ArgumentNullException(nameof(stackName));

            if (this._cloudFormationContext.Logger == null)
            {
                throw new ArgumentNullException(nameof(context), "context.Logger cannot be null");
            }
        }

        /// <summary>Builds this instance.</summary>
        /// <returns>A new <see cref="CloudFormationRunner"/></returns>
        public CloudFormationRunner Build()
        {
            return new CloudFormationRunner(
                this._clientFactory ?? new DefaultClientFactory(this._cloudFormationContext),
                this._stackName,
                this._templateLocation,
                this._parameters,
                this._capabilities,
                this._cloudFormationContext,
                this._tags,
                this._waitForInProgressUpdate,
                this._deleteNoopChangeset,
                this._changesetOnly,
                this._resourceImportsLocation,
                this._roleARN,
                this._clientToken,
                this._notificationARNs,
                this._usePreviousTemplate,
                this._rollbackConfiguration,
                this._stackPolicy,
                this._stackPolicyDuringUpdate,
                this._resourceTypes,
                this._terminationProtection,
                this._onFailure,
                this._timeoutInMinutes,
                this._disableRollback,
                this._resourcesToRetain);
        }

        /// <summary>Add capabilities.</summary>
        /// <param name="capabilities">The capabilities.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithCapabilities(IEnumerable<Capability> capabilities)
        {
            this._capabilities = capabilities == null ? new List<Capability>() : capabilities.ToList();
            return this;
        }

        /// <summary>
        /// Set whether to create change set only without performing update.
        /// </summary>
        /// <param name="enable">If <c>true</c> (default), execute change set only.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithChangesetOnly(bool enable = true)
        {
            this._changesetOnly = enable;
            return this;
        }

        /// <summary>
        /// Adds a user supplied client factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithClientFactory(IAwsClientFactory factory)
        {
            this._clientFactory = factory;
            return this;
        }

        /// <summary>
        /// A unique identifier for this CreateChangeSet request. Specify this token if you plan to retry requests so that AWS CloudFormation knows that you're not attempting to create another change set with the same name. You might retry CreateChangeSet requests to ensure that AWS CloudFormation successfully received them.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The builder.</returns>
        public CloudFormationBuilder WithClientToken(string token)
        {
            this._clientToken = token;
            return this;
        }

        /// <summary>
        /// Set whether to auto-delete change sets that return no change.
        /// The default for this is <c>true</c>, so call this method with <c>false</c> to retain change set for future inspection.
        /// </summary>
        /// <param name="delete">if set to <c>true</c> [delete].</param>
        /// <returns>The builder</returns>
        // ReSharper disable once UnusedMember.Global
        public CloudFormationBuilder WithDeleteNoopChangeset(bool delete)
        {
            this._deleteNoopChangeset = delete;
            return this;
        }

        /// <summary>
        /// The Amazon Resource Names (ARNs) of Amazon Simple Notification Service (Amazon SNS) topics that AWS CloudFormation associates with the stack.
        /// To remove all associated notification topics, specify an empty list.
        /// </summary>
        /// <param name="notificationARNs">The notification ARNs.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithNotificationARNs(IEnumerable<string> notificationARNs)
        {
            this._notificationARNs = notificationARNs == null ? new List<string>() : notificationARNs.ToList();
            return this;
        }

        /// <summary>Sets the stack parameters.</summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithParameters(IDictionary<string, string> parameters)
        {
            this._parameters = parameters ?? new Dictionary<string, string>();
            return this;
        }

        /// <summary>
        /// Adds resources to import.
        /// </summary>
        /// <param name="resourceImportsLocation">Location of resource imports file (or string content).</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithResourceImports(string resourceImportsLocation)
        {
            this._resourceImportsLocation = resourceImportsLocation;
            return this;
        }

        /// <summary>
        /// The Amazon Resource Name (ARN) of an AWS Identity and Access Management (IAM) role that AWS CloudFormation assumes when executing the change set. AWS CloudFormation uses the role's credentials to make calls on your behalf. AWS CloudFormation uses this role for all future operations on the stack. As long as users have permission to operate on the stack, AWS CloudFormation uses this role even if the users don't have permission to pass it. Ensure that the role grants least privilege.
        /// If you don't specify a value, AWS CloudFormation uses the role that was previously associated with the stack. If no role is available, AWS CloudFormation uses a temporary session that is generated from your user credentials.
        /// </summary>
        /// <param name="arn">The ARN.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithRoleArn(string arn)
        {
            this._roleARN = arn;
            return this;
        }

        /// <summary>Adds stack-level tags.</summary>
        /// <param name="tags">The tags.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithTags(IEnumerable<Tag> tags)
        {
            this._tags = tags == null ? new List<Tag>() : tags.ToList();
            return this;
        }

        /// <summary>Adds the template location. Either a local path, or an HTTPS or S3 URI pointing to a template in S3.</summary>
        /// <param name="templateLocation">The template location.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithTemplateLocation(string templateLocation)
        {
            this._templateLocation = templateLocation;
            return this;
        }

        /// <summary>Sets whether to wait for in progress update.</summary>
        /// <param name="enable">If <c>true</c> (default), wait for stack action to complete, logging stack events to the given <see cref="ILogger"/> implementation.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithWaitForInProgressUpdate(bool enable = true)
        {
            this._waitForInProgressUpdate = enable;
            return this;
        }

        /// <summary>
        /// Whether to use existing template for stack update.
        /// </summary>
        /// <param name="enable">If <c>true</c> (default), use the previous template stored in CloudFormation to perform an update.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithUsePreviousTemplate(bool enable = true)
        {
            this._usePreviousTemplate = enable;
            return this;
        }

        /// <summary>
        /// Adds a rollback configuration.
        /// </summary>
        /// <param name="rollbackConfiguration">The rollback configuration.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithRollbackConfiguration(RollbackConfiguration rollbackConfiguration)
        {
            this._rollbackConfiguration = rollbackConfiguration;
            return this;
        }

        /// <summary>
        /// Sets a new stack policy.
        /// </summary>
        /// <param name="stackPolicy">The stack policy.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithStackPolicy(string stackPolicy)
        {
            this._stackPolicy = stackPolicy;
            return this;
        }

        /// <summary>
        /// Sets a temporary stack policy for use during an update.
        /// </summary>
        /// <param name="stackPolicy">The stack policy.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithStackPolicyDuringUpdate(string stackPolicy)
        {
            this._stackPolicyDuringUpdate = stackPolicy;
            return this;
        }

        /// <summary>
        /// Sets the template resource types that you have permissions to work with for this update stack action.
        /// </summary>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithResourceType(IEnumerable<string> resourceType)
        {
            this._resourceTypes = resourceType?.ToList();
            return this;
        }

        /// <summary>
        /// Enable termination protection (create only)
        /// </summary>
        /// <param name="enable">If <c>true</c> (default), set termination protection on the stack.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithTerminationProtection(bool enable)
        {
            this._terminationProtection = enable;
            return this;
        }

        /// <summary>
        /// Determines what action will be taken if stack creation fails.
        /// </summary>
        /// <param name="onFailure">The on failure.</param>
        /// <returns>The builder</returns>
        /// <exception cref="ArgumentException">Cannot set OnFailure when DisableRollback is true</exception>
        public CloudFormationBuilder WithOnFailure(OnFailure onFailure)
        {
            if (this._disableRollback)
            {
                throw new ArgumentException("Cannot set OnFailure when DisableRollback is true");
            }

            this._onFailure = onFailure;
            return this;
        }

        /// <summary>
        /// The amount of time that can pass before the stack status becomes CREATE_FAILED (Create only)
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithTimeoutInMinutes(int timeout)
        {
            this._timeoutInMinutes = timeout;
            return this;
        }

        /// <summary>
        /// Disables rollback on create failure.
        /// </summary>
        /// <returns>The builder</returns>
        /// <param name="enable">If <c>true</c> (default), disable rollback if stack create fails.</param>
        /// <exception cref="ArgumentException">Cannot set DisableRollback when OnFailure has been set</exception>
        public CloudFormationBuilder WithDisableRollback(bool enable)
        {
            this._disableRollback = enable;

            if (this._disableRollback && this._onFailure != null)
            {
                throw new ArgumentException("Cannot set DisableRollback when OnFailure has been set");
            }

            return this;
        }

        /// <summary>
        /// For stacks in the DELETE_FAILED state, a list of resource logical IDs that are associated with the resources you want to retain.
        /// </summary>
        /// <param name="retainResource">The resources to retain.</param>
        /// <returns>The builder</returns>
        public CloudFormationBuilder WithRetainResource(IEnumerable<string> retainResource)
        {
            this._resourcesToRetain = retainResource?.ToList();
            return this;
        }
    }
}