namespace Firefly.PSCloudFormation
{
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;

    /// <summary>
    /// <para type="synopsis">Calls the AWS CloudFormation UpdateStack API operation.</para>
    /// <para type="description">
    /// A change set is first created and displayed to the user. Unless -Force is specified, the user may choose to continue or abandon at this stage.
    /// The call does not return until the stack update has completed unless -PassThru is present,
    /// in which case it returns immediately and you can check the status of the stack via the DescribeStacks API
    /// Stack events for this template and any nested stacks are output to the console.
    /// </para>
    /// <para type="description">
    /// If -Wait is present, and a stack is found to be updating as a result of another process, this command will wait for that operation to complete
    /// following the stack events, prior to submitting the change set request.
    /// </para>
    /// <para type="link" uri="https://github.com/fireflycons/PSCloudFormation/blob/master/static/s3-usage.md">PSCloudFormation private S3 bucket</para>
    /// <para type="link" uri="https://github.com/fireflycons/PSCloudFormation/blob/master/static/resource-import.md">Resource Import (PSCloudFormation)</para>
    /// <para type="link" uri="(https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/resource-import.html)">Resource Import (AWS docs)</para>
    /// </summary>
    /// <example>
    /// <code>Update-PSCFNStack -StackName "my-stack" -TemplateBody "{TEMPLATE CONTENT HERE}" -PK1 PV1 -PK2 PV2</code>
    /// <para>
    /// Updates the stack my-stack and follows the output until the operation completes.
    /// The template is parsed from the supplied content with customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If update of the stack fails, it will be rolled back.
    /// </para>
    /// </example>
    /// <example>
    /// <code>Update-PSCFNStack -StackName "my-stack" -UsePreviousTemplate -PK1 PV1 -PK2 PV2</code>
    /// <para>
    /// Updates the stack my-stack and follows the output until the operation completes.
    /// The template currently associated with the stack is used and updated with new customization parameters ('PK1' and 'PK2' represent the names of parameters declared in the template content, 'PV1' and 'PV2' represent the values for those parameters.
    /// If update of the stack fails, it will be rolled back.
    /// </para>
    /// </example>
    /// <example>
    /// <code>Update-PSCFNStack -StackName "my-stack" -TemplateLocation ~/my-templates/template.json -ResourcesToImport ~/my-templates/import-resources.yaml</code>
    /// <para>
    /// Performs a resource import on the stack my-stack and follows the output until the operation completes.
    /// The template is parsed from the supplied content and resources to import are parsed from the supplied import file.
    /// Note that when importing resources, only <c>IMPORT</c> changes are permitted. Nothing else in the stack can be changed in the same operation.
    /// If update of the stack fails, it will be rolled back.
    /// </para>
    /// </example>
    /// <seealso cref="Firefly.PSCloudFormation.StackParameterCloudFormationCommand" />
    [Cmdlet(VerbsData.Update, "PSCFNStack")]
    [OutputType(typeof(CloudFormationResult))]
    // ReSharper disable once UnusedMember.Global
    public class UpdateStackCommand : StackParameterCloudFormationCommand
    {
        /// <summary>
        /// The stack policy during update location
        /// </summary>
        private string stackPolicyDuringUpdateLocation;

        /// <summary>
        /// The resources to import
        /// </summary>
        private string resourcesToImport;

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
        public string StackPolicyDuringUpdateLocation
        {
            get => this.stackPolicyDuringUpdateLocation;
            set => this.stackPolicyDuringUpdateLocation = this.ResolvePath(value);
        }

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
        // ReSharper disable once UnusedMember.Global - used via underlying property
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
        /// Note that when performing an import, this is the only change that can happen to the stack. If any other resources are changed, the changeset will fail to create.
        /// <para type="description">
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.
        /// </para>
        /// </para>
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ResourcesToImport
        {
            get => this.resourcesToImport; 
            set => this.resourcesToImport = this.ResolvePath(value);
        }

        /// <summary>
        /// Gets or sets the wait.
        /// <para type="description">
        /// If set, and the target stack is found to have an operation already in progress,
        /// then the command waits until that operation completes, printing out stack events as it goes.
        /// </para>
        /// </summary>
        /// <value>
        /// The wait.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected override StackOperation StackOperation { get; } = StackOperation.Update;

        /// <summary>
        /// Gets the builder for <see cref="CloudFormationRunner" /> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>
        /// Builder for <see cref="CloudFormationRunner" />.
        /// </returns>
        protected override CloudFormationBuilder GetBuilder()
        {
            return base.GetBuilder().WithStackPolicyDuringUpdate(this.StackPolicyDuringUpdateLocation)
                .WithUsePreviousTemplate(this.UsePreviousTemplateFlag).WithWaitForInProgressUpdate(this.Wait);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            this.Logger = new PSLogger(this);

            if (this.TemplateLocation == null && !this.UsePreviousTemplate)
            {
                throw new ArgumentException("Must supply one of -TemplateLocation, -UsePreviousTemplate");
            }

            await base.OnProcessRecord();

            using (var runner = this.GetBuilder()
                .WithStackPolicyDuringUpdate(this.StackPolicyDuringUpdateLocation)
                .WithUsePreviousTemplate(this.UsePreviousTemplateFlag)
                .WithResourceImports(this.ResourcesToImport)
                .Build())
            {
                return await runner.UpdateStackAsync(this.AcceptChangeset);
            }
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