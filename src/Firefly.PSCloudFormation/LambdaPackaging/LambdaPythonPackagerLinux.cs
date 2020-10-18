namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    internal class LambdaPythonPackagerLinux : LambdaPythonPackager
    {
        public LambdaPythonPackagerLinux(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        protected override bool IsVirtualEnv(string path)
        {
            return false;
        }
    }
}