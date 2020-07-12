namespace Firefly.PSCloudFormation.Tests.Unit.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// Disposable temporary directory structure
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TempDirectory : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempDirectory"/> class.
        /// </summary>
        public TempDirectory()
        {
            this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(this.Path);
        }

        /// <summary>
        /// Gets the path to the temp directory.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Performs an implicit conversion from <see cref="TempDirectory"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="self">This instance of <see cref="TempDirectory"/>.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(TempDirectory self) => self.Path;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(this.Path))
            {
                try
                {
                    Directory.Delete(this.Path, true);
                }
                catch
                {
                    // Swallow this - it's only in TEMP after all
                }
            }
        }
    }
}