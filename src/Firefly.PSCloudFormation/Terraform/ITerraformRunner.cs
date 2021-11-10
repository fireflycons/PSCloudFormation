namespace Firefly.PSCloudFormation.Terraform
{
    using System;

    /// <summary>
    /// Interface defining execution of terraform binary
    /// </summary>
    internal interface ITerraformRunner
    {
        /// <summary>
        /// Runs Terraform.
        /// </summary>
        /// <param name="command">The command (e.g. plan, import etc).</param>
        /// <param name="throwOnError">if <c>true</c> throw an exception if terraform exits with non-zero status.</param>
        /// <param name="output">Action to collect output from the command. Can be <c>null</c></param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>If exceptions are disabled by <paramref name="throwOnError"/> then <c>false</c> is returned when terraform exits with non-zero status.</returns>
        bool Run(string command, bool throwOnError, Action<string> output, params string[] arguments);

        /// <summary>
        /// Evaluates the specified expression by invoking <c>terraform console</c>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>Evaluation result</returns>
        string Evaluate(string expression);
    }
}