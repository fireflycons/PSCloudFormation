namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections.ObjectModel;
    using System.Management.Automation.Host;

    /// <summary>
    /// User interface operations
    /// </summary>
    internal interface IUserInterface
    {
        /// <summary>
        /// Prompts user for a choice.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="message">The message.</param>
        /// <param name="choices">The choices.</param>
        /// <param name="defaultChoice">The default choice.</param>
        /// <returns>Selected choice</returns>
        int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);
    }
}