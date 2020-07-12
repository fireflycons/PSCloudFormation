namespace Firefly.PSCloudFormation
{
    using System.Management.Automation;

    /// <summary>
    /// Concrete implementation for PowerShell
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.IPathResolver" />
    // ReSharper disable once InconsistentNaming
    internal class PSPathResolver : IPathResolver
    {
        /// <summary>
        /// The path intrinsic
        /// </summary>
        private readonly PathIntrinsics pathIntrinsics;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSPathResolver"/> class.
        /// </summary>
        /// <param name="sessionState">State of the session.</param>
        public PSPathResolver(SessionState sessionState)
        {
            this.pathIntrinsics = sessionState.Path;
        }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>
        /// Absolute path.
        /// </returns>
        public string ResolvePath(string relativePath)
        {
            return this.pathIntrinsics.GetUnresolvedProviderPathFromPSPath(relativePath);
        }

        /// <summary>
        /// Sets the location in the file system.
        /// </summary>
        /// <param name="path">The path to set as current.</param>
        public void SetLocation(string path)
        {
            this.pathIntrinsics.SetLocation(path);
        }

        /// <summary>
        /// Gets the current location in the file system
        /// </summary>
        /// <returns>Current file system location</returns>
        public string GetLocation()
        {
            return this.pathIntrinsics.CurrentLocation.Path;
        }
    }
}