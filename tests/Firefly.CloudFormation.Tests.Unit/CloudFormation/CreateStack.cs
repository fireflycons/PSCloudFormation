namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Model;
    using Firefly.CloudFormation.Tests.Unit.resources;
    using Firefly.CloudFormation.Tests.Unit.Utils;

    using FluentAssertions;

    using Moq;

    using Xunit;
    using Xunit.Abstractions;

    public class CreateStack
    {
        /// <summary>
        /// The stack name
        /// </summary>
        private const string StackName = "test-stack";

        /// <summary>
        /// The stack identifier
        /// </summary>
        private static readonly string StackId =
            $"arn:aws:cloudformation:{TestHelpers.RegionName}:{TestHelpers.AccountId}:stack/test-stack";

        /// <summary>
        /// The output
        /// </summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStack"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public CreateStack(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Should create stack if stack does not exist.
        /// </summary>
        [Fact]
        public async void ShouldCreateStackIfStackDoesNotExist()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCloudFormation = new Mock<IAmazonCloudFormation>();
            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .Throws(new AmazonCloudFormationException($"Stack with id {StackName} does not exist")).ReturnsAsync(
                    new DescribeStacksResponse
                        {
                            Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName,
                                                     StackId = StackId,
                                                     StackStatus = StackStatus.CREATE_COMPLETE
                                                 }
                                         }
                        });

            mockCloudFormation.Setup(cf => cf.CreateStackAsync(It.IsAny<CreateStackRequest>(), default))
                .ReturnsAsync(new CreateStackResponse { StackId = StackId });

            mockCloudFormation.Setup(cf => cf.DescribeStackEventsAsync(It.IsAny<DescribeStackEventsRequest>(), default))
                .ReturnsAsync(
                    new DescribeStackEventsResponse
                        {
                            StackEvents = new List<StackEvent>
                                              {
                                                  new StackEvent
                                                      {
                                                          StackName = StackName,
                                                          StackId = StackId,
                                                          ResourceStatus = ResourceStatus.CREATE_COMPLETE,
                                                          Timestamp = DateTime.Now.AddSeconds(1)
                                                      }
                                              }
                        });

            mockCloudFormation.Setup(cf => cf.DescribeStackResourcesAsync(It.IsAny<DescribeStackResourcesRequest>(), default))
                .ReturnsAsync(new DescribeStackResourcesResponse { StackResources = new List<StackResource>() });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));
            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object)
                .WithFollowOperation()
                .WithTemplateLocation(template.Path)
                .Build();

            (await runner.CreateStackAsync()).StackOperationResult.Should().Be(StackOperationResult.StackCreated);
            logger.StackEvents.Count.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Should fail to create if stack exists.
        /// </summary>
        [Fact]
        public void ShouldFailIfStackExists()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCloudFormation = new Mock<IAmazonCloudFormation>();
            mockCloudFormation.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).ReturnsAsync(
                new DescribeStacksResponse { Stacks = new List<Stack> { new Stack { StackName = StackName } } });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));
            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object)
                .WithTemplateLocation(template.Path)
                .Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.CreateStackAsync();

            action.Should().Throw<StackOperationException>().WithMessage($"Stack {StackName} already exists");
        }
    }
}