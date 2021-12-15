namespace Firefly.PSCloudFormation.Tests.Integration.Utils
{
    class TestTimestampGenerator : ITimestampGenerator
    {
        public string GenerateTimestamp()
        {
            return "20200101000000000";
        }
    }
}
