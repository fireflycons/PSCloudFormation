namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;

    internal class Quirks
    {
        public static Quirks GetQuirks(string resourceType)
        {
            switch (resourceType)
            {
                case "aws_iam_role":

                    return new IamRoleQuirks();

                default:

                    return new Quirks();
            }
        }

        public virtual QuirkType GetQuirksForMappingKey(string key)
        {
            return QuirkType.None;
        }
    }

    internal class IamRoleQuirks : Quirks
    {
        private static readonly Dictionary<string, QuirkType> MappingKeyQuirkTypes =
            new Dictionary<string, QuirkType> { { "inline_policy", QuirkType.KeyWithoutEquals | QuirkType.OmitOuterSequence } };

        public override QuirkType GetQuirksForMappingKey(string key)
        {
            return MappingKeyQuirkTypes.ContainsKey(key) ? MappingKeyQuirkTypes[key] : base.GetQuirksForMappingKey(key);
        }
    }
}