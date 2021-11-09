namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;

    using Amazon.CloudFormation;

    using Firefly.CloudFormation.Parsers;
    using Firefly.CloudFormation.Resolvers;
    using Firefly.CloudFormation.Utils;
    using Firefly.CloudFormationParser;

    public abstract class TemplateResolvingCloudFormationCommand : BaseCloudFormationCommand, IDynamicParameters
    {
        /// <summary>
        /// List of parameter names entered as arguments by the user to look up in <c>PSBoundParameters</c>
        /// </summary>
        protected List<string> StackParameterNames = new List<string>();

        /// <summary>
        /// The parameter file location
        /// </summary>
        private string parameterFile;

        /// <summary>
        /// Gets or sets the parameter file.
        /// <para type="description">
        /// If present, location of a list of stack parameters to apply.
        /// This is a JSON or YAML list of parameter structures with fields <c>ParameterKey</c> and <c>ParameterValue</c>.
        /// This is similar to <c>aws cloudformation create-stack</c>  except the other fields defined for that are ignored here.
        /// Parameters not supplied to an update operation are assumed to be <c>UsePreviousValue</c>.
        /// If a parameter of the same name is defined on the command line, the command line takes precedence.
        /// If your stack has a parameter with the same name as one of the parameters to this cmdlet, then you *must* set the stack parameter via a parameter file.
        /// </para>
        /// <para type="description">
        /// You can specify either a string containing JSON or YAML, or path to a file that contains the parameters.
        /// </para>
        /// </summary>
        /// <value>
        /// The parameter file.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]

        // ReSharper disable once UnusedMember.Global
        public string ParameterFile
        {
            get => this.parameterFile;

            set
            {
                this.parameterFile = value;
                this.ResolvedParameterFile = this.PathResolver.ResolvePath(value);
            }
        }

        protected SwitchParameter ForceS3Flag { get; set; }

        /// <summary>
        /// Gets or sets the resolved parameter file.
        /// </summary>
        /// <value>
        /// The resolved parameter file.
        /// </value>
        protected string ResolvedParameterFile { get; set; }

        /// <summary>
        /// Gets or sets the resolved template location.
        /// </summary>
        /// <value>
        /// The resolved template location.
        /// </value>
        protected string ResolvedTemplateLocation { get; set; }

        /// <summary>
        /// Gets the stack operation.
        /// </summary>
        /// <value>
        /// The stack operation.
        /// </value>
        protected abstract StackOperation StackOperation { get; }

        /// <summary>
        /// Gets the list of parameters that will be passed to CloudFormation.
        /// </summary>
        /// <value>
        /// The stack parameters.
        /// </value>
        protected IDictionary<string, string> StackParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets all non-SSM parameters as defined by the template.
        /// </summary>
        /// <value>
        /// The template parameters.
        /// </value>
        protected List<IParameter> TemplateParameters { get; private set; } = new List<IParameter>();

        /// <summary>
        /// Gets or sets a value indicating whether <c>-UsePreviousTemplate</c> switch was set by the <c>Update-PSCFNStack</c> cmdlet.
        /// </summary>
        /// <value>
        ///   <c>true</c> if <c>-UsePreviousTemplate</c> was set; otherwise, <c>false</c>.
        /// </value>
        protected bool UsePreviousTemplateFlag { get; set; }

        /// <inheritdoc cref="BaseCloudFormationCommand" />
        protected abstract TemplateStage TemplateStage { get; }

        /// <summary>
        /// Gets any parameters defined in the CloudFormation template as a <see cref="RuntimeDefinedParameterDictionary"/>
        /// </summary>
        /// <returns>A <see cref="RuntimeDefinedParameterDictionary"/></returns>
        public object GetDynamicParameters()
        {
            if (this.ResolvedTemplateLocation == null && !this.UsePreviousTemplateFlag)
            {
                // We can also get called when user is tab-completing the cmdlet name.
                // Clearly we have no arguments yet.
                // However if we are completing arguments, and -UsePreviousTemplate is set then we do need to run.
                return null;
            }

            var templateResolver = new TemplateResolver(
                this.CreateCloudFormationContext(),
                this.TemplateStage,
                this.StackName,
                this.UsePreviousTemplateFlag,
                this.ForceS3Flag);

            try
            {
                var task = templateResolver.ResolveFileAsync(this.ResolvedTemplateLocation);
                task.Wait();
            }
            catch (AggregateException e)
            {
                var ex = e.InnerExceptions.FirstOrDefault() ?? e;
                this.ThrowExecutionError(e.Message, this, ex);
            }

            var templateManager = new TemplateManager(templateResolver, this.StackOperation, this.Logger);

            var paramsDictionary = templateManager.GetStackDynamicParameters(this.ReadParameterFile());

            // All parameters parsed from the template
            this.TemplateParameters = templateManager.TemplateParameters;

            // Names of parameters entered by the user.
            this.StackParameterNames = paramsDictionary.Keys.ToList();

            return paramsDictionary;
        }

        /// <summary>
        /// Reads a file containing values for template parameters..
        /// </summary>
        /// <returns>Dictionary of parameter key/parameter value.</returns>
        internal IDictionary<string, string> ReadParameterFile()
        {
            return string.IsNullOrEmpty(this.ResolvedParameterFile)
                       ? new Dictionary<string, string>()
                       : ParameterFileParser.Create(this.ResolveParameterFileContent()).ParseParameterFile();
        }

        /// <summary>
        /// Processes the parameters.
        /// </summary>
        protected virtual void ProcessParameters()
        {
            // Read parameter file, if any
            var fileParameters = this.ReadParameterFile();

            if (fileParameters.Any())
            {
                this.Logger.LogVerbose($"Parameters from file: {string.Join(", ", fileParameters.Keys)}");
            }

            // Resolve dynamic parameters to stack parameters
            var userParameters = this.MyInvocation.BoundParameters.Where(p => this.StackParameterNames.Contains(p.Key))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                        {
                            if (kvp.Value is Array arr)
                            {
                                return string.Join(",", from object val in arr select val.ToString().Trim());
                            }

                            return kvp.Value.ToString();
                        });

            // Merge file and user parameters, with user parameters taking precedence
            this.StackParameters = userParameters
                .Concat(fileParameters.Where(kvp => !userParameters.ContainsKey(kvp.Key)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Resolves the content of the parameter file.
        /// </summary>
        /// <returns>Parameter file content.</returns>
        /// <exception cref="ArgumentException">Parameter file cannot be in S3 - ParameterFile</exception>
        private string ResolveParameterFileContent()
        {
            if (File.Exists(this.ResolvedParameterFile))
            {
                return File.ReadAllText(this.ResolvedParameterFile);
            }

            if (Uri.TryCreate(this.ResolvedParameterFile, UriKind.Absolute, out var uri)
                && !string.IsNullOrEmpty(uri.Scheme))
            {
                throw new ArgumentException("Parameter file cannot be in S3", nameof(this.ParameterFile));
            }

            // It is string content
            return this.ResolvedParameterFile.Unquote();
        }
    }
}