namespace Firefly.CloudFormation.Utils
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    /// <summary>
    /// Interface to which all messages and stack events are sent.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Log informational message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Logs a stack event.
        /// </summary>
        /// <param name="event">The event.</param>
        void LogStackEvent(StackEvent @event);

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogVerbose(string message, params object[] args);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Logs a change set.
        /// The base implementation merely calculates column widths that a concrete implementation can use to render the change set details.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>Map of change column name to max width required.</returns>
        IDictionary<string, int> LogChangeset(DescribeChangeSetResponse changes);

        /// <summary>
        /// Sets the width of the stack name column for stack event rendering
        /// </summary>
        /// <param name="width">The width.</param>
        /// <remarks>
        /// Once the template has been parsed, <see cref="CloudFormationRunner"/> checks for <c>AWS::CloudFormation::Stack</c> resources
        /// and make a best-guess at the expected width of column required to display this.
        /// </remarks>
        void SetStackNameColumnWidth(int width);

        /// <summary>
        /// Sets the width of the logical resource name column for stack event rendering
        /// </summary>
        /// <param name="width">The width.</param>
        /// <remarks>
        /// Once the template has been parsed, <see cref="CloudFormationRunner"/> checks for resources
        /// and make a best-guess at the expected width of column required to display this.
        /// </remarks>
        void SetResourceNameColumnWidth(int width);
    }
}