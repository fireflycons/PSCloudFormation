namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// <para>
    /// Base class for Create/Update operations
    /// </para>
    /// <para>
    /// This class provides all the common cmdlet arguments for commands that create or change stacks,
    /// and is the point where PowerShell dynamic parameters are introduced.
    /// Dynamic parameters are created for each CloudFormation parameter found in the <c>Parameters</c>
    /// section of the given CloudFormation template, with the exception of SSM parameter types which
    /// are not user-supplied.
    /// </para>
    /// </summary>
    /// <seealso cref="BaseCloudFormationCommand" />
    /// <seealso cref="System.Management.Automation.IDynamicParameters" />
    public abstract class StackParameterCloudFormationCommand : TemplateResolvingCloudFormationCommand
    {
        /// <summary>
        /// The stack policy location
        /// </summary>
        private string stackPolicyLocation;

        /// <summary>
        /// The template location
        /// </summary>
        private string templateLocation;

        /// <summary>
        /// Gets or sets the capabilities.
        /// <para type="description">
        /// In some cases, you must explicitly acknowledge that your stack template contains certain capabilities in order for AWS CloudFormation to create the stack.
        /// <list type="bullet">
        /// <item>
        /// <term>CAPABILITY_IAM and CAPABILITY_NAMED_IAM</term>
        /// <description>
        /// Some stack templates might include resources that can affect permissions in your AWS account; for example, by creating new AWS Identity and Access Management (IAM) users.
        /// For those stacks, you must explicitly acknowledge this by specifying one of these capabilities.
        /// </description>
        /// </item>
        /// <item>
        /// <term>CAPABILITY_AUTO_EXPAND</term>
        /// <description>
        /// Some template contain macros. Macros perform custom processing on templates; this can include simple actions like find-and-replace operations,
        /// all the way to extensive transformations of entire templates.
        /// Because of this, users typically create a change set from the processed template,
        /// so that they can review the changes resulting from the macros before actually creating the stack.
        /// If your stack template contains one or more macros, and you choose to create a stack directly from the processed template,
        /// without first reviewing the resulting changes in a change set, you must acknowledge this capability.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <value>
        /// The capabilities.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Capability")]
        [ValidateSet("CAPABILITY_IAM", "CAPABILITY_NAMED_IAM", "CAPABILITY_AUTO_EXPAND")]
        public string[] Capabilities { get; set; }

        /// <summary>
        /// Gets or sets the force s3.
        /// <para type="description">
        /// If present, forces upload of a local template (file or string body) to S3, irrespective of whether the template size is over the maximum of 51,200 bytes
        /// </para>
        /// <para>
        /// Occasionally there is a socket closed by remote host exception thrown when working with local templates.
        /// Experimentation found that uploading the errant template to S3 first circumvents this error so is a suitable workaround, thus this option is provided.
        /// </para>
        /// </summary>
        [SuppressParameterSelect]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter ForceS3
        {
            get => this.ForceS3Flag;
            set => this.ForceS3Flag = value;
        }

        /// <summary>
        /// Gets or sets the notification ARNs.
        /// <para type="description">
        /// The Simple Notification Service (SNS) topic ARNs to publish stack related events.
        /// You can find your SNS topic ARNs using the SNS console or your Command Line Interface (CLI).
        /// </para>
        /// </summary>
        /// <value>
        /// The notification ARNs.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]

        // ReSharper disable once InconsistentNaming
        public string[] NotificationARNs { get; set; }

        /// <summary>
        /// Gets or sets the type of the resource.
        /// <para type="description">
        /// The template resource types that you have permissions to work with for this create stack action, such as AWS::EC2::Instance, AWS::EC2::*, or Custom::MyCustomInstance.
        /// Use the following syntax to describe template resource types: AWS::* (for all AWS resource), Custom::* (for all custom resources),
        /// Custom::logical_ID (for a specific custom resource), AWS::service_name::* (for all resources of a particular AWS service), and AWS::service_name::resource_logical_ID (for a specific AWS resource).
        /// If the list of resource types doesn't include a resource that you're creating, the stack creation fails. By default, AWS CloudFormation grants permissions to all resource types.
        /// AWS Identity and Access Management (IAM) uses this parameter for AWS CloudFormation-specific condition keys in IAM policies. For more information, see Controlling Access with AWS Identity and Access Management.
        /// </para>
        /// </summary>
        /// <value>
        /// The type of the resource.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[] ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the rollback configuration monitoring time in minute.
        /// <para type="description">
        /// The amount of time, in minutes, during which CloudFormation should monitor all the rollback triggers after the stack creation or update operation deploys all necessary resources.
        /// The default is 0 minutes.If you specify a monitoring period but do not specify any rollback triggers, CloudFormation still waits the specified period of time before cleaning up old resources after update operations.
        /// You can use this monitoring period to perform any manual stack validation desired, and manually cancel the stack creation or update (using CancelUpdateStack, for example) as necessary.
        /// If you specify 0 for this parameter, CloudFormation still monitors the specified rollback triggers during stack creation and update operations.
        /// Then, for update operations, it begins disposing of old resources immediately once the operation completes.
        /// </para>
        /// </summary>
        /// <value>
        /// The rollback configuration monitoring time in minute.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("RollbackConfiguration_MonitoringTimeInMinutes")]

        // ReSharper disable once InconsistentNaming
        public int RollbackConfiguration_MonitoringTimeInMinute { get; set; }

        /// <summary>
        /// Gets or sets the rollback configuration rollback trigger.
        /// <para type="description">
        /// The triggers to monitor during stack creation or update actions. By default, AWS CloudFormation saves the rollback triggers specified for a stack and applies them to any subsequent update operations for the stack, unless you specify otherwise.
        /// If you do specify rollback triggers for this parameter, those triggers replace any list of triggers previously specified for the stack. If a specified trigger is missing, the entire stack operation fails and is rolled back.
        /// </para>
        /// </summary>
        /// <value>
        /// The rollback configuration rollback trigger.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]

        // ReSharper disable once InconsistentNaming
        public RollbackTrigger[] RollbackConfiguration_RollbackTrigger { get; set; }

        /// <summary>
        /// Gets or sets the stack policy location.
        /// <para type="description">
        /// Structure containing the stack policy body. For more information, go to Prevent Updates to Stack Resources in the AWS CloudFormation User Guide.
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the policy body.
        /// </para>
        /// </summary>
        /// <value>
        /// The stack policy location.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("StackPolicyBody", "StackPolicyURL")]

        // ReSharper disable once UnusedMember.Global
        public string StackPolicyLocation
        {
            get => this.stackPolicyLocation;

            set
            {
                this.stackPolicyLocation = value;
                this.ResolvedStackPolicyLocation = this.PathResolver.ResolvePath(value);
            }
        }

        /// <summary>
        /// Gets or sets the tags.
        /// <para type="description">
        /// Key-value pairs to associate with this stack. AWS CloudFormation also propagates these tags to the resources created in the stack. A maximum number of 50 tags can be specified.
        /// </para>
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Tags")]
        public Tag[] Tag { get; set; }

        /// <summary>
        /// Gets or sets the template location.
        /// <para type="description">
        /// Structure containing the template body. For more information, go to Template Anatomy in the AWS CloudFormation User Guide.
        /// </para>
        /// <para type="description">
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the template body.
        /// </para>
        /// <para type="description">
        /// You can also pipe a template body to this command, e.g. from the output of the <c>New-PSCFNPackage</c> command.
        /// </para>
        /// </summary>
        /// <value>
        /// The template location.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [Alias("TemplateBody", "TemplateURL")]

        // ReSharper disable once UnusedMember.Global
        public string TemplateLocation
        {
            get => this.templateLocation;

            set
            {
                this.templateLocation = value;

                // Try to resolve as a path through the file system provider. PS and .NET may have different ideas about the current directory.
                this.ResolvedTemplateLocation = this.PathResolver.ResolvePath(value);
            }
        }

        /// <summary>
        /// Gets or sets the resolved stack policy location.
        /// </summary>
        /// <value>
        /// The resolved stack policy location.
        /// </value>
        protected string ResolvedStackPolicyLocation { get; set; }

        /// <inheritdoc />
        protected override TemplateStage TemplateStage => TemplateStage.Processed;

        /// <inheritdoc />
        protected override CloudFormationBuilder GetBuilder()
        {
            RollbackConfiguration rollbackConfiguration = null;

            if (this.RollbackConfiguration_RollbackTrigger != null)
            {
                rollbackConfiguration = new RollbackConfiguration
                                            {
                                                MonitoringTimeInMinutes =
                                                    this.RollbackConfiguration_MonitoringTimeInMinute,
                                                RollbackTriggers = this.RollbackConfiguration_RollbackTrigger.ToList()
                                            };
            }

            // Can't add parameters till dynamic parameters have been resolved.
            return base.GetBuilder().WithTemplateLocation(this.ResolvedTemplateLocation).WithTags(this.Tag)
                .WithNotificationARNs(this.NotificationARNs)
                .WithCapabilities(this.Capabilities?.Select(Capability.FindValue))
                .WithRollbackConfiguration(rollbackConfiguration).WithResourceType(this.ResourceType)
                .WithStackPolicy(this.ResolvedStackPolicyLocation).WithParameters(this.StackParameters)
                .WithForceS3(this.ForceS3);
        }

        /// <inheritdoc />
        protected override async Task<object> OnProcessRecord()
        {
            // If using previous template, there is by definition nothing to package.
            if (!this.UsePreviousTemplateFlag)
            {
                if (this.ResolvedTemplateLocation == null)
                {
                    throw new ArgumentException("Must supply -TemplateLocation");
                }

                // Override S3Util with one suitable for packaging
                using (var s3 = new S3Util(
                    this._ClientFactory,
                    this.Context,
                    this.ResolvedTemplateLocation,
                    null,
                    null,
                    null))
                {
                    // Check whether template needs packaging
                    var packager = new PackagerUtils(this.PathResolver, this.Logger, s3, new OSInfo());

                    if (packager.RequiresPackaging(this.ResolvedTemplateLocation))
                    {
                        this.Logger.LogInformation(
                            "Template contains resources that require packaging. Packager will use default bucket for storage.");
                        this.ResolvedTemplateLocation = await packager.ProcessTemplate(
                                                            this.ResolvedTemplateLocation,
                                                            this.WorkingDirectory);
                    }
                }
            }

            this.ProcessParameters();

            if (this.StackParameters.Any())
            {
                this.Logger.LogVerbose($"Parameters to pass to stack: {string.Join(", ", this.StackParameters.Keys)}");
            }

            return null;
        }
    }
}