namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormationParser;

    /// <summary>
    /// Settings for terraform exporter
    /// </summary>
    internal interface ITerraformExportSettings
    {
        /// <summary>
        /// Gets a value indicating whether to add the <c>terraform:stack_name</c> default tag to all resources.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [add default tag]; otherwise, <c>false</c>.
        /// </value>
        bool AddDefaultTag { get; }

        /// <summary>
        /// Gets the AWS account identifier.
        /// </summary>
        /// <value>
        /// The AWS account identifier.
        /// </value>
        string AwsAccountId { get; }

        /// <summary>
        /// Gets the AWS region.
        /// </summary>
        /// <value>
        /// The AWS region.
        /// </value>
        string AwsRegion { get; }

        /// <summary>
        /// Gets the cloud formation client.
        /// </summary>
        /// <value>
        /// The cloud formation client.
        /// </value>
        IAmazonCloudFormation CloudFormationClient { get; }

        /// <summary>
        /// Gets a value indicating whether to export nested stacks as Terraform modules.
        /// </summary>
        /// <value>
        ///   <c>true</c> to export modules; otherwise, <c>false</c>.
        /// </value>
        bool ExportNestedStacks { get; }

        /// <summary>
        /// Gets the resources.
        /// </summary>
        /// <value>
        /// The resources.
        /// </value>
        IReadOnlyCollection<CloudFormationResource> Resources { get; }

        /// <summary>
        /// Gets the runner that drives the Terraform binary.
        /// </summary>
        /// <value>
        /// The runner.
        /// </value>
        ITerraformRunner Runner { get; }

        /// <summary>
        /// Gets the list of stack exports for use when evaluating <c>Fn::Import</c>
        /// </summary>
        /// <value>
        /// The stack exports.
        /// </value>
        IReadOnlyCollection<Export> StackExports { get; }

        /// <summary>
        /// Gets the name of the stack being exported.
        /// </summary>
        /// <value>
        /// The name of the stack.
        /// </value>
        string StackName { get; }

        /// <summary>
        /// Gets the template as parsed by CloudFormation Parser.
        /// </summary>
        /// <value>
        /// The template.
        /// </value>
        ITemplate Template { get; }

        /// <summary>
        /// Gets the Terraform workspace directory where the generated code and state will be stored.
        /// </summary>
        /// <value>
        /// The workspace directory.
        /// </value>
        string WorkspaceDirectory { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        ILogger Logger { get; }
    }
}