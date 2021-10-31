using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal abstract class CollectionStart : HclEvent
    {
        public override int NestingIncrease => 1;
    }
}
