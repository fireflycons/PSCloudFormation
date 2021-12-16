namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Collections;
    using System.Collections.Generic;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Commands;
    using Firefly.PSCloudFormation.Tests.Common.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    using Stack = Amazon.CloudFormation.Model.Stack;

    public class StackOutputs
    {
        private readonly ITestOutputHelper output;

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

        private IAwsClientFactory ClientFactory { get; }

        private ICloudFormationContext Context { get; }

        [Fact]
        public async void ShouldReturnOutputsAsParameterBlock()
        {
            var cmd = new GetStackOutputsCommand
                          {
                              StackName = "test-stack",
                              AsParameterBlock = true
                          };

            var result = (string)await cmd.GetStackOutputs(
                                     this.Context,
                                     this.ClientFactory,
                                     GetStackOutputsCommand.ImportsParameterSet);

            result.Should().Contain("FirstParameter").And.Contain("SecondParameter");
        }

        [Fact]
        public async void ShouldReturnExportedOutputsAsImports()
        {
            var cmd = new GetStackOutputsCommand
                          {
                              StackName = "test-stack", AsCrossStackReferences = true
                          };

            var result = (string)await cmd.GetStackOutputs(
                this.Context,
                this.ClientFactory,
                GetStackOutputsCommand.ImportsParameterSet);

            result.Should().NotContain("SecondParameter", "ExportName is not declared on this parameter");
        }

        [Fact]
        public async void ShouldReturnTwoParametersWithExpectedValuesAsHashtable()
        {
            var cmd = new GetStackOutputsCommand { StackName = "test-stack", AsHashTable = true };

            var result = (Hashtable)await cmd.GetStackOutputs(
                this.Context,
                this.ClientFactory,
                GetStackOutputsCommand.HashParameterSet);

            result.Count.Should().Be(2);
            result["FirstParameter"].Should().Be("arn:aws:first-parameter");
            result["SecondParameter"].Should().Be("arn:aws:second-parameter");
        }
    }
}