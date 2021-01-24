namespace Firefly.PSCloudFormation.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Extension methods for <see cref="DirectoryInfo"/>
    /// </summary>
    public static class DirectoryInfoExtensions
    {
        /// <summary>
        /// Compute MD5 of all files in this directory sub-tree
        /// </summary>
        /// <param name="self">The self.</param>
        /// <returns>Computed MD5</returns>
        /// <exception cref="ArgumentNullException">self is null</exception>
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static string MD5(this DirectoryInfo self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            var md5List = new List<string>();

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                foreach (var file in Directory.EnumerateFiles(self.FullName, "*", SearchOption.AllDirectories))
                {
                    using (var stream = File.OpenRead(file))
                    {
                        md5List.Add(
                            BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty)
                                .ToLowerInvariant());
                    }
                }

                return BitConverter
                    .ToString(md5.ComputeHash(Encoding.ASCII.GetBytes(string.Join(string.Empty, md5List))))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Creates if not exists.
        /// </summary>
        /// <param name="self">The self.</param>
        public static void CreateIfNotExists(this DirectoryInfo self)
        {
            if (!self.Exists)
            {
                Directory.CreateDirectory(self.FullName);
            }
        }
    }
}