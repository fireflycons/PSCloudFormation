namespace Firefly.PSCloudFormation.Tests.Common.Utils
{
    class TestTimestampGenerator : ITimestampGenerator
    {
        public string GenerateTimestamp()
        {
            return "20200101000000000";
        }
    }
}
