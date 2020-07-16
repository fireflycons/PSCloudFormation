namespace Firefly.CloudFormation.Tests.Unit.Utils
{
    using System.Collections.Generic;

    using Amazon;

    using Firefly.CloudFormation.Utils;

    using Moq;

    public static class TestHelpers
    {
        public const string AccountId = "123456789012";

        public static readonly RegionEndpoint Region = RegionEndpoint.EUWest1;

        public static readonly string RegionName = Region.SystemName;

        internal static Mock<ICloudFormationContext> GetContextMock(TestLogger logger)
        {
            var mockContext = new Mock<ICloudFormationContext>();

            mockContext.Setup(c => c.Logger).Returns(logger);
            mockContext.Setup(c => c.Region).Returns(Region);

            return mockContext;
        }

        internal static Mock<IAwsClientFactory> GetClientFactoryMock()
        {
            var mock = new Mock<IAwsClientFactory>();

            return mock;
        }
    }
}