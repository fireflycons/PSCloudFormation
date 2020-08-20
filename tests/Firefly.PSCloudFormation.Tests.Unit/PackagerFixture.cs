namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System;

    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;

    public class PackagerFixture : AutoResourceLoader, IDisposable
    {
        [EmbeddedResource("DeepNestedStack")]
        public TempDirectory DeepNestedStack { get; set; }

        public void Dispose()
        {
            this.DeepNestedStack?.Dispose();
        }
    }
}