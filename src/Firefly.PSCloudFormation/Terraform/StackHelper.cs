namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser.Serialization.Settings;
    using Firefly.CloudFormationParser.TemplateObjects;

    /// <summary>
    /// Helper methods for exporting CloudFormation stacks to Terraform
    /// </summary>
    internal static class StackHelper
    {
        /// <summary>
        /// The stack identifier regex
        /// </summary>
        public static readonly Regex StackIdRegex = new Regex(
            @"^arn:(?<partition>[^:]+):cloudformation:(?<region>[^:]+):(?<account>\d+):stack/(?<stack>[^/]+)");

        /// <summary>
        /// Reads the stack asynchronous.
        /// </summary>
        /// <param name="cloudFormationClient">The cloud formation client.</param>
        /// <param name="stackName">Name of the stack.</param>
        /// <param name="parameterValues">The parameter values.</param>
        /// <returns>A <see cref="ReadStackResult"/> with the </returns>
        /// <exception cref="System.InvalidOperationException">Number of parsed resources does not match number of actual physical resources.</exception>
        public static async Task<ReadStackResult> ReadStackAsync(
            IAmazonCloudFormation cloudFormationClient,
            string stackName,
            IDictionary<string, object> parameterValues = null)
        {
            if (parameterValues == null)
            {
                parameterValues = new Dictionary<string, object>();
            }

            // Determine various pseudo-parameter values from the stack description
            var stack = (await cloudFormationClient.DescribeStacksAsync(
                             new DescribeStacksRequest { StackName = stackName })).Stacks.First();

            var match = StackIdRegex.Match(stack.StackId);

            parameterValues.Add("AWS::AccountId", match.Groups["account"].Value);
            parameterValues.Add("AWS::Region", match.Groups["region"].Value);
            parameterValues.Add("AWS::StackName", match.Groups["stack"].Value);
            parameterValues.Add("AWS::StackId", stack.StackId);
            parameterValues.Add("AWS::Partition", match.Groups["partition"].Value);
            parameterValues.Add("AWS::NotificationARNs", stack.NotificationARNs);

            // Get the parsed template
            var template = await Template.Deserialize(
                               new DeserializerSettingsBuilder()
                                   .WithCloudFormationStack(cloudFormationClient, stackName)
                                   .WithExcludeConditionalResources(true)
                                   .WithParameterValues(parameterValues)
                                   .Build());

            // Get the physical resources
            var resources =
                await cloudFormationClient.DescribeStackResourcesAsync(
                    new DescribeStackResourcesRequest { StackName = stackName });

            // This should be equivalent to what we read from the template
            var check = resources.StackResources
                .Select(r => r.LogicalResourceId)
                .OrderBy(lr => lr)
                .SequenceEqual(template.Resources
                    .Select(r => r.Name)
                    .OrderBy(lr => lr));

            if (!check)
            {
                throw new System.InvalidOperationException(
                    "Number of parsed resources does not match number of actual physical resources.");
            }

            // Combine physical stack resources and parsed template resources into single objects containing both.
            var combinedResources = resources.StackResources.Join(
                    template.Resources,
                    sr => sr.LogicalResourceId,
                    tr => tr.Name,
                    (stackResource, templateResource) => new CloudFormationResource(templateResource, stackResource))
                .ToList();

            return new ReadStackResult
                       {
                           AccountId = match.Groups["account"].Value,
                           Region = match.Groups["region"].Value,
                           Resources = combinedResources,
                           Template = template,
                           Outputs = stack.Outputs
                       };
        }
    }
}