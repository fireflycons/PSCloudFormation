namespace Firefly.CloudFormation.Tests.Unit.resources
{
    using System.IO;
    using System.Reflection;

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
    }
}