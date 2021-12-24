namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
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
        /// Gets the default encoding.
        /// </summary>
        /// <value>
        /// The default encoding.
        /// </value>
        public static Encoding DefaultEncoding { get; } = new UTF8Encoding(false);

        /// <summary>
        /// Opens an existing file for async reading.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>A read-only FileStream on the specified path.</returns>
        public static FileStream OpenReadAsync(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                DefaultBufferSize,
                DefaultOptions);
        }

        /// <summary>
        /// Opens an existing file or creates a new file for async writing.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>An unshared FileStream object on the specified path with Write access.</returns>
        public static FileStream OpenWriteAsync(string filePath)
        {
            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize, true);
        }

        /// <summary>
        /// Opens an existing file for async append.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>An unshared FileStream object on the specified path with Write access.</returns>
        public static FileStream OpenAppendAsync(string filePath)
        {
            return new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, DefaultBufferSize, true);
        }

        /// <summary>
        /// Reads all lines asynchronous.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>List of lines read.</returns>
        public static async Task<IEnumerable<string>> ReadAllLinesAsync(string filePath)
        {
            var lines = new List<string>();

            using (var sourceStream = OpenReadAsync(filePath))
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
            using (var sourceStream = OpenReadAsync(filePath))
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

            using (FileStream sourceStream = OpenWriteAsync(filePath))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
    }
}