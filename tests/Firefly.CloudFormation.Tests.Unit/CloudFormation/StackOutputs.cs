using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System.Linq;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests <see cref="CloudFormationOperations.GetStackOutputsAsync"/>
    /// </summary>
    public class StackOutputs
    {
        /// <summary>
        /// The stack name
        /// </summary>
        private const string StackName = "test-stack";

        /// <summary>
        /// The output
        /// </summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="StackOutputs"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public StackOutputs(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Stack should return expected outputs.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldReturnExpectedOutputs()
        {
            var expectedParameters = new Dictionary<string, string>
                                         {
                                             { "Output1", "Value1" },
                                             { "Output2", "Value2" }
                                         };

            var stackOutputs = expectedParameters
                .Select(kv => new Output { OutputKey = kv.Key, OutputValue = kv.Value }).ToList();

            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCf = new Mock<IAmazonCloudFormation>();
            mockCf.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).ReturnsAsync(
                new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                     {
                                         new Stack()
                                             {
                                                 StackName = StackName,
                                                 Outputs = stackOutputs
                                             }
                                     }
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);
            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);

            var outputs = (await operations.GetStackOutputsAsync(StackName)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            outputs.SequenceEqual(expectedParameters).Should().BeTrue();
        }
    }
}
