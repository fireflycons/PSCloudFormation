namespace Firefly.PSCloudFormation.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Runtime.InteropServices.ComTypes;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;
    using Amazon.Runtime;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormationParser.Serialization.Settings;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.PSCloudFormation.AbstractCommands;
    using Firefly.PSCloudFormation.Terraform;
    using Firefly.PSCloudFormation.Utils;

    using CPParameter = Firefly.CloudFormationParser.TemplateObjects.Parameter;

    /// <summary>
    /// <para type="synopsis">
    /// Export an existing CloudFormation stack to Terraform
    /// </para>
    /// <para type="description">
    /// Reads the CloudFormation stack and exports as many of its resources as possible to a Terraform workspace.
    /// This provides a starting position for migration of stacks to Terraform.
    /// </para>
    /// <para type="description">
    /// Once the resource ownership has been passed to Terraform, all the resources within the CloudFormation template should have
    /// their deletion policy set to Retain, then the CloudFormation stack deleted, thus leaving the resources intact.
    /// </para>
    /// <example>
    /// <code>Export-PSCFNTerraform -StackName my-stack -WorkspaceDirectory ~/tf/my-stack</code>
    /// <para>
    /// Reads my-stack from AWS via the CloudFormation service and generates Terraform code for that stack in the specified directory.
    /// </para>
    /// </example>
    /// <example>
    /// <code>Export-PSCFNTerraform -StackName my-stack -WorkspaceDirectory ~/tf/my-stack -NonInteractive</code>
    /// <para>
    /// As the first example, but does not ask questions about resources that cannot be imported directly. These resources are reported as not imported.
    /// </para>
    /// </example>
    /// <example>
    /// <code>Export-PSCFNTerraform -StackName my-stack -WorkspaceDirectory ~/tf/my-stack -Force</code>
    /// <para>
    /// As the first example, but if an existing state file is found in the workspace, it is overwritten without prompting.
    /// </para>
    /// </example>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.AbstractCommands.CloudFormationServiceCommand" />
    [Cmdlet(VerbsData.Export, "PSCFNTerraform")]

    // ReSharper disable once UnusedMember.Global - cmdlet interface
    public class ExportTerraformCommand : TemplateResolvingCloudFormationCommand
    {
        /// <summary>
        /// The stack identifier regex
        /// </summary>
        private static readonly Regex StackIdRegex = new Regex(@"^arn:(?<partition>\w+):cloudformation:(?<region>[^:]+):(?<account>\d+):stack/(?<stack>[^/]+)");

        /// <summary>
        /// The workspace directory
        /// </summary>
        private string workspaceDirectory;

        /// <summary>
        /// Gets or sets the non interactive.
        /// <para type="description">
        /// If set, do not ask the user questions.
        /// </para>
        /// <para type="description">
        /// Some resources such as lambda permissions cannot at this time directly be associated
        /// with the resources they refer to. In these cases a dialog with the user is initiated
        /// such that the user may select the appropriate resource e.g. lambda function to
        /// associate the resource being imported with. This switch disables that user interaction
        /// meaning that the resource will have to be resolved manually later.
        /// </para>
        /// </summary>
        /// <value>
        /// The non interactive.
        /// </value>
        [Parameter]
        public SwitchParameter NonInteractive { get; set; }

        /// <inheritdoc />
        public override string Select { get; set; }

        /// <summary>
        /// Gets or sets the workspace directory.
        /// <para type="description">
        /// Specifies the directory for the Terraform workspace. It will be created if it does not exist.
        /// </para>
        /// </summary>
        /// <value>
        /// The workspace directory.
        /// </value>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string WorkspaceDirectory
        {
            get => this.workspaceDirectory;

            set
            {
                this.workspaceDirectory = value;
                this.ResolvedWorkspaceDirectory = this.PathResolver.ResolvePath(value);

                // Set this here.
                // It needs to be set in order to read template from CFN
                // but cant be set in constructor or XmlDoc2CmdletDoc crashes
                this.UsePreviousTemplateFlag = true;
            }
        }

        /// <inheritdoc />
        protected override StackOperation StackOperation => StackOperation.Export;

        /// <inheritdoc />
        protected override TemplateStage TemplateStage => TemplateStage.Original;

        /// <summary>
        /// Gets or sets the resolved workspace directory.
        /// </summary>
        /// <value>
        /// The resolved workspace directory.
        /// </value>
        private string ResolvedWorkspaceDirectory { get; set; }

        /// <inheritdoc />
        protected override async Task<object> OnProcessRecord()
        {
            var state = Path.Combine(this.ResolvedWorkspaceDirectory, "terraform.tfstate");
            var stateBackup = Path.Combine(this.ResolvedWorkspaceDirectory, "terraform.tfstate.backup");
            
            if (File.Exists(state))
            {
                if (!this.Force)
                {
                    if (this.AskYesNo(
                            "Overwrite existing state?",
                            "Existing terraform state will be deleted.",
                            ChoiceResponse.No,
                            "Delete the state",
                            "Abort operation") == ChoiceResponse.No)
                    {
                        return new CloudFormationResult();
                    }
                }

                foreach (var file in new[] { state, stateBackup }.Where(File.Exists))
                {
                    File.Delete(file);
                }

            }
           
            var context = this.CreateCloudFormationContext();

            using (var client = CreateCloudFormationClient(context))
            {
                // Get any parameter values supplied by user.
                var userArgs = this.MyInvocation.BoundParameters.Where(bp => this.StackParameterNames.Contains(bp.Key))
                    .ToDictionary(bp => bp.Key, bp => bp.Value);

                // Determine various pseudo-parameter values from the stack description
                var stack =
                    (await client.DescribeStacksAsync(new DescribeStacksRequest { StackName = this.StackName })).Stacks
                    .First();

                var mc = StackIdRegex.Match(stack.StackId);

                userArgs.Add("AWS::AccountId", mc.Groups["account"].Value);
                userArgs.Add("AWS::Region", mc.Groups["region"].Value);
                userArgs.Add("AWS::StackName", mc.Groups["stack"].Value);
                userArgs.Add("AWS::StackId", stack.StackId);
                userArgs.Add("AWS::Partition", mc.Groups["partition"].Value);
                userArgs.Add("AWS::NotificationARNs", stack.NotificationARNs);

                var builder = new DeserializerSettingsBuilder().WithCloudFormationStack(client, this.StackName)
                    .WithExcludeConditionalResources(true)
                    .WithParameterValues(userArgs);

                // Get the template
                var dssettings = builder.Build();

                var template = await Template.Deserialize(dssettings);

                // Get the physical resources
                var resources =
                    await client.DescribeStackResourcesAsync(
                        new DescribeStackResourcesRequest { StackName = this.StackName });

                // This should be equivalent to what we read from the template
                var check = resources.StackResources.Select(r => r.LogicalResourceId).OrderBy(lr => lr)
                    .SequenceEqual(template.Resources.Select(r => r.Name).OrderBy(lr => lr));

                if (!check)
                {
                    throw new System.InvalidOperationException(
                        "Number of parsed resources does not match number of actual physical resources.");
                }

                var cr = resources.StackResources.OrderBy(sr => sr.LogicalResourceId).Zip(
                    template.Resources.OrderBy(tr => tr.Name),
                    (sr, tr) => new CloudFormationResource(tr, sr)).ToList();

                var settings = new TerrafomSettings
                                   {
                                       AwsAccountId = mc.Groups["account"].Value,
                                       AwsRegion = mc.Groups["region"].Value,
                                       NonInteractive = this.NonInteractive,
                                       Resources = cr,
                                       Runner = new TerraformRunner(context.Credentials, this.Logger),
                                       Template = template,
                                       WorkspaceDirectory = this.ResolvedWorkspaceDirectory
                                   };

                var exporter = new TerraformExporter(settings, this.Logger, new PSUserInterface(this.Host.UI));

                try
                {
                    exporter.Export();
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            return new CloudFormationResult();
        }

        /// <summary>
        /// Creates the cloud formation client.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Cloud formation client</returns>
        private static IAmazonCloudFormation CreateCloudFormationClient(IPSCloudFormationContext context)
        {
            var config = new AmazonCloudFormationConfig { RegionEndpoint = context.Region };

            if (context.CloudFormationEndpointUrl != null)
            {
                config.ServiceURL = context.CloudFormationEndpointUrl.AbsoluteUri;
            }

            return new AmazonCloudFormationClient(context.Credentials ?? new AnonymousAWSCredentials(), config);
        }
    }
}