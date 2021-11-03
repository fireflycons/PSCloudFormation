using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.State
{
    using Newtonsoft.Json.Linq;

    internal class DirectReference : Reference
    {
        public DirectReference(string resourceAddress)
        : base (resourceAddress)
        {
        }

        public override string ReferenceExpression => $"{this.ObjectAddress}.id";
    }
}
