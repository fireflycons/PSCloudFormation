namespace Firefly.PSCloudFormation.Commands
{
    using System;
    using System.Linq;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;
    using Amazon.S3.Model.Internal.MarshallTransformations;

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
    /// <para type="description">
    /// While a stack is in the UPDATE_IN_PROGRESS phase, pressing ESC 3 times in the space of a second will cancel the update forcing all modifications to roll back.
    /// Once the state transitions to UPDATE_COMPLETE_CLEANUP_IN_PROGRESS, the update can no longer be cancelled.
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
        /// Gets or sets the include nested stacks.
        /// <para type="description">
        /// Creates a change set for the all nested stacks specified in the template.
        /// </para>
        /// </summary>
        /// <value>
        /// The include nested stacks.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("IncludeNestedStack")]
        public SwitchParameter IncludeNestedStacks { get; set; }

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
            set => this.stackPolicyDuringUpdateLocation = this.PathResolver.ResolvePath(value);
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
            set => this.resourcesToImport = this.PathResolver.ResolvePath(value);
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
        /// Cancels the update task. ESC must be pressed 3 times within one second to initiate update stack cancellation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///   <c>0</c> if the task exited normally, <c>1</c> if it was cancelled by the token, or <c>2</c> if user aborted stack update
        /// </returns>
        protected override Task<object> CancelUpdateTask(CancellationToken cancellationToken)
        {
            // This is a bit weird. If this method is declared async, the task runs however the status remains WaitingForActivation 
            // and Task.WaitAny() blocks indefinitely waiting for this to go to Running.
            return Task.Run(
                () =>
                    {
                        if (!string.IsNullOrEmpty(this.ResourcesToImport))
                        {
                            // Docs say only UPDATE_IN_PROGRESS can be interrupted, so no need to run this thread.
                            return 0;
                        }
                            
                        var lastPress = new DateTime(1900, 1, 1);

                        var pressCount = 0;

                        while (true)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return (object)1;
                            }

                            if (Console.KeyAvailable)
                            {
                                var keyInfo = Console.ReadKey();
                                if (keyInfo.Key == ConsoleKey.Escape)
                                {
                                    Console.Error.WriteLine(" AESC");
                                    lastPress = DateTime.Now;
                                    ++pressCount;

                                    Beep(pressCount);

                                    if (pressCount == 3)
                                    {
                                        // Interrupt update
                                        using (var cfn = this._ClientFactory.CreateCloudFormationClient())
                                        {
                                            Stack stack;

                                            try
                                            {
                                                // If we really are in the middle of an update operation, the stack will exist and this block won't throw.
                                                // ReSharper disable once MethodSupportsCancellation
                                                stack = cfn.DescribeStacksAsync(
                                                        new DescribeStacksRequest { StackName = this.StackName }).Result
                                                    .Stacks.First();
                                            }
                                            catch
                                            {
                                                // ESC pressed before stack existence was verified.
                                                pressCount = 0;
                                                continue;
                                            }

                                            if (stack.StackStatus == StackStatus.UPDATE_IN_PROGRESS)
                                            {
                                                Console.Error.WriteLine("** Aborting Update **");

                                                // ReSharper disable once MethodSupportsCancellation
                                                var res = cfn.CancelUpdateStackAsync(
                                                    new CancelUpdateStackRequest { StackName = this.StackName }).Result;

                                                return 2;
                                            }

                                            if (stack.StackStatus.Value.Contains("CLEANUP"))
                                            {
                                                Console.Error.Write("\n** Too Late! Update phase is complete.\n");
                                                return 2;
                                            }

                                            if (stack.StackStatus.Value.Contains("ROLLBACK"))
                                            {
                                                Console.Error.Write("\n** Update already rolling back.\n");
                                                return 2;
                                            }

                                            // Stack is not yet updating, so reset press count.
                                            pressCount = 0;
                                        }
                                    }
                                }
                            }

                            Thread.Sleep(10);

                            if ((DateTime.Now - lastPress).TotalSeconds > 1)
                            {
                                pressCount = 0;
                            }
                        }
                    },
                cancellationToken);
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
                .WithIncludeNestedStacks(this.IncludeNestedStacks)
                .Build())
            {
                return await runner.UpdateStackAsync(this.AcceptChangeset);
            }
        }

        /// <summary>
        /// Beeps the console in line with specified press count.
        /// </summary>
        /// <param name="pressCount">The press count.</param>
        private static void Beep(int pressCount)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Console.Beep(400 * (int)Math.Pow(2, pressCount - 1), 100);
        }

        /// <summary>
        /// Callback method for Update Stack to allow user to accept the change set.
        /// </summary>
        /// <param name="changeset">The change set details.</param>
        /// <returns><c>true</c> if update should proceed; else <c>false</c></returns>
        private bool AcceptChangeset(DescribeChangeSetResponse changeset)
        {
            if (!this.Force)
            {
                if (this.AskYesNo(
                        $"Begin update of {this.StackName} now?",
                        null,
                        ChoiceResponse.Yes,
                        "Start rebuild now.",
                        "Cancel operation.") == ChoiceResponse.No)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(this.ResourcesToImport))
            {
                // Docs say only UPDATE_IN_PROGRESS can be interrupted.
                Console.WriteLine("Press ESC 3 times within one second to cancel update while update in progress");
            }

            return true;
        }
    }
}