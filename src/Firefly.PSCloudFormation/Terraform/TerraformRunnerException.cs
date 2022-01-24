namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Runtime.Serialization;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    /// <summary>
    /// Exception raised if an invocation of the terraform executable has non-zero exit code.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class TerraformRunnerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformRunnerException"/> class.
        /// </summary>
        /// <param name="commandTail">The full command being executed.</param>
        /// <param name="exitCode">Exit code of process.</param>
        /// <param name="errors">Number of errors recorded.</param>
        /// <param name="warnings">Number of warnings recorded.</param>
        public TerraformRunnerException(string commandTail, int exitCode, int errors, int warnings)
            : base(FormatBaseMessage(commandTail, exitCode))
        {
            this.ExitCode = exitCode;
            this.Errors = errors;
            this.Warnings = warnings;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="TerraformRunnerException"/> class from being created.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private TerraformRunnerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformRunnerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        // ReSharper disable once UnusedMember.Local
        private TerraformRunnerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HclSerializerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected TerraformRunnerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the number of errors recorded.
        /// </summary>
        public int Errors { get; }

        /// <summary>
        /// Gets the number of warnings recorded.
        /// </summary>
        public int Warnings { get; }

        /// <summary>
        /// Gets the exit code of the terraform process.
        /// </summary>
        public int ExitCode { get; }

        private static string FormatBaseMessage(string commandTail, int exitCode)
        {
            return $"The invocation 'terraform {commandTail}' failed with code {exitCode}";
        }
    }
} 