namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// Extension methods for <see cref="FileInfo"/>
    /// </summary>
    public static class FileInfoExtensions
    {
        /// <summary>
        /// Compute MD5 of this file.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>MD5 hash of the file.</returns>
        /// <exception cref="ArgumentNullException">self is null</exception>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public static string MD5(this FileSystemInfo self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(self.FullName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }
    }
}