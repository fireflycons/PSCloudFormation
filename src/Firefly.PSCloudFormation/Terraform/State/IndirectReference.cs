namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;

    internal class IndirectReference : Reference
    {
        public IndirectReference()
            : base("TODO")
        {
        }

        public override string ReferenceExpression => throw new NotImplementedException();
    }
}