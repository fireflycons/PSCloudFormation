namespace Firefly.PSCloudFormation.Terraform.PlanDeserialization
{
    /// <summary>
    /// Internal representation  of error messages returned by <c>terraform plan</c>
    /// </summary>
    internal enum PlanErrorType
    {
        /// <summary>
        /// A type of HCL parse error.
        /// These must all be fixed before we get onto any other kind of error
        /// thus if we cannot eliminate them all, we have to give up.
        /// Known errors
        /// - <c>jsonencode</c> where key name contains punctuation and is not quoted
        /// </summary>
        MissingAttributeSeparator,

        /// <summary>
        /// A computed attribute such as an ID or ARN is present in the resource definition
        /// </summary>
        UnconfigurableAtribute,

        /// <summary>
        /// The resource attribute key is invalid or unknown.
        /// </summary>
        InvalidOrUnknownKey,

        /// <summary>
        /// A required attribute is absent from the resource definition - not resolvable
        /// </summary>
        MissingRequiredArgument,
        
        /// <summary>
        /// Error as yet unsupported.
        /// If we get to only these and/or MissingAttributeSeparator remaining
        /// after two passes, give up.
        /// </summary>
        Unrecognized
    }
}