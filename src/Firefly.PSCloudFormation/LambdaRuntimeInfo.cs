﻿namespace Firefly.PSCloudFormation
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation.Parsers;

    /// <summary>
    /// Object describing the runtime of a lambda
    /// </summary>
    [DebuggerDisplay("{runtime}")]
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
        /// The runtime as read from the template
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable - used for debugger display
        private readonly string runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaRuntimeInfo"/> class.
        /// </summary>
        /// <param name="lambdaResource">The lambda resource.</param>
        /// <exception cref="PackagerException">
        /// Missing property OR unknown runtime
        /// </exception>
        public LambdaRuntimeInfo(TemplateResource lambdaResource)
        {
            this.runtime = lambdaResource.GetResourcePropertyValue("Runtime");

            if (this.runtime == null)
            {
                throw new PackagerException($"{lambdaResource.LogicalName}: Missing property 'Runtime'");
            }

            var m = RuntimeVersionRegex.Match(this.runtime);

            if (!m.Success)
            {
                throw new PackagerException($"{lambdaResource.LogicalName}: Unknown runtime '{this.runtime}'");
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