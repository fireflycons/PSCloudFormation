namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Async file IO helpers
    /// </summary>
    internal class AsyncFileHelpers
    {
        /// <summary>
        /// The default buffer size
        /// </summary>
        private const int DefaultBufferSize = 4096;

        /// <summary>
        /// File accessed asynchronous reading and sequentially from beginning to end.
        /// </summary>
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        /// <summary>
        /// The default encoding
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        /// <summary>
        /// Reads all lines asynchronous.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>List of lines read.</returns>
        public static async Task<IEnumerable<string>> ReadAllLinesAsync(string filePath)
        {
            var lines = new List<string>();

            using (var sourceStream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       DefaultBufferSize,
                       DefaultOptions))
            using (var reader = new StreamReader(sourceStream, DefaultEncoding))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }

                return lines;
            }
        }

        /// <summary>
        /// Reads all text asynchronous.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>String containing all text.</returns>
        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            using (var sourceStream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read,
                       DefaultBufferSize,
                       DefaultOptions))
            using (var reader = new StreamReader(sourceStream, DefaultEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Writes all text asynchronous.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="text">The text.</param>
        /// <returns>Task to await.</returns>
        public static async Task WriteAllTextAsync(string filePath, string text)
        {
            byte[] encodedText = DefaultEncoding.GetBytes(text);

            using (FileStream sourceStream = new FileStream(
                       filePath,
                       FileMode.Append,
                       FileAccess.Write,
                       FileShare.None,
                       DefaultBufferSize,
                       true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
    }
}