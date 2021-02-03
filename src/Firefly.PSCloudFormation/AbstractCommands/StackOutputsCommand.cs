namespace Firefly.PSCloudFormation.AbstractCommands
{
    using System.Collections;
    using System.Linq;

    using Firefly.CloudFormation;

    /// <summary>
    /// Base for cmdlets that can return stack outputs
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.AbstractCommands.StackParameterCloudFormationCommand" />
    public abstract class StackOutputsCommand : StackParameterCloudFormationCommand
    {
        /// <summary>
        /// Gets the stack outputs.
        /// </summary>
        /// <value>
        /// The outputs.
        /// </value>
        [SelectableOutputProperty]
        protected Hashtable Outputs { get; private set; }

        /// <summary>
        /// Perform any post stack processing, such as retrieving values for select properties
        /// </summary>
        protected override void AfterOnProcessRecord()
        {
            base.AfterOnProcessRecord();
            this.Outputs = new Hashtable(
                new CloudFormationOperations(this._ClientFactory, this.Context).GetStackAsync(this.StackName)
                    .Result
                    .Outputs
                    .ToDictionary(o => o.OutputKey, o => o.OutputValue));
        }
    }
}