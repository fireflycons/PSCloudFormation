namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests <see cref="CloudFormationOperations.GetStackOperationalStateAsync"/>
    /// </summary>
    public class OperationalState
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
        /// Initializes a new instance of the <see cref="OperationalState"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public OperationalState(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Tests that stack should be busy when create or update is in progress.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
        /// <returns>A <see cref="Task"/></returns>
        [Theory]
        [InlineData("CREATE_IN_PROGRESS")]
        [InlineData("IMPORT_IN_PROGRESS")]
        [InlineData("IMPORT_ROLLBACK_IN_PROGRESS")]
        [InlineData("REVIEW_IN_PROGRESS")]
        [InlineData("ROLLBACK_IN_PROGRESS")]
        [InlineData("UPDATE_COMPLETE_CLEANUP_IN_PROGRESS")]
        [InlineData("UPDATE_IN_PROGRESS")]
        [InlineData("UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS")]
        [InlineData("UPDATE_ROLLBACK_IN_PROGRESS")]
        public async Task ShouldBeBusyWhenCreateOrUpdateIsInProgress(string stackStatus)
        {
            var operations = this.SetupCloudFormationOperations(stackStatus);

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.Busy);
        }

        /// <summary>
        /// Tests the readiness should be not found when stack does not exist.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldBeNotFoundWhenStackDoesNotExist()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCf = new Mock<IAmazonCloudFormation>();
            mockCf.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).Throws(
                new AmazonCloudFormationException($"Stack with id {StackName} does not exist"));
            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);

            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.NotFound);
        }

        /// <summary>
        /// Tests the readiness should be not found when stack recently deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldBeNotFoundWhenStackRecentlyDeleted()
        {
            var operations = this.SetupCloudFormationOperations("DELETE_COMPLETE");

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.NotFound);

        }

        /// <summary>
        /// Tests the readiness should be deleting when stack delete in progress.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldBeDeletingWhenStackDeleteInProgress()
        {
            var operations = this.SetupCloudFormationOperations("DELETE_IN_PROGRESS");

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.Deleting);
        }

        /// <summary>
        /// Tests the stack should be ready when previous stack operation succeeded.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
        /// <returns>A <see cref="Task"/></returns>
        [Theory]
        [InlineData("CREATE_COMPLETE")]
        [InlineData("IMPORT_COMPLETE")]
        [InlineData("ROLLBACK_COMPLETE")]
        [InlineData("UPDATE_COMPLETE")]
        [InlineData("UPDATE_ROLLBACK_COMPLETE")]
        public async Task ShouldBeReadyWhenPreviousStackOperationSucceeded(string stackStatus)
        {
            var operations = this.SetupCloudFormationOperations(stackStatus);

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.Ready);
        }

        /// <summary>
        /// Tests the stack should be broken when previous stack operation failed.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
        /// <returns>A <see cref="Task"/></returns>
        [Theory]
        [InlineData("CREATE_FAILED")]
        [InlineData("IMPORT_ROLLBACK_FAILED")]
        [InlineData("ROLLBACK_FAILED")]
        [InlineData("UPDATE_ROLLBACK_FAILED")]
        public async Task ShouldBeBrokenWhenPreviousStackOperationFailed(string stackStatus)
        {
            var operations = this.SetupCloudFormationOperations(stackStatus);

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.Broken);
        }

        /// <summary>
        /// Tests the stack should be delete failed when previous stack delete failed.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        [Fact]
        public async Task ShouldBeDeleteFailedWhenPreviousStackDeleteFailed()
        {
            var operations = this.SetupCloudFormationOperations("DELETE_FAILED");

            (await operations.GetStackOperationalStateAsync(StackName)).Should().Be(StackOperationalState.DeleteFailed);
        }

        /// <summary>
        /// Sets up the cloud formation operations for most tests.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
        /// <returns>A <see cref="CloudFormationOperations"/> for the test</returns>
        private CloudFormationOperations SetupCloudFormationOperations(string stackStatus)
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
                                                 StackName = StackName, StackStatus = StackStatus.FindValue(stackStatus)
                                             }
                                     }
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCf.Object);

            var operations = new CloudFormationOperations(mockClientFactory.Object, mockContext.Object);
            return operations;
        }
    }
}