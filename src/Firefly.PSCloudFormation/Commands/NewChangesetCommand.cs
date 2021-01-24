namespace Firefly.PSCloudFormation.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Amazon.CloudFormation.Model;

    /// <summary>
    /// <para type="synopsis">
    /// Calls the AWS CloudFormation CreateChangeSet API operation.
    /// </para>
    /// <para type="description">
    /// Creates a list of changes that will be applied to a stack so that you can review the changes before executing them.
    /// You can create a change set for a stack that doesn't exist or an existing stack.
    /// If you create a change set for a stack that doesn't exist, the change set shows all of the resources that AWS CloudFormation will create.
    /// If you create a change set for an existing stack, AWS CloudFormation compares the stack's information with the information that you submit in the change set and lists the differences.
    /// Use change sets to understand which resources AWS CloudFormation will create or change, and how it will change resources in an existing stack, before you create or update a stack.
    /// When you are satisfied with the changes the change set will make, execute the change set by using the ExecuteChangeSet action.
    /// AWS CloudFormation doesn't make changes until you execute the change set. To create a change set for the entire stack hierarchy, set IncludeNestedStacks to True.
    /// </para>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.StackParameterCloudFormationCommand" />
    /// <seealso cref="Firefly.PSCloudFormation.IChangesetArguments" />
    [Cmdlet(VerbsCommon.New, "PSCFNChangeset")]
    // ReSharper disable once UnusedMember.Global
    public class NewChangesetCommand : StackParameterCloudFormationCommand, IChangesetArguments
    {
        /// <summary>
        /// The changeset detail output
        /// </summary>
        private string changesetDetail;

        /// <summary>
        /// The resources to import
        /// </summary>
        private string resourcesToImport;

        /// <summary>
        /// Gets or sets the changeset detail.
        /// <para type="description">
        /// Specifies a path to a file into which to write detailed JSON change information.
        /// This can be useful in situations where you need to get other people to review changes, or you want to add the changeset information to e.g. git.
        /// </para>
        /// <para type="description">
        /// If this argument is not present, the detail is output to the pipeline. The output is always JSON.
        /// </para>
        /// </summary>
        /// <value>
        /// The changeset detail.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        // ReSharper disable once UnusedMember.Global
        public string ChangesetDetail
        {
            get => this.changesetDetail;

            set
            {
                this.changesetDetail = value;
                this.ResolvedChangesetDetail = this.PathResolver.ResolvePath(value);
            }
        }

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
        /// Gets or sets the resources to import.
        /// <para type="description">
        /// The resources to import into your stack.
        /// </para>
        /// <para type="description">
        /// If you created an AWS resource outside of AWS CloudFormation management, you can bring this existing resource into AWS CloudFormation management using resource import.
        /// You can manage your resources using AWS CloudFormation regardless of where they were created without having to delete and re-create them as part of a stack.
        /// Note that when performing an import, this is the only change that can happen to the stack. If any other resources are changed, the changeset will fail to create.
        /// </para>
        /// <para type="description">
        /// You can specify either a string, path to a file, or URL of a object in S3 that contains the resource import body as JSON or YAML.
        /// </para>
        /// <para type="link" uri="https://fireflycons.github.io/PSCloudFormation/articles/changesets.html">Changeset documentation (PSCloudFormation)</para>
        /// </summary>
        /// <value>
        /// The resources to import.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        // ReSharper disable once UnusedMember.Global
        public string ResourcesToImport
        {
            get => this.resourcesToImport;

            set
            {
                this.resourcesToImport = value;
                this.ResolvedResourcesToImport = this.PathResolver.ResolvePath(value);
            }
        }

        /// <summary>
        /// Gets or sets the show in browser.
        /// <para type="description">
        /// If set and a GUI is detected, display the changeset detail in the default browser.
        /// </para>
        /// <para type="link" uri="https://fireflycons.github.io/PSCloudFormation/articles/changesets.html">Changeset documentation (PSCloudFormation)</para>
        /// </summary>
        /// <value>
        /// The show in browser.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter ShowInBrowser { get; set; }

        /// <summary>
        /// Gets or sets the select.
        /// </summary>
        /// <value>
        /// The select.
        /// </value>
        public override string Select { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected override StackOperation StackOperation { get; }

        /// <summary>
        /// Gets the valid values for select argument.
        /// </summary>
        /// <returns>
        /// List of acceptable values
        /// </returns>
        protected override List<string> GetSelectArgumentValues()
        {
            // Use the Select system to select JSON string output
            var outputs = base.GetSelectArgumentValues();
            outputs.Add("json");
            return outputs;
        }

        /// <summary>
        /// Begins the processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.ChangesetDetail == null && !this.ShowInBrowser)
            {
                // Select output as JSON to pipeline
                this.Select = "json";
            }

            base.BeginProcessing();
        }

        /// <summary>
        /// New handler for ProcessRecord. Ensures CloudFormation client is properly disposed.
        /// </summary>
        /// <returns>
        /// Output to place into pipeline.
        /// </returns>
        /// <exception cref="ArgumentException">Must supply -TemplateLocation</exception>
        protected override async Task<object> OnProcessRecord()
        {
            if (this.ResolvedTemplateLocation == null && !this.UsePreviousTemplateFlag)
            {
                throw new ArgumentException("Must supply one of -TemplateLocation, -UsePreviousTemplate");
            }

            await base.OnProcessRecord();

            using (var runner = this.GetBuilder()
                .WithUsePreviousTemplate(this.UsePreviousTemplateFlag)
                .WithResourceImports(this.ResolvedResourcesToImport)
                .WithIncludeNestedStacks(this.IncludeNestedStacks)
                .WithChangesetOnly()
                .Build())
            {
               var result = await runner.UpdateStackAsync(null);

               if (this.ShowInBrowser)
               {
                   var logger = (PSLogger)this.Logger;

                   if (logger.CanViewChangesetInBrowser())
                   {
                       logger.ViewChangesetInBrowser();
                   }
                   else
                   {
                       logger.LogWarning("Failed to detect a GUI. If you think this is incorrect, please raise an issue.");
                   }
               }

               return result;
            }
        }

        /// <summary>
        /// Gets the JSON changes to return as selected output.
        /// </summary>
        /// <param name="outputName">Name of the output.</param>
        /// <returns>
        /// The value for the selected output.
        /// </returns>
        /// <exception cref="ArgumentException">Invalid value for -Select parameter.</exception>
        protected override object GetSelectedOutput(string outputName)
        {
            if (outputName != "json")
            {
                throw new ArgumentException("Invalid value for -Select parameter.");
            }

            return ((PSLogger)this.Logger).GetJsonChanges();
        }
    }
}