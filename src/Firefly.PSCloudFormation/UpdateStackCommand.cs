namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.CloudFormation;

    /// <summary>
    /// <para type="synpopsis">Calls the AWS CloudFormation UpdateStack API operation. </para>
    /// <para type="description">
    /// Calls the AWS CloudFormation UpdateStack API operation.
    /// A change set is first created and displayed to the user. Unless -Force is specified, the user may choose to continue or abandon at this stage.
    /// If -Wait is present on the command line, then the call does not return until the stack has updated.
    /// Stack events for this template and any nested stacks are output to the console.
    /// If -Wait is not present, the call returns immediately after stack update begins. You can check the status of the stack via the DescribeStacks API.
    /// </para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.StackParameterCloudFormationCommand" />
    [Cmdlet(VerbsData.Update, "PSCFNStack1")]
    public class UpdateStackCommand : StackParameterCloudFormationCommand
    {
        /// <summary>
        /// Gets or sets the stack policy during update location.
        /// <para type="description">
        /// Structure containing the temporary overriding stack policy body. For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
        /// If you want to update protected resources, specify a temporary overriding stack policy during this update. If you do not specify a stack policy, the current policy that is associated with the stack will be used.
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.
        /// </para>
        /// </summary>
        /// <value>
        /// The stack policy during update location.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("StackPolicyDuringUpdateBody", "StackPolicyDuringUpdateURL")]
        public string StackPolicyDuringUpdateLocation { get; set; }

        /// <summary>
        /// Gets or sets the use previous template.
        /// <para type="description">
        /// Reuse the existing template that is associated with the stack that you are updating.
        /// Conditional: You must specify only one of the following parameters: TemplateLocation or set the UsePreviousTemplate to true.
        /// </para>
        /// </summary>
        /// <value>
        /// The use previous template.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UsePreviousTemplate
        {
            get => this.UsePreviousTemplateFlag;
            set => this.UsePreviousTemplateFlag = value;
        }

        /// <summary>
        /// Gets or sets the resources to import.
        /// <para type="description">
        /// The resources to import into your stack.
        /// </para>
        /// <para type="description">
        /// If you created an AWS resource outside of AWS CloudFormation management, you can bring this existing resource into AWS CloudFormation management using resource import.
        /// You can manage your resources using AWS CloudFormation regardless of where they were created without having to delete and re-create them as part of a stack.
        /// Note that when performing an import, this is the only change that can happen to the stack. If any other resources ae changed, the changeset will fail to create.
        /// <para type="description">
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.
        /// </para>
        /// </para>
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ResourcesToImport { get; set; }

        /// <summary>
        /// Gets the builder for <see cref="CloudFormationRunner" /> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>
        /// Builder for <see cref="CloudFormationRunner" />.
        /// </returns>
        protected override CloudFormationBuilder GetBuilder()
        {
            return base.GetBuilder().WithStackPolicyDuringUpdate(this.StackPolicyDuringUpdateLocation)
                .WithUsePreviousTemplate(this.UsePreviousTemplateFlag);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            await base.OnProcessRecord();

            var runner = this.GetBuilder()
                .WithStackPolicyDuringUpdate(this.StackPolicyDuringUpdateLocation)
                .WithUsePreviousTemplate(this.UsePreviousTemplateFlag)
                .WithResourceImports(this.ResourcesToImport)
                .Build();

            return await runner.UpdateStackAsync(this.AcceptChangeset);
        }

        /// <summary>
        /// Callback method for Update Stack to allow user to accept the change set.
        /// </summary>
        /// <param name="changeset">The change set details.</param>
        /// <returns><c>true</c> if update should proceed; else <c>false</c></returns>
        private bool AcceptChangeset(DescribeChangeSetResponse changeset)
        {
            if (this.Force)
            {
                return true;
            }

            return this.AskYesNo(
                       $"Begin update of {this.StackName} now?",
                       null,
                       ChoiceResponse.Yes,
                       "Start rebuild now.",
                       "Cancel operation.") == ChoiceResponse.Yes;
        }
    }
}