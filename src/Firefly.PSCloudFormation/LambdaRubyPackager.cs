namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// Packager for Node.JS lambda
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.LambdaPackager" />
    /// <seealso href="https://docs.aws.amazon.com/lambda/latest/dg/ruby-package.html"/>
    internal class LambdaRubyPackager : LambdaPackager
    {
        public LambdaRubyPackager(
            FileSystemInfo lambdaArtifact,
            List<LambdaDependency> dependencies,
            IPSS3Util s3,
            ILogger logger)
            : base(lambdaArtifact, dependencies, s3, logger)
        {
        }

        protected override async Task<ResourceUploadSettings> PackageDirectory(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        protected override string PreparePackage(string workingDirectory)
        {
            throw new NotImplementedException();
        }
    }
}