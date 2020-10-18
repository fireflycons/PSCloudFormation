namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormation;
    using Firefly.PSCloudFormation.Utils;

    internal class LambdaPythonPackagerWindows : LambdaPythonPackager
    {
        private const string IncludeDir = "include";

        private const string Lib64Dir = "lib64";

        private const string LibDir = "lib";

        private const string ScriptsDir = "scripts";

        /// <summary>
        /// Directories that are found within a virtual env.
        /// </summary>
        private static readonly string[] VenvDirectories = { ScriptsDir, LibDir, Lib64Dir, IncludeDir };

        public LambdaPythonPackagerWindows(LambdaArtifact lambdaArtifact, IPSS3Util s3, ILogger logger)
            : base(lambdaArtifact, s3, logger)
        {
        }

        /// <summary>
        /// Determines whether the specified path is a virtual env.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is virtual env] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A virtual env should contain at least the following directories: Include, Lib or Lib64, Scripts
        /// </remarks>
        protected override bool IsVirtualEnv(string path)
        {
            var directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(d => Path.GetFileName(d).ToLowerInvariant()).ToList();

            var inCommon = VenvDirectories.Intersect(directories).ToList();

            return inCommon.Contains(ScriptsDir) && inCommon.Contains(IncludeDir)
                                                 && (inCommon.Contains(Lib64Dir) || inCommon.Contains(LibDir));
        }
    }
}