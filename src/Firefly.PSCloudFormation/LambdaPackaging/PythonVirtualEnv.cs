namespace Firefly.PSCloudFormation.LambdaPackaging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management.Automation.Language;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    using Firefly.PSCloudFormation.LambdaPackaging.PEP508;
    using Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model;
    using Firefly.PSCloudFormation.Utils;

    using sly.parser;
    using sly.parser.generator;

    internal class PythonVirtualEnv
    {
        private static readonly Regex PackageNameRegex = new Regex(@"^(?<packageName>.*)-\d+(\.\d+)*");

        private static readonly Regex RequirementsRegex =
            new Regex(@"^Requires\-Dist\:\s*(?<dependency>[\w+\-]+)");

        private List<PythonModule> modules = new List<PythonModule>();

        /// <summary>
        /// Variables for the PEP 508 Parser. Since we're building for a lambda environment, the OS will be Amazon Linux 2
        /// </summary>
        private readonly Dictionary<string, string> pep508Variables = new Dictionary<string, string>
                                                                                 {
                                                                                     { "os_name", "posix" },
                                                                                     { "sys_platform", "linux" },
                                                                                     {
                                                                                         "platform_machine", "x86_64"
                                                                                     }, // Pretty sure it is. May change to graviton
                                                                                     {
                                                                                         "platform_python_implementation",
                                                                                         "CPython"
                                                                                     },
                                                                                     { "platform_system", "Linux" },
                                                                                     {
                                                                                         "python_version", "0.0"
                                                                                     }, // Will be set by the specified lambda runtime from the resource declaration
                                                                                     {
                                                                                         "python_full_version", "0.0"
                                                                                     }, // Will be set by the specified lambda runtime from the resource declaration - we don't know the point release
                                                                                     {
                                                                                         "implementation_name",
                                                                                         "cpython"
                                                                                     },
                                                                                     {
                                                                                         "extra", string.Empty
                                                                                     } // Assumption of no 'extras'
                                                                                 };

        private readonly Parser<MetadataToken, IExpression> parser;

        private readonly IOSInfo platform;

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonVirtualEnv"/> class.
        /// </summary>
        /// <exception cref="PackagerException">Cannot find VIRTUAL_ENV environment variable. Activate your virtual env first, then run.</exception>
        public PythonVirtualEnv(IOSInfo platform)
        {
            this.platform = platform;
            if (string.IsNullOrEmpty(this.VirtualEnvDir))
            {
                throw new PackagerException(
                    "Cannot find VIRTUAL_ENV environment variable. Activate your virtual env first, then run.");
            }

            // Create PEP508 parser instance
            var parserInstance = new PEP508Parser();
            var builder = new ParserBuilder<MetadataToken, IExpression>();
            var build = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, "logical_expression");

            if (build.IsError)
            {
                var errorMessage = string.Join(Environment.NewLine, build.Errors.Select(e => e.Message));
                throw new PackagerException($"Error building PEP508 parser:\n{errorMessage}");
            }

            this.parser = build.Result;
        }

        /// <summary>
        /// Gets the absolute path to bin directory.
        /// </summary>
        /// <value>
        /// The bin directory.
        /// </value>
        public string BinDir =>
            Path.Combine(this.VirtualEnvDir, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Scripts" : "bin");

        /// <summary>
        /// Gets the absolute path to include directory.
        /// </summary>
        /// <value>
        /// The include directory.
        /// </value>
        public string IncludeDir => Path.Combine(this.VirtualEnvDir, "include");

        /// <summary>
        /// Gets the absolute path to library directory.
        /// </summary>
        /// <value>
        /// The library directory.
        /// </value>
        public string LibDir => Path.Combine(this.VirtualEnvDir, "lib");

        /// <summary>
        /// Gets the virtual env directory.
        /// </summary>
        /// <value>
        /// The virtual env directory.
        /// </value>
        public string VirtualEnvDir { get; } = Environment.GetEnvironmentVariable("VIRTUAL_ENV");

        /// <summary>
        /// Gets the <see cref="PythonModule"/> with the specified module name.
        /// </summary>
        /// <value>
        /// The <see cref="PythonModule"/>.
        /// </value>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>A <see cref="PythonModule"/> or <c>null</c> if not found</returns>
        internal PythonModule this[string moduleName]
        {
            get
            {
                return this.modules.FirstOrDefault(m => m.Name == moduleName);
            }
        }

        /// <summary>
        /// Gets the absolute path to site packages directory.
        /// </summary>
        /// <param name="runtimeInfo">The runtime information.</param>
        /// <returns>Absolute path to site packages directory.</returns>
        /// <exception cref="DirectoryNotFoundException">site-packages not found for runtime '{runtimeInfo.Runtime}'</exception>
        /// <remarks>
        /// On non-Windows platforms, site-packages is sorted into python version directories.
        /// </remarks>
        public string GetSitePackagesDir(LambdaRuntimeInfo runtimeInfo)
        {
            if (this.platform.OSPlatform == OSPlatform.Windows)
            {
                return Path.Combine(this.LibDir, "site-packages");
            }

            // Linux/MacOs have a site packages for each python version
            var path = Path.Combine(this.LibDir, runtimeInfo.Runtime, "site-packages");

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"site-packages not found for runtime '{runtimeInfo.Runtime}'");
            }

            return path;
        }

        public PythonVirtualEnv Load(LambdaRuntimeInfo runtimeInfo)
        {
            var sitePackages = this.GetSitePackagesDir(runtimeInfo);

            foreach (var dist in Directory.EnumerateDirectories(
                sitePackages,
                "*.dist-info",
                SearchOption.TopDirectoryOnly))
            {
                // RECORD file tells us where to find the code and is a 3 field header-less CSV
                var recordFile = Path.Combine(dist, "RECORD");

                // Name comes from the dist-info directory name
                // Package names always have hyphens, not underscores
                var module = new PythonModule
                                 {
                                     Name = PackageNameRegex.Match(Path.GetFileName(dist)).Groups["packageName"].Value
                                         .Replace('_', '-')
                                 };

                if (File.Exists(recordFile))
                {
                    var paths = new HashSet<string>();

                    // Pull all items from RECORD file into a set to get a unique list of what to include, ignoring dist-info files, Windows .pyd files and scripts placed in bin
                    foreach (var path in File.ReadAllLines(recordFile)
                        .Select(line => line.Split(',').First().Split('/').First())
                        .Where(path => !(path.EndsWith(".dist-info") || path.EndsWith(".pyd")) && path != ".." && path != "__pycache__"))
                    {
                        paths.Add(Path.Combine(sitePackages, path));
                    }

                    // TODO: Warn if a binary is a Windows DLL. Not likely to be good in Lambda (linux)
                    if (paths.Count == 0)
                    {
                        throw new PackagerException(
                            $"Found no content for package {module.Name}");
                    }

                    foreach (var p in paths)
                    {
                        module.Paths.Add(Path.Combine(sitePackages, p));
                    }
                }
                else
                {
                    throw new FileNotFoundException(
                        "Unable to determine package location - cannot find RECORD file",
                        recordFile);
                }

                // METADATA file tells us any dependencies with Requires-Dist records.
                var metadataFile = Path.Combine(dist, "METADATA");

                if (File.Exists(metadataFile))
                {
                    foreach (var dependency in File.ReadAllLines(metadataFile)
                        .Where(l => l.StartsWith("Requires-Dist:")))
                    {
                        var includeDependency = true;

                        if (dependency.Contains(";"))
                        {
                            var expression = dependency.Split(';').Last();
                            var r = this.parser.Parse(expression);

                            if (r.IsError)
                            {
                                var errorMessage = string.Join(
                                    Environment.NewLine,
                                    r.Errors.Select(e => e.ErrorMessage));
                                throw new PackagerException(
                                    $"Error parsing: {expression}\nIf this expression is valid, please raise an issue.\n{errorMessage}");
                            }

                            // Set variables according to targeted Python version.
                            this.pep508Variables["python_version"] = runtimeInfo.RuntimeVersion;
                            this.pep508Variables["python_full_version"] = runtimeInfo.RuntimeVersion;
                            var evaluation = r.Result.Evaluate(new ExpressionContext(this.pep508Variables));

                            if (evaluation == null)
                            {
                                throw new PackagerException(
                                    $"Error evaluating: {expression}\nIf this expression is valid, please raise an issue.");
                            }

                            includeDependency = (bool)evaluation;
                        }

                        if (includeDependency)
                        {
                            module.Dependencies.Add(RequirementsRegex.Match(dependency).Groups["dependency"].Value);
                        }
                    }
                }

                this.modules.Add(module);
            }

            return this;
        }

        /// <summary>
        /// Represents a python module in site-packages.
        /// Module may include assets in the root and in subdirectories
        /// </summary>
        [DebuggerDisplay("{Name}")]
        internal class PythonModule
        {
            public List<string> Dependencies { get; } = new List<string>();

            public bool IsSingleFile => Paths.Count == 1 && File.Exists(this.Paths[0]);

            public string Name { get; set; }

            public List<string> Paths { get; set; } = new List<string>();

            public List<string> Extras { get; } = new List<string>();
        }
    }
}