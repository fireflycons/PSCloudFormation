namespace Firefly.PSCloudFormation.Utils
{
    using System.Collections.ObjectModel;
    using System.Management.Automation.Host;

    /// <summary>
    /// PowerShell host user interface
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Utils.IUserInterface" />
    internal class PSUserInterface : IUserInterface
    {
        /// <summary>
        /// The UI
        /// </summary>
        private readonly PSHostUserInterface ui;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSUserInterface"/> class.
        /// </summary>
        /// <param name="ui">The UI.</param>
        public PSUserInterface(PSHostUserInterface ui)
        {
            this.ui = ui;
        }

        /// <summary>
        /// Prompts user for a choice.
        /// </summary>
        /// <param name="caption">The caption.</param>
        /// <param name="message">The message.</param>
        /// <param name="choices">The choices.</param>
        /// <param name="defaultChoice">The default choice.</param>
        /// <returns>
        /// Selected choice
        /// </returns>
        public int PromptForChoice(
            string caption,
            string message,
            Collection<ChoiceDescription> choices,
            int defaultChoice)
        {
            return this.ui.PromptForChoice(message, caption, choices, defaultChoice);
        }
    }
}