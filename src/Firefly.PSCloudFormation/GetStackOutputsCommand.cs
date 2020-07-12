namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Parsers;
    using Firefly.CloudFormation.Utils;

    /// <summary>
    /// <para type="synopsis">Get the outputs of a stack in various formats</para>
    /// <para type="description">
    /// This function can be used to assist creation of new CloudFormation templates
    /// that refer to the outputs of another stack.
    /// It can be used to generate either mapping or parameter blocks based on these outputs
    /// by converting the returned object to JSON or YAML
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSCFNStackOutputs1")]
    [CmdletBinding(DefaultParameterSetName = HashParameterSet)]

    // ReSharper disable once UnusedMember.Global - public api
    public class GetStackOutputsCommand : CloudFormationServiceCommand
    {
        /// <summary>
        /// Constant for Hash parameter set
        /// </summary>
        internal const string HashParameterSet = "Hash";

        /// <summary>
        /// Constant for import block parameter set.
        /// </summary>
        internal const string ImportsParameterSet = "Imports";

        /// <summary>
        /// Constant for parameter block parameter set
        /// </summary>
        internal const string ParameterBlockParameterSet = "Parameters";

        /// <summary>
        /// Gets or sets as cross stack references.
        /// <para type="description">
        ///  If set, returned object is formatted as a set of <c>Fn::ImportValue statements</c>, with any text matching the
        /// stack name within the output's ExportName being replaced with a placeholder generated from the stack name with the word 'Stack' appended.
        /// Make this a parameter to your new stack.
        /// </para>
        /// <para type="description">
        /// Whilst the result output is not much use as it is, the individual elements can
        /// be copied and pasted in where an <c>Fn::ImportValue</c> statements for that parameter would be used.
        /// </para>
        /// </summary>
        /// <value>
        /// As cross stack references.
        /// </value>
        [Parameter(Mandatory = true, ParameterSetName = ImportsParameterSet, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter AsCrossStackReferences { get; set; }

        /// <summary>
        /// Gets or sets switch for hash table output
        /// <para type="description">
        /// If set (default), returned object is a hash table - key/value pairs for each stack output.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = HashParameterSet, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter AsHashTable { get; set; }

        /// <summary>
        /// Gets or sets switch for parameter block output
        /// <para type="description">
        /// If set, returned object is formatted as a CloudFormation parameter block.
        /// </para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            ParameterSetName = ParameterBlockParameterSet,
            ValueFromPipelineByPropertyName = true)]
        public SwitchParameter AsParameterBlock { get; set; }

        /// <summary>
        /// Gets or sets the format for template outputs.
        /// <para type="description">
        /// Sets how output of parameters for CloudFormation template fragments should be formatted.
        /// </para>
        /// </summary>
        /// <value>
        /// The format.
        /// </value>
        [Parameter(
            Mandatory = true,
            ParameterSetName = ParameterBlockParameterSet,
            ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = ImportsParameterSet)]
        [ValidateSet("JSON", "YAML")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the stack name.
        /// <para type="description">
        /// One or more stacks to process. One object is produced for each stack
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        public string StackName { get; set; }

        /// <summary>
        /// Gets the stack outputs
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="clientFactory">The client factory.</param>
        /// <param name="parameterSetName">Name of the parameter set in force</param>
        /// <returns>Object containing outputs in selected format.</returns>
        internal async Task<object> GetStackOutputs(ICloudFormationContext context, IAwsClientFactory clientFactory, string parameterSetName)
        {
            var ops = new CloudFormationOperations(clientFactory, context);

            var stackOutputs = (await ops.GetStackAsync(this.StackName)).Outputs;

            switch (parameterSetName)
            {
                case HashParameterSet:

                    return new Hashtable(stackOutputs.ToDictionary(o => o.OutputKey, o => o.OutputValue));

                case ParameterBlockParameterSet:

                    var parameters = new Dictionary<string, Dictionary<string, string>>();

                    foreach (var p in stackOutputs)
                    {
                        var parameterProps = new Dictionary<string, string>
                                                 {
                                                     {
                                                         "Type",
                                                         TemplateManager.GetParameterTypeFromStringValue(p.OutputValue)
                                                     }
                                                 };

                        if (!string.IsNullOrEmpty(p.Description))
                        {
                            parameterProps.Add("Description", p.Description);
                        }

                        parameters.Add(p.OutputKey, parameterProps);
                    }

                    return TemplateParser.SerializeObjectGraphToString(
                        new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                            {
                                { "Parameters", parameters }
                            },
                        (SerializationFormat)Enum.Parse(typeof(SerializationFormat), this.Format, true));

                case ImportsParameterSet:

                    var ti = new CultureInfo("en-US");
                    var stackParam = string.Join(
                        string.Empty,
                        this.StackName.Split('-', '_').Select(s => ti.TextInfo.ToTitleCase(s)));

                    return TemplateParser.SerializeObjectGraphToString(
                        stackOutputs.Where(o => !string.IsNullOrEmpty(o.ExportName)).Select(
                            p => new Dictionary<string, Dictionary<string, string>>
                                     {
                                         {
                                             "Fn::ImportValue",
                                             new Dictionary<string, string>
                                                 {
                                                     {
                                                         "Fn::Sub",
                                                         p.ExportName.Replace(this.StackName, $"${{{stackParam}}}Stack")
                                                     }
                                                 }
                                         }
                                     }).ToList(),
                        (SerializationFormat)Enum.Parse(typeof(SerializationFormat), this.Format, true));

                default:

                    this.ThrowExecutionError($"Unsupported parameter set {this.ParameterSetName}", this, null);
                    return null;
            }
        }

        /// <summary>
        /// Process pipeline record
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var context = this.CreateCloudFormationContext();

            try
            {
                this.WriteObject(
                    this.GetStackOutputs(
                        context,
                        new PSAwsClientFactory(
                            this.CreateClient(this._CurrentCredentials, this._RegionEndpoint),
                            context),
                        this.ParameterSetName).Result);
            }
            catch (Exception e)
            {
                this.ThrowExecutionError(e.Message, this, null);
            }
        }
    }
}