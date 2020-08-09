using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Collections;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;
    using Amazon.S3.Model;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    using Stack = Amazon.CloudFormation.Model.Stack;

    public class StackOutputs
    {
        private readonly ITestOutputHelper output;

        private ICloudFormationContext Context { get; }

        private IAwsClientFactory ClientFactory { get; }

        public StackOutputs(ITestOutputHelper output)
        {
            this.output = output;
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            this.Context = TestHelpers.GetContextMock(logger).Object;

            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(
                    new DescribeStacksResponse
                        {
                            Stacks = new List<Stack>
                                         {
                                             new Stack
                                                 {
                                                     Outputs = new List<Output>
                                                                   {
                                                                       new Output
                                                                           {
                                                                               Description = "First Parameter",
                                                                               ExportName = "test-stack-FirstParameter",
                                                                               OutputKey = "FirstParameter",
                                                                               OutputValue = "arn:aws:first-parameter"
                                                                           },
                                                                       new Output
                                                                           {
                                                                               Description = "Second Parameter",
                                                                               OutputKey = "SecondParameter",
                                                                               OutputValue = "arn:aws:second-parameter"
                                                                           }
                                                                   }
                                                 }
                                         }
                        });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            this.ClientFactory = mockClientFactory.Object;
        }

        [Fact]
        public void ShouldReturnTwoParametersWithExpectedValuesAsHashtable()
        {
            var cmd = new GetStackOutputsCommand { StackName = "test-stack", AsHashTable = true };

            var result = (Hashtable)cmd.GetStackOutputs(this.Context, this.ClientFactory, GetStackOutputsCommand.HashParameterSet).Result;

            result.Count.Should().Be(2);
            result["FirstParameter"].Should().Be("arn:aws:first-parameter");
            result["SecondParameter"].Should().Be("arn:aws:second-parameter");
        }

        [Theory]
        [InlineData("JSON")]
        [InlineData("YAML")]
        public void ShouldReturnImportsInCorrectFormat(string format)
        {
            var cmd = new GetStackOutputsCommand { StackName = "test-stack", AsCrossStackReferences = true, Format = format };

            var result = (string)cmd.GetStackOutputs(this.Context, this.ClientFactory, GetStackOutputsCommand.ImportsParameterSet).Result;
        }
    }
}
