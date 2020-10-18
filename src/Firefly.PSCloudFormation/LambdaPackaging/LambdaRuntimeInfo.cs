namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation.Parsers;

    /// <summary>
    /// Object describing the runtime of a lambda
    /// </summary>
    [DebuggerDisplay("{Runtime}")]
    internal class LambdaRuntimeInfo
    {
        /// <summary>
        /// Map of string language to runtime type.
        /// </summary>
        private static readonly Dictionary<string, LambdaRuntimeType> RuntimeTypes =
            new Dictionary<string, LambdaRuntimeType>
                {
                    { "python", LambdaRuntimeType.Python },
                    { "nodejs", LambdaRuntimeType.Node },
                    { "ruby", LambdaRuntimeType.Ruby },
                    { "java", LambdaRuntimeType.Java },
                    { "dotnetcore", LambdaRuntimeType.DotNet },
                    { "go", LambdaRuntimeType.Go },
                    { "provided", LambdaRuntimeType.Custom }
                };

        /// <summary>
        /// Regex to decode runtime property
        /// </summary>
        private static readonly Regex RuntimeVersionRegex = new Regex(@"(?<lang>[a-z]+)(?<version>[\d\.][a-z0-9\.]*)?");

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaRuntimeInfo"/> class.
        /// </summary>
        /// <param name="lambdaResource">The lambda resource.</param>
        /// <exception cref="PackagerException">
        /// Missing property OR unknown runtime
        /// </exception>
        public LambdaRuntimeInfo(ITemplateResource lambdaResource)
        {
            this.Runtime = lambdaResource.GetResourcePropertyValue("Runtime");

            if (this.Runtime == null)
            {
                throw new PackagerException($"{lambdaResource.LogicalName}: Missing property 'Runtime'");
            }

            var m = RuntimeVersionRegex.Match(this.Runtime);

            if (!m.Success)
            {
                throw new PackagerException($"{lambdaResource.LogicalName}: Unknown runtime '{this.Runtime}'");
            }

            var lang = m.Groups["lang"].Value;

            if (!RuntimeTypes.ContainsKey(lang))
            {
                throw new PackagerException($"{lambdaResource.LogicalName}: Unknown runtime '{lang}'");
            }

            this.RuntimeType = RuntimeTypes[lang];
            this.RuntimeVersion = m.Groups["version"].Value;
        }

        /// <summary>
        /// Gets the runtime as read from the template (e.g. python3.8
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable - used for debugger display
        public string Runtime { get; }

        /// <summary>
        /// Gets or sets the runtime type.
        /// </summary>
        /// <value>
        /// The type of the runtime.
        /// </value>
        public LambdaRuntimeType RuntimeType { get; set; }

        /// <summary>
        /// Gets or sets the runtime version.
        /// </summary>
        /// <value>
        /// The runtime version.
        /// </value>
        public string RuntimeVersion { get; set; }
    }
}