namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// Allows push/pop current directory in a using block
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class WorkingDirectoryContext : IDisposable
    {
        /// <summary>
        /// The previous directory
        /// </summary>
        private readonly string previousDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkingDirectoryContext"/> class.
        /// </summary>
        /// <param name="newDirectory">The new directory.</param>
        public WorkingDirectoryContext(string newDirectory)
        {
            this.previousDirectory = Directory.GetCurrentDirectory();

            if (!Directory.Exists(newDirectory))
            {
                Directory.CreateDirectory(newDirectory);
            }

            Directory.SetCurrentDirectory(newDirectory);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Directory.SetCurrentDirectory(this.previousDirectory);
        }
    }
}