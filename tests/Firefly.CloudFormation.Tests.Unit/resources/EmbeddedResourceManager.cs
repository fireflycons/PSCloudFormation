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
            var resource = $"{ResourceNamespace}.{name}";
            var s =  ThisAssembly.GetManifestResourceStream(resource);

            if (s == null)
            {
                throw new FileNotFoundException($"Resource '{resource}' not found. Did you set it to embedded resource?");
            }

            return s;
        }

        public static string GetResourceString(string name)
        {
            using var sr = new StreamReader(GetResourceStream(name));
            return sr.ReadToEnd();
        }
    }
}