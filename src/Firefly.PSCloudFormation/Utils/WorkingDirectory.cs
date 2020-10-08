namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.IO;

    using Firefly.CloudFormation;

    /// <summary>
    /// A temporary directory used for packaging
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class WorkingDirectory : IDisposable
    {
        /// <summary>
        /// The logger for error messages
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The working directory
        /// </summary>
        private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkingDirectory"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public WorkingDirectory(ILogger logger)
        {
            this.logger = logger;
            Directory.CreateDirectory(this.workingDirectory);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="WorkingDirectory"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>
        /// The result of the conversion - being the directory path.
        /// </returns>
        public static implicit operator string(WorkingDirectory self)
        {
            return self.workingDirectory;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(this.workingDirectory))
            {
                try
                {
                    Directory.Delete(this.workingDirectory, true);
                }
                catch (Exception e)
                {
                    this.logger?.LogWarning(
                        $"Cannot remove workspace directory '{this.workingDirectory}': {e.Message}");
                }
            }
        }
    }
}