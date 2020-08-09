namespace Firefly.PSCloudFormation.Tests.Unit.Resources
{
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    public class EmbeddedResourceManager
    {
        private static readonly string ResourceNamespace = typeof(IResourceLocator).Namespace;

        private static readonly Assembly ThisAssembly = Assembly.GetExecutingAssembly();

        public static Stream GetResourceStream(string name)
        {
            var s =  ThisAssembly.GetManifestResourceStream($"{ResourceNamespace}.{name}");
            return s;
        }

        public static string GetResourceString(string name)
        {
            using var sr = new StreamReader(GetResourceStream(name));
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Recursively download all resources from a resource folder of embedded resources to a temporary directory
        /// </summary>
        /// <param name="resourceFolder">The resource folder.</param>
        /// <returns>A <c>TempDirectory</c> containing the downloaded resources</returns>
        public static TempDirectory GetResourceDirectory(string resourceFolder)
        {
            var folder = $"{ResourceNamespace}.{resourceFolder}.";
            var resourcesToDownload = ThisAssembly.GetManifestResourceNames().Where(n => n.StartsWith(folder));
            var tempDir = new TempDirectory();

            foreach (var resource in resourcesToDownload)
            {
                var resourceRelative = resource.Substring(folder.Length);

                // If the resource file name contains periods other than that for the file extension, this will cause problems
                resourceRelative =
                   Path.GetFileNameWithoutExtension(resourceRelative).Replace(
                        ".",
                        Path.DirectorySeparatorChar.ToString())
                    + Path.GetExtension(resourceRelative);
                var saveTo = Path.Combine(tempDir.Path, resourceRelative);
                var saveToDir = Path.GetDirectoryName(saveTo);

                if (!Directory.Exists(saveToDir))
                {
                    Directory.CreateDirectory(saveToDir);
                }

                using var rs = ThisAssembly.GetManifestResourceStream(resource);
                using var f = File.Create(saveTo);
                // ReSharper disable once PossibleNullReferenceException - Can't be null as we got this from GetManifestResourceNames()
                rs.CopyTo(f);
            }

            return tempDir;
        }
    }
}