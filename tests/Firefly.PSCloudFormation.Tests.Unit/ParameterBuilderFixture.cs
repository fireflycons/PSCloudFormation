using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Management.Automation;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.PSCloudFormation.Tests.Unit.Resources;

    using Moq;

    public class ParameterBuilderFixture
    {
        public ParameterBuilderFixture()
        {
            var templateBody = EmbeddedResourceManager.GetResourceString("ParameterTest.json");
            var mockTemplateResolver = new Mock<IInputFileResolver>();

            mockTemplateResolver.Setup(r => r.FileContent).Returns(templateBody);
            var templateManager = new TemplateManager(mockTemplateResolver.Object);
            this.ParameterDictionary = templateManager.GetStackDynamicParameters();
        }

        public RuntimeDefinedParameterDictionary ParameterDictionary { get; }
    }
}
