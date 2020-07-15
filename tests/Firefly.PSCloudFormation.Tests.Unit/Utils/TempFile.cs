namespace Firefly.PSCloudFormation.Tests.Unit.Utils
{
    using System;
    using System.Diagnostics;
    using System.IO;

    [DebuggerDisplay("{Path}")]
    public class TempFile : IDisposable
    {
        private readonly string filePath = System.IO.Path.GetTempFileName();

        public TempFile(int sizeInBytes)
        {
            using var s = File.OpenWrite(this.filePath);
            s.Write(new byte[sizeInBytes], 0, sizeInBytes);
        }

        public TempFile(Stream resourceStream)
        {
            using var s = File.OpenWrite(this.filePath);
            resourceStream.CopyTo(s);
        }

        public string Path => this.filePath;

        public void Dispose()
        {
            if (File.Exists(this.filePath))
            {
                File.Delete(this.filePath);
            }
        }
    }
}