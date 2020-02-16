namespace PSCloudFormation.Exceptions
{
    using System;
    using Amazon.CloudFormation;

    public class CloudFormationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="stackArn">The ARN of the failing stack.</param>
        /// <param name="status">Status of failed stack.</param>
        public CloudFormationException(string message, string stackArn, StackStatus status)
            :base(message)
        {
            this.StackStatus = status;
            this.Arn = stackArn;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        protected CloudFormationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected CloudFormationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFormationException"/> class.
        /// </summary>
        protected CloudFormationException()
        {
        }

        public String Arn { get; private set; }

        public StackStatus StackStatus { get; private set; }
    }
}