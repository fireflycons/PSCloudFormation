namespace Firefly.PSCloudFormation.Tests.Unit.Utils
{
    using Firefly.CloudFormation.Utils;

    class TestTimestampGenerator : ITimestampGenerator
    {
        public string GenerateTimestamp()
        {
            return "20200101000000000";
        }
    }
}
