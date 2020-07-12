using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    using InvalidOperationException = System.InvalidOperationException;

    /// <summary>
    /// Tests for <see cref="CloudFormationOperations.StackExistsAsync"/>
    /// </summary>
    public class StackExists
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
        /// Initializes a new instance of the <see cref="StackExists"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public StackExists(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Test for affirmative result.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldExistWhenDescribeStacksReturnsTheStack()
        {
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
                                                 StackName = StackName
                                             }
                                     }
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);
            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);

            (await operations.StackExistsAsync(StackName)).Should().BeTrue();
        }

        /// <summary>
        /// Test for negative result
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldNotExistWhenDescribeStacksThrowsTheExpectedExceptionForStackNotFound()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCf = new Mock<IAmazonCloudFormation>();
            mockCf.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).Throws(
                new AmazonCloudFormationException($"Stack with id {StackName} does not exist"));

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);
            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);

            (await operations.StackExistsAsync(StackName)).Should().BeFalse();
        }

        /// <summary>
        /// Test for unexpected exception
        /// </summary>
        [Fact]
        public void ShoulPassOnExceptionWhenDescribeStacksThrowsUnexpectedException()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCf = new Mock<IAmazonCloudFormation>();
            mockCf.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).Throws(
                new InvalidOperationException());

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);
            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);

            Func<Task<bool>> action = async () => await operations.StackExistsAsync(StackName);

            action.Should().Throw<InvalidOperationException>();
        }
    }
}
