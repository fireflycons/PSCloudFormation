namespace Firefly.PSCloudFormation
{
    using System.Text.RegularExpressions;

    using Firefly.CloudFormation.Parsers;

    /// <summary>
    /// Splits the lambda resource's Handler property.
    /// Currently only supports Node, Python and Ruby
    /// </summary>
    internal class LambdaHandlerInfo
    {
        /// <summary>
        /// Regex to match handlers in form file.method or ruby's file.module::class.method
        /// </summary>
        private static readonly Regex HandlerRegex = new Regex(
            @"^((?<file>[^\.]+)\.(?<module>[^\.:]+::[^\.:]+(::[^\.:]+){0,}\.)?(?<method>[^\.]+))$");

        /// <summary>
        /// Initializes a new instance of the <see cref="LambdaHandlerInfo"/> class.
        /// </summary>
        /// <param name="lambdaResource"><see cref="TemplateResource"/> describing the lambda</param>
        public LambdaHandlerInfo(TemplateResource lambdaResource)
        {
            this.Handler = lambdaResource.GetResourcePropertyValue("Handler");

            var m = HandlerRegex.Match(this.Handler);

            this.IsValidSignature = m.Success;

            if (!m.Success)
            {
                return;
            }

            this.FilePart = m.Groups["file"].Value;
            this.ModulePart = m.Groups["module"].Value;
            this.MethodPart = m.Groups["method"].Value;
        }

        /// <summary>
        /// Gets the file part of the handler value.
        /// </summary>
        /// <value>
        /// The file part.
        /// </value>
        public string FilePart { get; }

        /// <summary>
        /// Gets the handler, as passed to the constructor.
        /// </summary>
        /// <value>
        /// The handler.
        /// </value>
        public string Handler { get; }

        /// <summary>
        /// Gets a value indicating whether the handler info is valid (we got at least a file and a method)
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValidSignature { get; }

        /// <summary>
        /// Gets the method part of the handler value.
        /// </summary>
        /// <value>
        /// The method part.
        /// </value>
        public string MethodPart { get; }

        /// <summary>
        /// Gets the module part of the handler value if any.
        /// </summary>
        /// <value>
        /// The module part; else <c>null</c> if none.
        /// </value>
        public string ModulePart { get; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Handler;
        }
    }
}