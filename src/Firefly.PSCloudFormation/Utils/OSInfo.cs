namespace Firefly.PSCloudFormation.Utils
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Object to determine operating system - Windows or otherwise
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class OSInfo : IOSInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OSInfo"/> class.
        /// </summary>
        public OSInfo()
        {
            // For the purpose of Python venv directory structure, it's Windows or not Windows.
            this.OSPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows :
                              RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : OSPlatform.Linux;
        }

        /// <summary>
        /// Gets the os platform.
        /// </summary>
        /// <value>
        /// The os platform.
        /// </value>
        // ReSharper disable once StyleCop.SA1650
        public OSPlatform OSPlatform { get; }
    }
}