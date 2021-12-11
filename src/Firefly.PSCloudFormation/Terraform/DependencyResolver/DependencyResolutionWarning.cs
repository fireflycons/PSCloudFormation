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
    [Serializable]
    internal abstract class DependencyResolutionWarning : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolutionWarning"/> class.
        /// </summary>
        /// <param name="intrinsic">The intrinsic being warned about.</param>
        /// <param name="containingResource">The resource that contains the errant intrinsic.</param>
        /// <param name="location">The the AWS property path to the intrinsic being warned about.</param>
        protected DependencyResolutionWarning(
            IIntrinsic intrinsic,
            CloudFormationResource containingResource,
            PropertyPath location)
            : base(string.Empty)
        {
            this.Location = location.Clone();
            this.Intrinsic = intrinsic;
            this.ContainingResource = containingResource;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolutionWarning"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        protected DependencyResolutionWarning(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolutionWarning"/> class.
        /// </summary>
        protected DependencyResolutionWarning()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolutionWarning"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected DependencyResolutionWarning(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolutionWarning"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected DependencyResolutionWarning(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the resource that contains the errant intrinsic.
        /// </summary>
        /// <value>
        /// The containing resource.
        /// </value>
        public CloudFormationResource ContainingResource { get; }

        /// <summary>
        /// Gets the intrinsic being warned about.
        /// </summary>
        /// <value>
        /// The intrinsic.
        /// </value>
        public IIntrinsic Intrinsic { get; }

        /// <summary>
        /// Gets the AWS property path to the intrinsic being warned about.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public PropertyPath Location { get; }

        /// <summary>
        /// Gets the name of the AWS resource formatted for exception message.
        /// </summary>
        /// <value>
        /// The name of the AWS resource.
        /// </value>
        protected string AwsResourceName =>
            $"\"{this.ContainingResource.LogicalResourceId}\" ({this.ContainingResource.ResourceType})";
    }
}