namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;

    [Flags]
    internal enum QuirkType
    {
        /// <summary>
        /// No quirks
        /// </summary>
        None = 0,

        /// <summary>
        /// Mapping key should be emitted without equals
        /// </summary>
        KeyWithoutEquals,

        /// <summary>
        /// If a sequence follows the key, it should be omitted.
        /// </summary>
        OmitOuterSequence
    }
}