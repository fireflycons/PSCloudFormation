namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.IO;

    public class TestPathResolver : IPathResolver
    {
        /// <summary>
        /// Gets the current location in the file system
        /// </summary>
        /// <returns>
        /// Current file system location
        /// </returns>
        public string GetLocation()
        {
            return Directory.GetCurrentDirectory();
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
            return Path.GetFullPath(relativePath);
        }

        /// <summary>
        /// Sets the location in the file system.
        /// </summary>
        /// <param name="path">The path to set as current.</param>
        public void SetLocation(string path)
        {
            Directory.SetCurrentDirectory(path);
        }
    }
}