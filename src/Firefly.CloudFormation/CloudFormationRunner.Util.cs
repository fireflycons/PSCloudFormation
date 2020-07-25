namespace Firefly.CloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Parsers;
    using Firefly.CloudFormation.Resolvers;
    using Firefly.CloudFormation.Utils;

    /// <summary>Class to manage all stack operations</summary>
    public partial class CloudFormationRunner
    {
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.client?.Dispose();
        }

        /// <summary>Creates the name of the change set.</summary>
        /// <returns>Name for change set</returns>
        private static string CreateChangeSetName()
        {
            // ReSharper disable once StringLiteralTypo
            return "fc-cloudformation-" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalMilliseconds;
        }

        /// <summary>
        /// Gets the most recent stack event.
        /// </summary>
        /// <param name="stackId">The stack identifier.</param>
        /// <returns>Timestamp of most recent event</returns>
        private async Task<DateTime> GetMostRecentStackEvent(string stackId)
        {
            string nextToken = null;
            var allEvents = new List<StackEvent>();

            // Get events for this stack
            do
            {
                var token = nextToken;
                var response = await PollyHelper.ExecuteWithPolly(
                                   () => this.client.DescribeStackEventsAsync(
                                       new DescribeStackEventsRequest { StackName = stackId, NextToken = token }));

                nextToken = response.NextToken;
                allEvents.AddRange(response.StackEvents);
            }
            while (nextToken != null);

            return allEvents.Any()
                       ? allEvents.OrderByDescending(e => e.Timestamp).First().Timestamp
                       : DateTime.MinValue;
        }

        /// <summary>
        /// Gets the stack name with description.
        /// </summary>
        /// <returns>Descriptive string for console output.</returns>
        private string GetStackNameWithDescription()
        {
            var sb = new StringBuilder().Append($"stack '{this.stackName}'");

            if (!string.IsNullOrEmpty(this.templateDescription))
            {
                sb.Append($" ({this.templateDescription})");
            }

            sb.AppendLine()
                .Append("Template source: ");

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (this.templateResolver.Source & ~InputFileSource.Oversize)
            {
                case InputFileSource.UsePreviousTemplate:

                    sb.Append("Template associated with existing stack");
                    break;

                case InputFileSource.String:

                    sb.Append("Input String");
                    break;

                default:

                    sb.Append(this.templateLocation);
                    break;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the stack parameters for update.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="stack">The stack.</param>
        /// <returns>List of <see cref="Parameter"/></returns>
        private List<Parameter> GetStackParametersForUpdate(IInputFileResolver resolver, Stack stack)
        {
            // Sort out the parameters
            var stackParameters = new List<Parameter>();

            // First, get the parameter names that are declared in the stack we are updating.
            var declaredParameterKeys = TemplateParser.Create(resolver.FileContent).GetParameters().ToList();

            // We can only pass in parameters for keys declared in the template we are pushing
            foreach (var parameter in declaredParameterKeys)
            {
                // Where we have a user-supplied parameter that matches the declared keys, add it
                if (this.parameters.ContainsKey(parameter.Name))
                {
                    stackParameters.Add(
                        new Parameter
                            {
                                ParameterKey = parameter.Name, ParameterValue = this.parameters[parameter.Name]
                            });
                }
                else if (stack.Parameters.Any(p => p.ParameterKey == parameter.Name))
                {
                    // Stack still has the declared parameter, but user hasn't supplied a new value,
                    // then it's use previous value.
                    stackParameters.Add(new Parameter { ParameterKey = parameter.Name, UsePreviousValue = true });
                }
            }

            var extraParameters = this.parameters.Keys.Where(k => declaredParameterKeys.All(p => p.Name != k)).ToList();

            if (extraParameters.Any())
            {
                this.context.Logger.LogWarning(
                    "The following parameters were supplied which are not required by the template you are applying:\n  "
                    + string.Join(", ", extraParameters));
            }

            return stackParameters;
        }

        /// <summary>Waits for a stack operation. to compete, sending stack event messages to the log.</summary>
        /// <param name="stackArn">  ARN or name of stack to wait on.</param>
        /// <param name="throwOnFailure">if set to <c>true</c> [throw on failure].</param>
        /// <returns>The stack being waited on.</returns>
        private async Task<Stack> WaitStackOperationAsync(string stackArn, bool throwOnFailure)
        {
            var describeStacksRequest = new DescribeStacksRequest { StackName = stackArn };

            while (true)
            {
                Thread.Sleep(this.waitPollTime);
                var stack = (await PollyHelper.ExecuteWithPolly(
                                 () => this.client.DescribeStacksAsync(describeStacksRequest))).Stacks.First();

                // Have we finished?
                var isComplete = stack.StackStatus.Value.EndsWith("COMPLETE")
                                 || stack.StackStatus.Value.EndsWith("FAILED");

                // Get events and render them
                var events = (await GetEventsAsync(stackArn)).OrderBy(e => e.Timestamp).ToList();

                if (events.Any())
                {
                    // Most recent event is last
                    this.lastEventTime = events.Last().Timestamp;

                    // Now output them oldest first
                    foreach (var evt in events.OrderBy(e => e.Timestamp))
                    {
                        this.context.Logger.LogStackEvent(evt);
                    }
                }

                if (isComplete)
                {
                    // Operation finished
                    if (Regex.IsMatch(stack.StackStatus.Value, "(ROLLBACK|FAILED)") && throwOnFailure)
                    {
                        throw new StackOperationException(stack);
                    }

                    // We done
                    return stack;
                }
            }

            // Local recursive function to collect events from nested stacks
            async Task<IEnumerable<StackEvent>> GetEventsAsync(string stackArnLocal)
            {
                string nextToken = null;
                var allEvents = new List<StackEvent>();

                // Get events for this stack
                do
                {
                    var token = nextToken;
                    var response = await PollyHelper.ExecuteWithPolly(
                                       () => this.client.DescribeStackEventsAsync(
                                           new DescribeStackEventsRequest
                                               {
                                                   StackName = stackArnLocal, NextToken = token
                                               }));

                    nextToken = response.NextToken;
                    allEvents.AddRange(response.StackEvents.Where(e => e.Timestamp > this.lastEventTime));
                }
                while (nextToken != null);

                // Enumerate any nested stack resources and recurse into each
                foreach (var nested in (await PollyHelper.ExecuteWithPolly(
                                            () => this.client.DescribeStackResourcesAsync(
                                                new DescribeStackResourcesRequest { StackName = stackArnLocal })))
                    .StackResources.Where(r => r.ResourceType == "AWS::CloudFormation::Stack"))
                {
                    allEvents.AddRange(await GetEventsAsync(nested.PhysicalResourceId));
                }

                return allEvents;
            }
        }

        /// <summary>
        /// Gets the resources to import.
        /// </summary>
        /// <returns>List of <see cref="ResourceToImport"/> or null if no input</returns>
        private async Task<List<ResourceToImport>> GetResourcesToImportAsync()
        {
            if (string.IsNullOrEmpty(this.resourcesToImportLocation))
            {
                return null;
            }

            // TemplateResolver is good enough for our purposes here
            return ResourceImportParser.Create(
                await new TemplateResolver(this.clientFactory, this.context, this.stackName, false)
                    .ResolveFileAsync(this.resourcesToImportLocation))
                .GetResourcesToImport();
        }

        /// <summary>
        /// Gets the update request with policy from change set request.
        /// </summary>
        /// <param name="changeSetRequest">The change set request.</param>
        /// <returns>New <see cref="UpdateStackRequest"/></returns>
        private async Task<UpdateStackRequest> GetUpdateRequestWithPolicyFromChangesetRequestAsync(CreateChangeSetRequest changeSetRequest)
        {
            var policyResolver = new StackPolicyResolver(this.clientFactory, this.context);

            var policy = await policyResolver.ResolveArtifactLocationAsync(
                             this.context,
                             this.stackPolicyLocation,
                             this.stackName);

            var updatePolicy = await policyResolver.ResolveArtifactLocationAsync(
                                   this.context,
                                   this.stackPolicyDuringUpdateLocation,
                                   this.stackName);

            return new UpdateStackRequest
                                    {
                                        Parameters = changeSetRequest.Parameters,
                                        Capabilities = changeSetRequest.Capabilities,
                                        StackName = changeSetRequest.StackName,
                                        ClientRequestToken = changeSetRequest.ClientToken,
                                        RoleARN = changeSetRequest.RoleARN,
                                        NotificationARNs = changeSetRequest.NotificationARNs,
                                        RollbackConfiguration = changeSetRequest.RollbackConfiguration,
                                        ResourceTypes = changeSetRequest.ResourceTypes,
                                        StackPolicyBody = policy.ArtifactBody,
                                        StackPolicyURL = policy.ArtifactUrl,
                                        StackPolicyDuringUpdateBody = updatePolicy.ArtifactBody,
                                        StackPolicyDuringUpdateURL = updatePolicy.ArtifactUrl,
                                        Tags = changeSetRequest.Tags,
                                        TemplateBody = changeSetRequest.TemplateBody,
                                        TemplateURL = changeSetRequest.TemplateURL,
                                        UsePreviousTemplate = changeSetRequest.UsePreviousTemplate
            };
        }
    }
}