namespace Firefly.PSCloudFormation
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception thrown by errors in packaging process
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class PackagerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerException"/> class.
        /// </summary>
        public PackagerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PackagerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public PackagerException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected PackagerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}