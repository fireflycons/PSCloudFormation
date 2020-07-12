namespace Firefly.CloudFormation.Model
{
    using System;
    using System.Runtime.Serialization;

    using Amazon.CloudFormation.Model;

    /// <summary>
    /// Thrown when an error with a stack operation is detected.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class StackOperationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperationException"/> class.
        /// </summary>
        /// <param name="stack">The stack.</param>
        public StackOperationException(Stack stack)
            : base($"Stack '{stack.StackName}': Operation failed. Status is {stack.StackStatus}")
        {
            this.Stack = stack;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperationException"/> class.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <param name="message">The message.</param>
        public StackOperationException(Stack stack, string message)
        : base(message)
        {
            this.Stack = stack;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StackOperationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public StackOperationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperationException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected StackOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public Stack Stack { get; }
    }
}