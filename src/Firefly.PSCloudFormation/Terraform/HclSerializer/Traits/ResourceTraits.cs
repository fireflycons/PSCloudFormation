namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Traits
{
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.EC2;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.S3;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits.VPC;

    /// <summary>
    /// Describes traits for the given resource.
    /// The state file contains much that doesn't directly translate to HCL, and this sorts that out.
    /// </summary>
    internal class ResourceTraits
    {
        /// <summary>
        /// Map-style state attributes that should be omitted as key-value rather than block
        /// </summary>
        private static readonly List<string> CommonNonBlockTypeAttributes = new List<string> { "tags" };

        /// <summary>
        /// State attributes that should be omitted for all resources.
        /// </summary>
        private static readonly List<string> CommonUnconfigurableAttributes =
            new List<string>
                {
                    "arn",
                    "id",
                    "create_date",
                    "unique_id",
                    "tags_all",
                    "timeouts"
                };

        /// <summary>
        /// Gets default values for attributes that are <c>null</c> in the state file.
        /// </summary>
        public Dictionary<string, object> DefaultValues => this.ResourceDefaultValues;

        /// <summary>
        /// Gets the list of state attributes that should not be emitted as HCL
        /// </summary>
        /// <value>
        /// The ignored attributes.
        /// </value>
        public List<string> IgnoredAttributes =>
            CommonUnconfigurableAttributes.Concat(this.ResourceUnconfigurableAttributes).ToList();

        /// <summary>
        /// Gets the mapping style attributes that are not block definitions
        /// </summary>
        /// <value>
        /// The non block type attributes.
        /// </value>
        public List<string> NonBlockTypeAttributes =>
            CommonNonBlockTypeAttributes.Concat(this.ResourceNonBlockTypeAttributes).ToList();

        /// <summary>
        /// Gets list of attributes that must be present for the resource in generated HCL.
        /// </summary>
        public List<string> RequiredAttributes => this.ResourceRequiredAttributes;

        /// <summary>
        /// Gets the resource specific default values for <c>null</c> attributes in the state file.
        /// </summary>
        public virtual Dictionary<string, object> ResourceDefaultValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the mapping style attributes that are not block definitions
        /// </summary>
        /// <value>
        /// The non block type attributes.
        /// </value>
        public virtual List<string> ResourceNonBlockTypeAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets the resource specific attributes where terraform validate will error with <c>Incorrect attribute value type</c>
        /// where an attribute is required, even if null or empty.
        /// </summary>
        public virtual List<string> ResourceRequiredAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets the resource specific attributes where terraform validate will error with <c>unconfigurable attribute</c>. 
        /// </summary>
        /// <value>
        /// The resource ignored attributes.
        /// </value>
        public virtual List<string> ResourceUnconfigurableAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets traits class for current resource type.
        /// </summary>
        /// <param name="resourceType">Type of the resource being serialized.</param>
        /// <returns>Resource specific <see cref="ResourceTraits"/></returns>
        public static ResourceTraits GetTraits(string resourceType)
        {
            switch (resourceType)
            {
                case "aws_instance":

                    return new AwsInstanceTraits();

                case "aws_s3_bucket":

                    return new AwsS3BucketTraits();

                case "aws_s3_bucket_policy":

                    return new AwsS3BucketPolicyTraits();

                case "aws_security_group":

                    return new AwsSecurityGroupTraits();

                default:

                    return new ResourceTraits();
            }
        }

        /// <summary>
        /// Applies any default value to the given scalar if its current value is <c>null</c>.
        /// </summary>
        /// <param name="currentPath">Current attribute path.</param>
        /// <param name="scalar">The scalar to check</param>
        /// <returns>Original scalar if unchanged, else new scalar with default value set.</returns>
        public Scalar ApplyDefaultValue(string currentPath, Scalar scalar)
        {
            if (this.DefaultValues.ContainsKey(currentPath) && scalar.Value == null)
            {
                var newValue = this.DefaultValues[currentPath];
                return new Scalar(newValue, newValue is string);
            }

            return scalar;
        }

        /// <summary>
        /// Determines wither a null or empty attribute should still be emitted.
        /// </summary>
        /// <param name="currentPath">Current attribute path.</param>
        /// <returns><c>true</c> if attribute should be emitted; else <c>false</c></returns>
        public bool ShouldEmitAttribute(string currentPath)
        {
            return this.RequiredAttributes.Contains(currentPath) || this.DefaultValues.ContainsKey(currentPath);
        }
    }
}