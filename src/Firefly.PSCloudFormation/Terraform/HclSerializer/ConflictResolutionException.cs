namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when conflicting arguments cannot be resolved.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class ConflictResolutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolutionException"/> class.
        /// </summary>
        /// <param name="argument1">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        public ConflictResolutionException(string argument1, string argument2)
        : this($"Unable to resolve conflict between \"{argument1}\" and \"{argument2}\". Please raise an issue.")
        {
            
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ConflictResolutionException"/> class from being created.
        /// </summary>
        private ConflictResolutionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolutionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        private ConflictResolutionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolutionException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        private ConflictResolutionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictResolutionException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected ConflictResolutionException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}