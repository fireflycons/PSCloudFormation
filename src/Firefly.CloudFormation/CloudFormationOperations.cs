namespace Firefly.CloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// Contains methods for operations on stacks that are not related to applying templates
    /// </summary>
    public class CloudFormationOperations
    {
        /// <summary>
        /// The client factory
        /// </summary>
        private readonly IAwsClientFactory clientFactory;

        /// <summary>The context</summary>
        private readonly ICloudFormationContext context;

        /// <summary>Initializes a new instance of the <see cref="CloudFormationOperations"/> class.</summary>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="context">The context.</param>
        public CloudFormationOperations(IAwsClientFactory clientFactory, ICloudFormationContext context)
        {
            this.clientFactory = clientFactory;
            this.context = context;
        }

        /// <summary>Gets the stack outputs.</summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>Dictionary of output key-value pairs, or null if error</returns>
        public async Task<IDictionary<string, string>> GetStackOutputsAsync(string stackName)
        {
            try
            {
                return (await this.DescribeStackAsync(stackName)).Stacks
                    .First().Outputs.ToDictionary(o => o.OutputKey, o => o.OutputValue);
            }
            catch (Exception ex)
            {
                this.context.Logger.LogError($"Error retrieving stack outputs: {ex.Message}");
                throw;
            }
        }

        /// <summary>  Tests if given stack exists.</summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>true if the stack exists; else false</returns>
        public async Task<bool> StackExistsAsync(string stackName)
        {
            try
            {
                await this.DescribeStackAsync(stackName);
                return true;
            }
            catch (AmazonCloudFormationException ex)
            {
                // if its not an "expected" exception - log the exception
                if (!ex.Message.Contains($"Stack with id {stackName} does not exist"))
                {
                    this.context.Logger.LogWarning($"Error checking for stack: {ex.Message}");
                }

                return false;
            }
        }

        /// <summary>
        /// Tests if the given stack is ready, i.e. is not currently being updated
        /// </summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>A <see cref="StackOperationalState"/> indicating the current state of the stack</returns>
        public async Task<StackOperationalState> GetStackOperationalStateAsync(string stackName)
        {
            try
            {
                var status = (await this.DescribeStackAsync(stackName))
                    .Stacks.First().StackStatus;

                if (status == StackStatus.DELETE_COMPLETE)
                {
                    // If it's just been deleted, it is effectively not found
                    return StackOperationalState.NotFound;
                }

                if (status == StackStatus.DELETE_IN_PROGRESS)
                {
                    return StackOperationalState.Deleting;
                }

                if (status.Value.EndsWith("COMPLETE"))
                {
                    return StackOperationalState.Ready;
                }

                if (status == StackStatus.DELETE_FAILED)
                {
                    return StackOperationalState.DeleteFailed;
                }

                return status.Value.EndsWith("FAILED") ? StackOperationalState.Broken : StackOperationalState.Busy;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"Stack with id {stackName} does not exist"))
                {
                    return StackOperationalState.NotFound;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets a stack by name
        /// </summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>A <see cref="Stack"/> object.</returns>
        /// <exception cref="StackOperationException">Stack does not exist</exception>
        public async Task<Stack> GetStackAsync(string stackName)
        {
            try
            {
                return (await this.DescribeStackAsync(stackName)).Stacks.First();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"Stack with id {stackName} does not exist"))
                {
                    throw new StackOperationException(ex.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Describes the named stack
        /// </summary>
        /// <param name="stackName">Name of the stack.</param>
        /// <returns>A <see cref="DescribeStacksResponse"/></returns>
        private async Task<DescribeStacksResponse> DescribeStackAsync(string stackName)
        {
            using (var client = this.clientFactory.CreateCloudFormationClient())
            {
                return await client.DescribeStacksAsync(new DescribeStacksRequest { StackName = stackName });
            }
        }
    }
}