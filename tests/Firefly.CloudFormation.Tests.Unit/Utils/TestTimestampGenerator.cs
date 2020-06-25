using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.CloudFormation.Tests.Unit.Utils
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
