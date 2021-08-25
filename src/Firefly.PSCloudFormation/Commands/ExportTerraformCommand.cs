namespace Firefly.PSCloudFormation.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.AbstractCommands;
    using Firefly.PSCloudFormation.Terraform;

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
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.AbstractCommands.CloudFormationServiceCommand" />
    [Cmdlet(VerbsData.Export, "PSCFNTerraform")]
    // ReSharper disable once UnusedMember.Global - cmdlet interface
    public class ExportTerraformCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// The workspace directory
        /// </summary>
        private string workspaceDirectory;

        /// <summary>
        /// Gets or sets the name of the stack.
        /// <para type="description">
        /// Specifies the name of an existing CloudFormation Stack to import to Terraform.
        /// </para>
        /// </summary>
        /// <value>
        /// The name of the stack.
        /// </value>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string StackName { get; set; }

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
            }
        }

        /// <summary>
        /// Gets or sets the force.
        /// <para type="description">
        /// If set, automatically overwrite any existing state file.
        /// </para>
        /// </summary>
        /// <value>
        /// The force.
        /// </value>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets or sets the resolved workspace directory.
        /// </summary>
        /// <value>
        /// The resolved workspace directory.
        /// </value>
        private string ResolvedWorkspaceDirectory { get; set; }

        /// <summary>
        /// Exports to terraform.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="clientFactory">The client factory.</param>
        /// <returns>Task to wait on</returns>
        internal async Task<object> ExportTerraform(ICloudFormationContext context, IAwsClientFactory clientFactory)
        {
            this.Logger.LogInformation($"\nReading stack {this.StackName} from AWS...");
            var exporter = new TerraformExporter(
                await new CloudFormationOperations(clientFactory, context).GetStackResources(this.StackName),
                new TerrafomSettings
                    {
                        AwsRegion = this._RegionEndpoint.SystemName,
                        Runner = new TerraformRunner(this._CurrentCredentials, this.Logger),
                        WorkspaceDirectory = this.ResolvedWorkspaceDirectory
                    },
                this.Logger);

            exporter.Export();

            return null;
        }

        /// <summary>
        /// Process pipeline record
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
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
                            "Abort operation") == ChoiceResponse.Yes)
                    {
                        foreach (var file in new[] { state, stateBackup})
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var context = this.CreateCloudFormationContext();

            try
            {
                var dummy = this.ExportTerraform(
                        context,
                        new PSAwsClientFactory(
                            this.CreateClient(this._CurrentCredentials, this._RegionEndpoint),
                            context))
                    .Result;
            }
            catch (Exception e)
            {
                this.ThrowExecutionError(e.Message, this, e);
            }
        }
    }
}