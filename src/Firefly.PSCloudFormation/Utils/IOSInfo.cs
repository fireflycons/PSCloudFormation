namespace Firefly.PSCloudFormation.Utils
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interface to operating system info
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal interface IOSInfo
    {
        /// <summary>
        /// Gets the os platform.
        /// </summary>
        /// <value>
        /// The os platform.
        /// </value>
        // ReSharper disable once StyleCop.SA1650
        OSPlatform OSPlatform { get; }
    }
}