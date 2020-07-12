using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.CloudFormation
{
    using System.Linq;
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

    public class UpdateStack
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

        private static readonly List<Change> StockChange = new List<Change>
                                                               {
                                                                   new Change
                                                                       {
                                                                           ResourceChange = new ResourceChange
                                                                                                {
                                                                                                    Action =
                                                                                                        ChangeAction
                                                                                                            .Modify,
                                                                                                    LogicalResourceId =
                                                                                                        "LogicalId",
                                                                                                    PhysicalResourceId =
                                                                                                        "PhysicalId",
                                                                                                    Replacement =
                                                                                                        Replacement
                                                                                                            .False,
                                                                                                    ResourceType =
                                                                                                        "ResourceType"
                                                                                                }
                                                                       }
                                                               };

        private static readonly DescribeStacksResponse ResponseStackCreateComplete = new DescribeStacksResponse
                                                                                         {
                                                                                             Stacks = new List<Stack>
                                                                                                          {
                                                                                                              new
                                                                                                              Stack()
                                                                                                                  {
                                                                                                                      StackName
                                                                                                                          = StackName,
                                                                                                                      StackStatus
                                                                                                                          = StackStatus
                                                                                                                              .CREATE_COMPLETE,
                                                                                                                      Parameters
                                                                                                                          = new
                                                                                                                              List
                                                                                                                              <Parameter
                                                                                                                              >()
                                                                                                                  }
                                                                                                          }
                                                                                         };

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStack"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public UpdateStack(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// If the stack does not exist, update should fail
        /// </summary>
        [Fact]
        public void ShouldFailIfStackDoesNotExist()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);

            var mockCloudFormation = new Mock<IAmazonCloudFormation>();
            mockCloudFormation.Setup(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default)).Throws(
                new AmazonCloudFormationException($"Stack with id {StackName} does not exist"));

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));
            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.UpdateStackAsync(null);

            action.Should().Throw<StackOperationException>().WithMessage($"Stack with id {StackName} does not exist");
        }

        /// <summary>
        /// Update should fail if stack is broken.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
        [Theory]
        [InlineData("CREATE_FAILED")]
        [InlineData("DELETE_FAILED")]
        [InlineData("IMPORT_ROLLBACK_FAILED")]
        [InlineData("ROLLBACK_FAILED")]
        [InlineData("UPDATE_ROLLBACK_FAILED")]
        public void ShouldFailIfStackIsBroken(string stackStatus)
        {
            var expectedMessage = $"Cannot update stack: Current state: * ({stackStatus})";
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.FindValue(stackStatus)
                                                 }
                                         }
                    }).ReturnsAsync(
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

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.UpdateStackAsync(null);

            action.Should().Throw<StackOperationException>().WithMessage(expectedMessage);
        }

        /// <summary>
        /// Update should fail if a stack operation is in progress and WithWaitForInProgressUpdate was not applied to the builder.
        /// </summary>
        /// <param name="stackStatus">The stack status.</param>
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
        public void ShouldFailIfStackBusyAndWaitIsFalse(string stackStatus)
        {
            var expectedMessage = $"Stack is being updated by another process.";
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.FindValue(stackStatus)
                                                 }
                                         }
                    }).ReturnsAsync(
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

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.UpdateStackAsync(null);

            action.Should().Throw<StackOperationException>().WithMessage(expectedMessage);
        }

        /// <summary>
        /// Should fail if stack being deleted by another process.
        /// </summary>
        [Fact]
        public void ShouldFailIfStackBeingDeleted()
        {
            var expectedMessage = $"Stack is being deleted by another process.";
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.DELETE_IN_PROGRESS
                                                 }
                                         }
                    }).ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.DELETE_IN_PROGRESS
                                                 }
                                         }
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.UpdateStackAsync(null);

            action.Should().Throw<StackOperationException>().WithMessage(expectedMessage);
        }

        /// <summary>
        /// If change set reports no changes, then stack should not be modified.
        /// </summary>
        /// <param name="statusReason">The status reason.</param>
        [Theory]
        [InlineData("The submitted information didn't contain changes")]
        [InlineData("No updates are to be performed")]
        public async Task ShouldReturnNoChangeIfChangesetReportsNoChanges(string statusReason)
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete);

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse { Status = ChangeSetStatus.FAILED, StatusReason = statusReason });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            (await runner.UpdateStackAsync(null)).StackOperationResult.Should().Be(StackOperationResult.NoChange);
            logger.ChangeSets.Count.Should().Be(0);
        }

        /// <summary>
        /// If caller set WithChangestOnly, then change set should be displayed and not applied.
        /// </summary>
        [Fact]
        public async Task ShouldCreateChangesetAndNotUpdateStackIfChangesetOnlySpecified()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete);

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).WithChangesetOnly().Build();

            (await runner.UpdateStackAsync(null)).StackOperationResult.Should().Be(StackOperationResult.NoChange);
            logger.InfoMessages.Last().Should().Be("Not updating stack since CreateChangesetOnly = true");
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// If caller's confirmation function returns <c>false</c>, then change set should be displayed and not applied.
        /// </summary>
        [Fact]
        public async Task ShouldCreateChangesetAndNotUpdateStackIfConfirmationFunctionReturnsFalse()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete);

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            (await runner.UpdateStackAsync(_ => false)).StackOperationResult.Should().Be(StackOperationResult.NoChange);
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Should return stack update in progress if wait for in progress update not specified.
        /// </summary>
        [Fact]
        public async Task ShouldReturnStackUpdateInProgressIfWaitForInProgressUpdateNotSpecified()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete);


            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockCloudFormation.SetupSequence(cf => cf.DescribeStackEventsAsync(It.IsAny<DescribeStackEventsRequest>(), default))
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
                                                          Timestamp = DateTime.Now.AddDays(-1)
                                                      }
                                              }
                        });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            (await runner.UpdateStackAsync(_ => true)).StackOperationResult.Should().Be(StackOperationResult.StackUpdateInProgress);
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
            logger.InfoMessages.Any(i => i.Contains($"Updating stack '{StackName}'")).Should().BeTrue();
        }

        /// <summary>
        /// Should throw if another update began while user was reviewing change set.
        /// </summary>
        [Fact]
        public void ShouldThrowIfAnotherUpdateBeganWhileUserWasReviewingChangeset()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.UPDATE_IN_PROGRESS,
                                                     Parameters = new List<Parameter>()
                                                 }
                                         }
                    });

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).Build();

            Func<Task<CloudFormationResult>> action = async() => await runner.UpdateStackAsync(_ => true);

            action.Should().Throw<StackOperationException>().WithMessage("Stack is being modified by another process.");
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ShouldReturnStackUpdatedAndEventsIfWaitForInProgressUpdateSpecified()
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(
                    new DescribeStacksResponse
                        {
                            Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.UPDATE_IN_PROGRESS,
                                                     Parameters = new List<Parameter>()
                                                 }
                                         }
                        }).ReturnsAsync(
                    new DescribeStacksResponse
                        {
                            Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.UPDATE_COMPLETE,
                                                     Parameters = new List<Parameter>()
                                                 }
                                         }
                        });

            mockCloudFormation.SetupSequence(cf => cf.DescribeStackEventsAsync(It.IsAny<DescribeStackEventsRequest>(), default))
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
                                                          Timestamp = DateTime.Now.AddDays(-1)
                                                      }
                                              }
                        })
                .ReturnsAsync(
                    new DescribeStackEventsResponse
                        {
                            StackEvents = new List<StackEvent> { new StackEvent { Timestamp = DateTime.Now.AddSeconds(1) } }
                        }).ReturnsAsync(
                    new DescribeStackEventsResponse
                        {
                            StackEvents = new List<StackEvent> { new StackEvent { Timestamp = DateTime.Now.AddSeconds(2) } }
                        });

            mockCloudFormation.Setup(cf => cf.DescribeStackResourcesAsync(It.IsAny<DescribeStackResourcesRequest>(), default))
                .ReturnsAsync(new DescribeStackResourcesResponse { StackResources = new List<StackResource>() });

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).WithWaitForInProgressUpdate().Build();

            (await runner.UpdateStackAsync(_ => true)).StackOperationResult.Should().Be(StackOperationResult.StackUpdated);
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
            logger.InfoMessages.Any(i => i.Contains($"Updating stack '{StackName}'")).Should().BeTrue();
            logger.StackEvents.Count.Should().BeGreaterThan(0);
        }
        
        /// <summary>
        /// Should throw if update fails.
        /// </summary>
        /// <param name="finalState">The final state.</param>
        [Theory]
        [InlineData("IMPORT_ROLLBACK_COMPLETE")]
        [InlineData("IMPORT_ROLLBACK_FAILED")]
        [InlineData("UPDATE_ROLLBACK_COMPLETE")]
        [InlineData("UPDATE_ROLLBACK_FAILED")]
        public void ShouldThrowIfUpdateFails(string finalState)
        {
            var logger = new TestLogger(this.output);
            var mockClientFactory = TestHelpers.GetClientFactoryMock();
            var mockContext = TestHelpers.GetContextMock(logger);
            var mockCloudFormation = new Mock<IAmazonCloudFormation>();

            mockCloudFormation.SetupSequence(cf => cf.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), default))
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(ResponseStackCreateComplete)
                .ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.UPDATE_IN_PROGRESS,
                                                     Parameters = new List<Parameter>()
                                                 }
                                         }
                    }).ReturnsAsync(
                    new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                                         {
                                             new Stack()
                                                 {
                                                     StackName = StackName, StackStatus = StackStatus.FindValue(finalState),
                                                     Parameters = new List<Parameter>()
                                                 }
                                         }
                    });

            mockCloudFormation.SetupSequence(cf => cf.DescribeStackEventsAsync(It.IsAny<DescribeStackEventsRequest>(), default))
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
                                                          Timestamp = DateTime.Now.AddDays(-1)
                                                      }
                                              }
                        })
                .ReturnsAsync(
                    new DescribeStackEventsResponse
                    {
                        StackEvents = new List<StackEvent> { new StackEvent { Timestamp = DateTime.Now.AddSeconds(1) } }
                    }).
                ReturnsAsync(
                    new DescribeStackEventsResponse
                    {
                        StackEvents = new List<StackEvent> { new StackEvent { Timestamp = DateTime.Now.AddSeconds(2) } }
                    }).
                ReturnsAsync(
                    new DescribeStackEventsResponse
                        {
                            StackEvents = new List<StackEvent>()
                        });

            mockCloudFormation.Setup(cf => cf.CreateChangeSetAsync(It.IsAny<CreateChangeSetRequest>(), default))
                .ReturnsAsync(new CreateChangeSetResponse { Id = "arn:aws:cloudformation:eu-west-1:123456789012:changeset/1234" });

            mockCloudFormation.SetupSequence(cf => cf.DescribeChangeSetAsync(It.IsAny<DescribeChangeSetRequest>(), default))
                .ReturnsAsync(new DescribeChangeSetResponse { Status = ChangeSetStatus.CREATE_IN_PROGRESS })
                .ReturnsAsync(
                    new DescribeChangeSetResponse
                    {
                        Status = ChangeSetStatus.CREATE_COMPLETE,
                        Changes = StockChange
                    });

            mockCloudFormation.Setup(cf => cf.DescribeStackResourcesAsync(It.IsAny<DescribeStackResourcesRequest>(), default))
                .ReturnsAsync(new DescribeStackResourcesResponse { StackResources = new List<StackResource>() });

            mockClientFactory.Setup(f => f.CreateCloudFormationClient()).Returns(mockCloudFormation.Object);

            using var template = new TempFile(EmbeddedResourceManager.GetResourceStream("test-stack.json"));

            var runner = CloudFormationRunner.Builder(mockContext.Object, StackName)
                .WithClientFactory(mockClientFactory.Object).WithTemplateLocation(template.Path).WithWaitForInProgressUpdate().Build();

            Func<Task<CloudFormationResult>> action = async () => await runner.UpdateStackAsync(_ => true);

            action.Should().Throw<StackOperationException>().WithMessage($"Stack '{StackName}': Operation failed. Status is {finalState}");
            logger.ChangeSets.Count.Should().BeGreaterThan(0);
            logger.InfoMessages.Any(i => i.Contains($"Updating stack '{StackName}'")).Should().BeTrue();
            logger.StackEvents.Count.Should().BeGreaterThan(0);
        }
    }
}
