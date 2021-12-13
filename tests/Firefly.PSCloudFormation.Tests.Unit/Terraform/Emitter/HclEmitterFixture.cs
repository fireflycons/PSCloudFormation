namespace Firefly.PSCloudFormation.Tests.Unit.Terraform.Emitter
{
    using System;
    using System.IO;

    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.Terraform;
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Tests.Unit.Utils;

    public class HclEmitterFixture : IDisposable
    {
        private readonly string mainDotTf;

        private readonly TempDirectory tempDirectory = new TempDirectory();

        private readonly string terraformBlock;

        private readonly AWSCredentials dummyCredentials = new BasicAWSCredentials(
            "AKIAEXAMBLE",
            "sdfgwretewffEXAMPLE");

        public HclEmitterFixture()
        {
            this.terraformBlock = new ConfigurationBlockBuilder().WithRegion("eu-west-1").Build();

            this.mainDotTf = Path.Combine(this.tempDirectory, "main.tf");
            File.WriteAllText(this.mainDotTf, this.terraformBlock);

            var cwd = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(this.tempDirectory);
                new TerraformRunner(this.dummyCredentials, new NullLogger()).Run("init", true, true, s => { });
            }
            finally
            {
                Directory.SetCurrentDirectory(cwd);
            }
        }

        public bool Validate(string hcl, ILogger logger)
        {
            File.WriteAllText(this.mainDotTf, this.terraformBlock + hcl);

            var cwd = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(this.tempDirectory);
                return new TerraformRunner(this.dummyCredentials, logger).Run(
                    "validate",
                    false,
                    true,
                    s => { },
                    "-no-color");
            }
            finally
            {
                Directory.SetCurrentDirectory(cwd);
            }
        }

        public void Dispose()
        {
            this.tempDirectory?.Dispose();
        }
    }
}