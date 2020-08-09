namespace Firefly.PSCloudFormation
{
    /// <summary>
    /// Interface to abstract away the PowerShell path intrinsic to enable unit testing
    /// </summary>
    internal interface IPathResolver
    {
        /// <summary>
        /// Gets the current location in the file system
        /// </summary>
        /// <returns>Current file system location</returns>
        string GetLocation();

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>Absolute path.</returns>
        string ResolvePath(string relativePath);

        /// <summary>
        /// Sets the location in the file system.
        /// </summary>
        /// <param name="path">The path to set as current.</param>
        void SetLocation(string path);
    }
}