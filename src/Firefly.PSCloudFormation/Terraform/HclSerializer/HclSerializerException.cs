namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class HclSerializerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        public HclSerializerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public HclSerializerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public HclSerializerException(string message, Exception inner)
            : base(message, inner)
        {
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
    }
}