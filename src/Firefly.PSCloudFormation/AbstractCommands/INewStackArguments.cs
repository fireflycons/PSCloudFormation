﻿namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System.Management.Automation;

    using Amazon.CloudFormation;

    /// <summary>
    /// Parameters related to stack creation.
    /// </summary>
    // ReSharper disable UnusedMemberInSuper.Global - This just describes parameters common to more than one cmdlet
    public interface INewStackArguments
    {
        /// <summary>
        /// Gets or sets the disable rollback.
        /// <para type="description">
        /// Set to <c>true</c> to disable rollback of the stack if stack creation failed. You can specify either DisableRollback or OnFailure, but not both.Default: <c>false</c>.
        /// </para>
        /// </summary>
        /// <value>
        /// The disable rollback.
        /// </value>
        SwitchParameter DisableRollback { get; set; }

        /// <summary>
        /// Gets or sets the enable termination protection.
        /// <para type="description">
        /// Whether to enable termination protection on the specified stack.
        /// If a user attempts to delete a stack with termination protection enabled, the operation fails and the stack remains unchanged.
        /// Termination protection is disabled on stacks by default. For nested stacks, termination protection is set on the root stack and cannot be changed directly on the nested stack.
        /// </para>
        /// </summary>
        /// <value>
        /// The enable termination protection.
        /// </value>
        SwitchParameter EnableTerminationProtection { get; set; }

        /// <summary>
        /// Gets or sets the on failure.
        /// <para type="description">
        /// Determines what action will be taken if stack creation fails.
        /// This must be one of: DO_NOTHING, ROLLBACK, or DELETE. You can specify either <c>OnFailure</c> or <c>DisableRollback</c>, but not both.Default: <c>ROLLBACK</c>
        /// </para>
        /// </summary>
        /// <value>
        /// The on failure.
        /// </value>
        OnFailure OnFailure { get; set; }

        /// <summary>
        /// Gets or sets the timeout in minutes.
        /// <para type="description">
        /// The amount of time that can pass before the stack status becomes CREATE_FAILED; if <c>DisableRollback</c> is not set or is set to <c>false</c>, the stack will be rolled back.
        /// </para>
        /// </summary>
        /// <value>
        /// The timeout in minutes.
        /// </value>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        int TimeoutInMinutes { get; set; }
    }
}