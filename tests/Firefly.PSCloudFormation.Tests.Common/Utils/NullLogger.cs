namespace Firefly.PSCloudFormation.Tests.Common.Utils
{
    using System.Collections.Generic;

    using Amazon.CloudFormation.Model;

    using Firefly.CloudFormation;

    public class NullLogger : ILogger
    {
        /// <inheritdoc />
        public IDictionary<string, int> LogChangeset(DescribeChangeSetResponse changes)
        {
            return new Dictionary<string, int>();
        }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args)
        {
        }

        /// <inheritdoc />
        public void LogError(string message, params object[] args)
        {
        }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args)
        {
        }

        /// <inheritdoc />
        public void LogStackEvent(StackEvent @event)
        {
        }

        /// <inheritdoc />
        public void LogVerbose(string message, params object[] args)
        {
        }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args)
        {
        }

        /// <inheritdoc />
        public void SetResourceNameColumnWidth(int width)
        {
        }

        /// <inheritdoc />
        public void SetStackNameColumnWidth(int width)
        {
        }
    }
}