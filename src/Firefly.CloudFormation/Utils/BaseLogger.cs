namespace Firefly.CloudFormation.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Amazon.CloudFormation;
    using Amazon.CloudFormation.Model;

    /// <summary>
    /// Extension methods for <see cref="ILogger"/>
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        /// <summary>
        /// Regex to identify errors in a stack event
        /// </summary>
        protected static readonly Regex ErrorStatus = new Regex("(ROLLBACK|FAILED)");

        /// <summary>
        /// Gets the width of the stack name column for stack events.
        /// </summary>
        /// <value>
        /// The width of the stack name column.
        /// </value>
        protected int StackNameColumnWidth { get; private set; }

        /// <summary>
        /// Gets the width of the resource name column for stack events.
        /// </summary>
        /// <value>
        /// The width of the resource name column.
        /// </value>
        protected int ResourceNameColumnWidth { get; private set; }

        /// <summary>
        /// Gets the width of the status column for stack events.
        /// </summary>
        /// <value>
        /// The width of the status column.
        /// </value>
        protected int StatusColumnWidth => Math.Max(
            typeof(StackStatus).GetFields().Select(f => f.Name).Max(n => n.Length),
            typeof(ResourceStatus).GetFields().Select(f => f.Name).Max(n => n.Length));

        /// <summary>
        /// Puts an ellipsis on strings longer than maximum length
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxLen">The maximum length.</param>
        /// <returns>Input string with ellipsis if too long.</returns>
        public string EllipsisString(string text, int maxLen)
        {
            if (text.Length > maxLen)
            {
                return text.Substring(0, maxLen - 3) + "...";
            }

            return text;
        }

        /// <summary>
        /// Logs a change set.
        /// The base implementation merely calculates column widths that a concrete implementation can use to render the change set details.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>
        /// Map of change column name to max width required.
        /// </returns>
        public virtual IDictionary<string, int> LogChangeset(DescribeChangeSetResponse changes)
        {
            return GetChangesetColumnWidths(changes.Changes);
        }

        /// <summary>
        /// Sets the width of the stack name column for stack event rendering
        /// </summary>
        /// <param name="width">The width.</param>
        /// <remarks>
        /// Once the template has been parsed, <see cref="CloudFormationRunner" /> checks for <c>AWS::CloudFormation::Stack</c> resources
        /// and make a best-guess at the expected width of column required to display this.
        /// </remarks>
        public void SetStackNameColumnWidth(int width)
        {
            this.StackNameColumnWidth = width;
        }

        /// <summary>
        /// Sets the width of the logical resource name column for stack event rendering
        /// </summary>
        /// <param name="width">The width.</param>
        /// <remarks>
        /// Once the template has been parsed, <see cref="CloudFormationRunner" /> checks for resources
        /// and make a best-guess at the expected width of column required to display this.
        /// </remarks>
        public void SetResourceNameColumnWidth(int width)
        {
            this.ResourceNameColumnWidth = width;
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public abstract void LogError(string message, params object[] args);

        /// <summary>
        /// Log informational message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public abstract void LogInformation(string message, params object[] args);

        /// <summary>
        /// Logs a stack event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <remarks>
        /// Not implemented here as the client may want to do funky things like coloring the output
        /// </remarks>
        public abstract void LogStackEvent(StackEvent @event);

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public abstract void LogWarning(string message, params object[] args);

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public abstract void LogVerbose(string message, params object[] args);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public abstract void LogDebug(string message, params object[] args);

        /// <summary>
        /// Gets the change set column widths.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>Map of property name to column width</returns>
        private static Dictionary<string, int> GetChangesetColumnWidths(IList<Change> changes)
        {
            var columnWidthMap = new Dictionary<string, int>();
            var props = typeof(ResourceChange).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod != null);

            foreach (var prop in props)
            {
                var columnValues = changes.Select(c => c.ResourceChange).Select(resourceChange =>
                    {
                         var val = prop.GetMethod.Invoke(resourceChange, null);
                         return val == null ? string.Empty : val.ToString();
                    }).ToList();

                columnWidthMap.Add(prop.Name, columnValues.Max(v => v.Length));
            }

            return columnWidthMap;
        }
    }
}