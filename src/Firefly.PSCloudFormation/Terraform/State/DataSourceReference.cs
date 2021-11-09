using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.State
{
    internal class DataSourceReference : Reference
    {
        public DataSourceReference(string objectAddress)
            : base(objectAddress)
        {
        }

        public override string ReferenceExpression => this.ObjectAddress;
    }
}
