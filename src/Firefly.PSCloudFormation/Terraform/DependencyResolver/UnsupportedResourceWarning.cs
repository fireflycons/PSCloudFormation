namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System;
    using System.Runtime.Serialization;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Utils;

    /// <summary>
    /// Thrown internally when a complete resolution cannot be found for a given intrinsic.
    /// Where this occurs, the HCL attribute will be output with its current value from state, i.e. unresolved.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    internal class UnsupportedResourceWarning : DependencyResolutionWarning
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedResourceWarning"/> class.
        /// </summary>
        /// <param name="intrinsic">The intrinsic being warned about.</param>
        /// <param name="containingResource">The resource that contains the errant intrinsic.</param>
        /// <param name="location">The the AWS property path to the intrinsic being warned about.</param>
        public UnsupportedResourceWarning(
            IIntrinsic intrinsic,
            CloudFormationResource containingResource,
            PropertyPath location)
            : base(intrinsic, containingResource, location)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedResourceWarning"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        protected UnsupportedResourceWarning(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedResourceWarning"/> class.
        /// </summary>
        protected UnsupportedResourceWarning()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedResourceWarning"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected UnsupportedResourceWarning(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedResourceWarning"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected UnsupportedResourceWarning(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message =>
            $"Resource '{this.ContainingResource.LogicalResourceId}' ({this.ContainingResource.ResourceType}): Unable to create reference to a resource not supported by Terraform at {this.Location.Path}. HCL will contain current attribute value.";
    }
}