namespace Firefly.CloudFormation.Tests.Unit.Utils
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation.Utils;

    using Xunit.Abstractions;

    /// <summary>
    /// Logging implementation for tests
    /// </summary>
    /// <seealso cref="ILogger" />
    public class TestLogger : BaseLogger
    {
        /// <summary>
        /// The XUnit output
        /// </summary>
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLogger"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public TestLogger(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Gets the buffer of change set messages.
        /// </summary>
        /// <value>
        /// The change sets.
        /// </value>
        public List<DescribeChangeSetResponse> ChangeSets { get; } = new List<DescribeChangeSetResponse>();

        /// <summary>
        /// Gets the buffer of error messages.
        /// </summary>
        /// <value>
        /// The error messages.
        /// </value>
        public List<string> ErrorMessages { get; } = new List<string>();

        /// <summary>
        /// Gets the buffer of information messages.
        /// </summary>
        /// <value>
        /// The information messages.
        /// </value>
        public List<string> InfoMessages { get; } = new List<string>();

        /// <summary>
        /// Gets the buffer of stack events.
        /// </summary>
        /// <value>
        /// The stack events.
        /// </value>
        public List<StackEvent> StackEvents { get; } = new List<StackEvent>();

        /// <summary>
        /// Gets the buffer of warning messages.
        /// </summary>
        /// <value>
        /// The warning messages.
        /// </value>
        public List<string> WarningMessages { get; } = new List<string>();

        /// <summary>
        /// Logs a change set.
        /// </summary>
        /// <param name="changes">The changes.</param>
        public override IDictionary<string, int> LogChangeset(DescribeChangeSetResponse changes)
        {
            this.ChangeSets.Add(changes);
            return base.LogChangeset(changes);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogDebug(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            this.output.WriteLine("DEBUG:  " + msg);
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogError(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            this.ErrorMessages.Add(msg);

            this.output.WriteLine("ERROR: " + message);
        }

        /// <summary>
        /// Log informational message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogInformation(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            this.InfoMessages.Add(msg);

            this.output.WriteLine($"INFO:  {msg}");
        }

        /// <summary>
        /// Logs a stack event.
        /// </summary>
        /// <param name="event">The event.</param>
        public override void LogStackEvent(StackEvent @event)
        {
            this.StackEvents.Add(@event);
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogVerbose(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            this.output.WriteLine("VERBOSE:  " + msg);
        }

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogWarning(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            this.WarningMessages.Add(msg);

            this.output.WriteLine("WARN:  " + msg);
        }
    }
}