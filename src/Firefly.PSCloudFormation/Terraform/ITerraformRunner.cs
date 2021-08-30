namespace Firefly.PSCloudFormation.Terraform
{
    using Firefly.PSCloudFormation.Terraform.PlanDeserialization;

    /// <summary>
    /// Interface defining execution of terraform binary
    /// </summary>
    internal interface ITerraformRunner
    {
        /// <summary>
        /// Gets the resource definition by calling <c>terraform state show</c>.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>Resource definition in HCL</returns>
        HclResource GetResourceDefinition2(string address);

        /// <summary>
        /// Runs Terraform.
        /// </summary>
        /// <param name="command">The command (e.g. plan, import etc).</param>
        /// <param name="throwOnError">if <c>true</c> throw an exception if terraform exits with non-zero status.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>If exceptions are disabled by <paramref name="throwOnError"/> then <c>false</c> is returned when terraform exits with non-zero status.</returns>
        bool Run(string command, bool throwOnError, params string[] arguments);

        /// <summary>
        /// Runs <c>terraform plan</c> on the current script.
        /// </summary>
        /// <returns>A <see cref="PlanErrorCollection"/> containing plan errors; else <c>null</c> if none.</returns>
        PlanErrorCollection RunPlan();
    }
}