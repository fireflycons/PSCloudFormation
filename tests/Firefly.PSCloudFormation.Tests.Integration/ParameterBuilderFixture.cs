namespace Firefly.PSCloudFormation.Tests.Integration
{
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    using System.Collections.Generic;
    using System.Management.Automation;

    using Firefly.CloudFormation.Resolvers;
    using Firefly.EmbeddedResourceLoader;

    using Moq;

    /// <summary>
    /// Fixture for the parameter builder tests which materializes required embedded resources.
    /// </summary>
    /// <seealso cref="Firefly.EmbeddedResourceLoader.AutoResourceLoader" />
    public class ParameterBuilderFixture : AutoResourceLoader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterBuilderFixture"/> class.
        /// </summary>
        public ParameterBuilderFixture()
        {
            var templateBody = this.ParamterTestJson;
            var mockTemplateResolver = new Mock<ITemplateResolver>();

            mockTemplateResolver.Setup(r => r.FileContent).Returns(templateBody);
            mockTemplateResolver.Setup(r => r.NoEchoParameters).Returns(new List<string>());

            var templateManager = new TemplateManager(mockTemplateResolver.Object, StackOperation.Create, null);
            this.ParameterDictionary = templateManager.GetStackDynamicParameters(new Dictionary<string, string>());
        }

        /// <summary>
        /// Gets the PowerShell dynamic parameter dictionary.
        /// </summary>
        /// <value>
        /// The parameter dictionary.
        /// </value>
        public RuntimeDefinedParameterDictionary ParameterDictionary { get; }

        /// <summary>
        /// Gets the content of embedded resource <c>ParameterTest.yaml</c>.
        /// </summary>
        [EmbeddedResource("ParameterTest.yaml")]
        public string ParameterTestYaml { get; private set; }

        /// <summary>
        /// Gets the content of embedded resource <c>ParameterTest.json</c>.
        /// </summary>
        [EmbeddedResource("ParameterTest.json")]
        public string ParamterTestJson { get; private set; }
    }
}