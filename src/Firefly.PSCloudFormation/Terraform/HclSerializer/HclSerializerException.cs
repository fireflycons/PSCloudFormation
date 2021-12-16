namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception type thrown for most errors that happen during serialization and generation og HCL. May wrap additional exceptions.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class HclSerializerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="resourceName">Name of the resource being serialized.</param>
        /// <param name="resourceType">Type of the resource being serialized.</param>
        /// <param name="message">The message that describes the error.</param>
        public HclSerializerException(string resourceName, string resourceType, string message)
            : base(message)
        {
            this.ResourceName = resourceName;
            this.ResourceType = resourceType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="resourceName">Name of the resource being serialized.</param>
        /// <param name="resourceType">Type of the resource being serialized.</param>
        /// <param name="inner">The inner.</param>
        public HclSerializerException(string message, string resourceName, string resourceType, Exception inner)
            : base(message, inner)
        {
            this.ResourceName = resourceName;
            this.ResourceType = resourceType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected HclSerializerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="HclSerializerException"/> class from being created.
        /// </summary>
        private HclSerializerException()
        {
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                if (this.ResourceType == null || this.ResourceName == null)
                {
                    return base.Message;
                }

                return $"Resource \"{this.ResourceType}.{this.ResourceName}\": {base.Message}";
            }
        }

        /// <summary>
        /// Gets the name of the resource being serialized.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        public string ResourceName { get; }

        /// <summary>
        /// Gets the type of the resource being serialized.
        /// </summary>
        /// <value>
        /// The type of the resource.
        /// </value>
        public string ResourceType { get; }
    }
}