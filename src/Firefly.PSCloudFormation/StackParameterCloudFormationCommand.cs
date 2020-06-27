namespace Firefly.PSCloudFormation
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.CloudFormation.Template;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Base class for Create/Update operations
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.BaseCloudFormationCommand" />
    /// <seealso cref="System.Management.Automation.IDynamicParameters" />
    public abstract class StackParameterCloudFormationCommand : BaseCloudFormationCommand, IDynamicParameters
    {
        /// <summary>
        /// List of parameter names entered as arguments by the user to look up in <c>PSBoundParameters</c>
        /// </summary>
        private List<string> stackParameterNames = new List<string>();

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
        /// Gets or sets the parameter file.
        /// <para type="description">
        /// If present, path to a JSON file containing a list of parameter structures as defined for 'aws cloudformation create-stack'. If a parameter of the same name is defined on the command line, the command line takes precedence.
        /// If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.
        /// </para>
        /// </summary>
        /// <value>
        /// The parameter file.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ParameterFile { get; set; }

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
        /// <parameter type="description">
        /// The triggers to monitor during stack creation or update actions. By default, AWS CloudFormation saves the rollback triggers specified for a stack and applies them to any subsequent update operations for the stack, unless you specify otherwise.
        /// If you do specify rollback triggers for this parameter, those triggers replace any list of triggers previously specified for the stack. If a specified trigger is missing, the entire stack operation fails and is rolled back.
        /// </parameter>
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
        public string StackPolicyLocation { get; set; }

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
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the template body.
        /// </para>
        /// </summary>
        /// <value>
        /// The template location.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("TemplateBody", "TemplateURL")]
        public string TemplateLocation { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected abstract StackOperation StackOperation { get;  }

        /// <summary>
        /// Gets the list of parameters that will be passed to CloudFormation.
        /// </summary>
        /// <value>
        /// The stack parameters.
        /// </value>
        protected IDictionary<string, string> StackParameters { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets all parameters as defined by the template.
        /// </summary>
        /// <value>
        /// The template parameters.
        /// </value>
        protected List<TemplateFileParameter> TemplateParameters { get; private set; } =
            new List<TemplateFileParameter>();

        /// <summary>
        /// Gets or sets a value indicating whether [use previous template flag].
        /// This is overridden by Update commands
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use previous template flag]; otherwise, <c>false</c>.
        /// </value>
        protected bool UsePreviousTemplateFlag { get; set; }

        /// <summary>
        /// Gets any parameters defined in the CloudFormation template as a <see cref="RuntimeDefinedParameterDictionary"/>
        /// </summary>
        /// <returns>A <see cref="RuntimeDefinedParameterDictionary"/></returns>
        public object GetDynamicParameters()
        {
            var templateResolver = new TemplateResolver(
                this.CreateCloudFormationContext(),
                this.StackName,
                this.UsePreviousTemplateFlag);

            var task = templateResolver.ResolveFileAsync(this.TemplateLocation);
            task.Wait();

            var templateManager = new TemplateManager(templateResolver, this.StackOperation);

            var paramsDictionary = templateManager.GetStackDynamicParameters();

            // All parameters parsed from the template
            this.TemplateParameters = templateManager.TemplateParameters;

            // Names of parameters entered by the user.
            this.stackParameterNames = paramsDictionary.Keys.ToList();

            return templateManager.GetStackDynamicParameters();
        }

        /// <summary>
        /// Reads a file containing values for template parameters..
        /// </summary>
        /// <returns>Dictionary of parameter key/parameter value.</returns>
        internal Dictionary<string, string> ReadParameterFile()
        {
            if (string.IsNullOrEmpty(this.ParameterFile))
            {
                return new Dictionary<string, string>();
            }

            using (var reader = File.OpenText(this.ParameterFile))
            {
                return ((JArray)JToken.ReadFrom(new JsonTextReader(reader))).Cast<JObject>().ToDictionary(
                    o => o["ParameterKey"].ToString(),
                    o => o["ParameterValue"].ToString());
            }
        }

        /// <summary>
        /// Gets the builder for <see cref="CloudFormationRunner" /> and populates the fields pertinent to this level.
        /// </summary>
        /// <returns>
        /// Builder for <see cref="CloudFormationRunner" />.
        /// </returns>
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
            return base.GetBuilder()
                .WithTemplateLocation(this.TemplateLocation)
                .WithTags(this.Tag)
                .WithNotificationARNs(this.NotificationARNs)
                .WithCapabilities(this.Capabilities?.Select(Capability.FindValue))
                .WithRollbackConfiguration(rollbackConfiguration)
                .WithResourceType(this.ResourceType)
                .WithStackPolicy(this.StackPolicyLocation)
                .WithParameters(this.StackParameters);
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        protected override async Task<object> OnProcessRecord()
        {
            await Task.Run(
                () =>
                    {
                        // Read parameter file, if any
                        var fileParameters = this.ReadParameterFile();

                        // Resolve dynamic parameters to stack parameters
                        var userParameters = this.MyInvocation.BoundParameters
                            .Where(p => this.stackParameterNames.Contains(p.Key)).ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.ToString());

                        // Merge file and user parameters, with user parameters taking precedence
                        this.StackParameters = userParameters
                            .Concat(fileParameters.Where(kvp => !userParameters.ContainsKey(kvp.Key)))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    });

            return null;
        }
    }
}