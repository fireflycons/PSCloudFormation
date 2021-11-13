namespace Firefly.PSCloudFormation.Terraform
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormationParser;

    /// <summary>
    /// Settings for terraform exporter
    /// </summary>
    internal interface ITerraformSettings
    {
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
        /// Gets a value indicating whether [non interactive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [non interactive]; otherwise, <c>false</c>.
        /// </value>
        bool NonInteractive { get; }

        /// <summary>
        /// Gets the physical resources as reported by the stack.
        /// </summary>
        /// <value>
        /// The physical resources.
        /// </value>
        IReadOnlyCollection<StackResource> PhysicalResources { get; }

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
    }
}