﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Management.Automation;

    using Firefly.CloudFormation.CloudFormation;
    using Firefly.CloudFormation.Resolvers;
    using Firefly.PSCloudFormation.Tests.Unit.Resources;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    using Moq;

    public class ParameterBuilderFixture
    {
        public ParameterBuilderFixture()
        {
            var templateBody = EmbeddedResourceManager.GetResourceString("ParameterTest.json");
            var mockTemplateResolver = new Mock<IInputFileResolver>();

            mockTemplateResolver.Setup(r => r.FileContent).Returns(templateBody);
            var templateManager = new TemplateManager(mockTemplateResolver.Object, StackOperation.Create, null);
            this.ParameterDictionary = templateManager.GetStackDynamicParameters(new Dictionary<string, string>());
        }

        public RuntimeDefinedParameterDictionary ParameterDictionary { get; }
    }
}
