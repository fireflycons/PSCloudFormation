namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;

    /// <summary>
    /// Parameters related to changeset creation.
    /// </summary>
    // ReSharper disable UnusedMemberInSuper.Global - This just describes parameters common to more than one cmdlet
    public interface IChangesetArguments
    {
        /// <summary>
        /// Gets or sets the changeset detail.
        /// <para type="description">
        /// Specifies a path to a file into which to write detailed JSON change information.
        /// This can be useful in situations where you need to get other people to review changes, or you want to add the changeset information to e.g. git.
        /// </para>
        /// <para type="description">
        /// The output is always JSON.
        /// </para>
        /// </summary>
        /// <value>
        /// The changeset detail.
        /// </value>
        string ChangesetDetail { get; set; }

        /// <summary>
        /// Gets or sets the include nested stacks.
        /// <para type="description">
        /// Creates a change set for the all nested stacks specified in the template.
        /// </para>
        /// </summary>
        /// <value>
        /// The include nested stacks.
        /// </value>
        SwitchParameter IncludeNestedStacks { get; set; }

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
        string ResourcesToImport { get; set; }

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
        SwitchParameter UsePreviousTemplate { get; set; }
    }
}