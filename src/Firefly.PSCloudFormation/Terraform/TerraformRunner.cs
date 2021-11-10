﻿namespace Firefly.PSCloudFormation.Terraform
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using InvalidOperationException = Amazon.CloudFormation.Model.InvalidOperationException;

    /// <summary>
    /// Implementation to invoke terraform binary
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.ITerraformRunner" />
    internal class TerraformRunner : ITerraformRunner
    {
        /// <summary>
        /// The credentials
        /// </summary>
        private readonly AWSCredentials credentials;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Path to terraform binary
        /// </summary>
        private readonly string terraformPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerraformRunner"/> class.
        /// </summary>
        /// <param name="credentials">AWS credentials.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.InvalidOperationException">Cannot find PATH environment variable</exception>
        /// <exception cref="System.IO.FileNotFoundException">Cannot find terraform executable</exception>
        public TerraformRunner(AWSCredentials credentials, ILogger logger)
        {
            this.credentials = credentials;
            this.logger = logger;
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            var tf = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "terraform.exe" : "terraform";

            if (string.IsNullOrEmpty(pathVar))
            {
                throw new InvalidOperationException("Cannot find PATH environment variable");
            }

            foreach (var path in pathVar.Split(';', ':'))
            {
                this.terraformPath = Path.Combine(path, tf);
                if (File.Exists(this.terraformPath))
                {
                    return;
                }
            }

            throw new FileNotFoundException("Cannot find terraform executable");
        }

        /// <summary>
        /// Runs ad-hoc Terraform commands.
        /// </summary>
        /// <param name="command">The command (e.g. plan, import etc).</param>
        /// <param name="throwOnError">if <c>true</c> throw an exception if terraform exits with non-zero status.</param>
        /// <param name="output">Action to collect output from the command. Can be <c>null</c></param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>
        /// If exceptions are disabled by <paramref name="throwOnError" /> then <c>false</c> is returned when terraform exits with non-zero status.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">terraform exited with code {process.ExitCode}</exception>
        public bool Run(string command, bool throwOnError, Action<string> output, params string[] arguments)
        {
            var commandtail = $"{command} ";
            commandtail += arguments == null
                               ? string.Empty
                               : string.Join(" ", arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

            var exitCode = this.RunProcess(
                commandtail.Trim(),
                msg =>
                    {
                        output?.Invoke(msg);
                        this.logger.LogInformation(msg);
                    },
                msg =>
                    {
                        output?.Invoke(msg);
                        this.logger.LogError(msg);
                    });

            if (exitCode != 0 && throwOnError)
            {
                throw new InvalidOperationException($"terraform exited with code {exitCode}");
            }

            return exitCode == 0;
        }

        /// <inheritdoc />
        public string Evaluate(string expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs terraform with AWS credentials in the environment
        /// </summary>
        /// <param name="commandTail">Command tail to send to terraform binary</param>
        /// <param name="stdout">Action to capture stdout</param>
        /// <param name="stderr">Action to capture stderr</param>
        /// <returns>Process exit code</returns>
        private int RunProcess(string commandTail, Action<string> stdout, Action<string> stderr)
        {
            var processInfo = new ProcessStartInfo
                                  {
                                      FileName = this.terraformPath,
                                      Arguments = commandTail,
                                      UseShellExecute = false,
                                      RedirectStandardOutput = true,
                                      RedirectStandardError = true,
                                      CreateNoWindow = true
                                  };

            var immutableCreds = this.credentials.GetCredentials();
            processInfo.Environment.Add("AWS_ACCESS_KEY_ID", immutableCreds.AccessKey);
            processInfo.Environment.Add("AWS_SECRET_ACCESS_KEY", immutableCreds.SecretKey);

            if (immutableCreds.UseToken)
            {
                processInfo.Environment.Add("AWS_SESSION_TOKEN", immutableCreds.Token);
            }

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (sender, e) =>
                    {
                        // Prepend line numbers to each line of the output.
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            stdout(e.Data);
                        }
                    };

                process.ErrorDataReceived += (sender, e) =>
                    {
                        // Prepend line numbers to each line of the output.
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            stderr(e.Data);
                        }
                    };

                process.Start();

                // Asynchronously read the standard output of the spawned process.
                // This raises OutputDataReceived events for each line of output.
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode;
            }
        }
    }
}