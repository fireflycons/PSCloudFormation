namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    internal interface IReferencedItem
    {
        /// <summary>
        /// Gets a regex to match this item by ARN value.
        /// </summary>
        /// <value>
        /// The ARN regex.
        /// </value>
        Regex ArnRegex { get; }

        /// <summary>
        /// Gets a value indicating whether this referenced item is scalar.
        /// For <c>list(...)</c> input variables, this will be <c>false</c>; else <c>true</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is scalar; otherwise, <c>false</c>.
        /// </value>
        bool IsScalar { get; }

        /// <summary>
        /// Gets the value to use when searching state schema for a value to replace with a reference.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        IList<string> ListIdentity { get; }

        /// <summary>
        /// Gets the value to use when searching state schema for a value to replace with a reference.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        string ScalarIdentity { get; }
    }
}