namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Text.RegularExpressions;

    using Amazon.CloudFormation.Model;
    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Concrete logger implementation for PowerShell
    /// </summary>
    /// <seealso cref="ILogger" />
    // ReSharper disable once InconsistentNaming
    public class PSLogger : BaseLogger
    {
        /// <summary>
        /// 30 chars whitespace for padding.
        /// </summary>
        private static readonly string Padding30 = new string(' ', 30);

        /// <summary>
        /// The <see cref="PSCmdlet"/> object.
        /// </summary>
        private readonly PSCmdlet cmdlet;

        /// <summary>
        /// Flag to output event headers
        /// </summary>
        private bool isFirstEvent = true;

        private string changesetLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSLogger"/> class.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet"/> object.</param>
        public PSLogger(PSCmdlet cmdlet)
        {
            this.cmdlet = cmdlet;
        }

        /// <summary>
        /// Logs a change set.
        /// The base implementation merely calculates column widths that a concrete implementation can use to render the change set details.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>
        /// Map of change column name to max width required.
        /// </returns>
        public override IDictionary<string, int> LogChangeset(DescribeChangeSetResponse changes)
        {
            if (!changes.Changes.Any())
            {
                this.LogInformation("No changes to stack resources. Other changes such as Outputs may be present.");
                return new Dictionary<string, int>();
            }

            if (this.changesetLogger != null)
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(this.changesetLogger));

                di.CreateIfNotExists();

                var json = JsonConvert.SerializeObject(
                    changes.Changes,
                    Formatting.Indented,
                    new ConstantClassJsonConverter());

                File.WriteAllText(this.changesetLogger, json);
                this.LogInformation($"Changeset detail written to '{this.changesetLogger}'");
            }

            var columnWidths = base.LogChangeset(changes);
            var foregroundColorMap = new Dictionary<string, ConsoleColor>
                                         {
                                             { "Add", ConsoleColor.Green },
                                             { "Import", ConsoleColor.Cyan },
                                             { "Modify", ConsoleColor.Yellow },
                                             { "Remove", ConsoleColor.Red },
                                             { "False", ConsoleColor.Green },
                                             { "True", ConsoleColor.Red },
                                             { "Conditional", ConsoleColor.Yellow }
                                         };

            var ui = this.cmdlet.Host.UI;
            var bg = ui.RawUI.BackgroundColor;

            // Adjust column widths for headings
            var actionColWidth = Math.Max(columnWidths["Action"], 6);
            var logicalResourceIdColWidth = Math.Max(columnWidths["LogicalResourceId"], 10);
            var resourceTypeColWidth = Math.Max(columnWidths["ResourceType"], 4);
            var replacementColWidth = Math.Max(columnWidths["Replacement"], 11);

            var changeFormatString =
                $"{{0,-{actionColWidth}}} {{1,-{logicalResourceIdColWidth}}} {{2,-{resourceTypeColWidth}}} {{3,-{replacementColWidth}}} {{4}}";

            // Resize window to be wide enough for a reasonable amount of Physical ID per line
            this.ResizeWindow(string.Format(changeFormatString, "x", "x", "x", "x", Padding30).Length);

            var leftIndent = GetLeftMarginForLastColumn(
                actionColWidth,
                logicalResourceIdColWidth,
                resourceTypeColWidth,
                replacementColWidth);
            var maxLineLength = ui.RawUI.WindowSize.Width - leftIndent;

            this.LogInformation("Changes to be made...\n");
            this.LogInformation(changeFormatString, "Action", "Logical ID", "Type", "Replacement", "Physical ID");
            this.LogInformation(changeFormatString, "------", "----------", "----", "-----------", "-----------");

            foreach (var r in changes.Changes.Select(change => change.ResourceChange))
            {
                ui.Write(foregroundColorMap[r.Action.Value], bg, r.Action.Value.PadRight(actionColWidth + 1));
                ui.Write(r.LogicalResourceId.PadRight(logicalResourceIdColWidth + 1));
                ui.Write(r.ResourceType.PadRight(resourceTypeColWidth + 1));

                if (r.Replacement == null)
                {
                    ui.Write(string.Empty.PadRight(replacementColWidth + 1));
                }
                else
                {
                    ui.Write(
                        foregroundColorMap[r.Replacement.Value],
                        bg,
                        r.Replacement.Value.PadRight(replacementColWidth + 1));
                }

                List<string> lines;

                if (r.PhysicalResourceId == null)
                {
                    ui.WriteLine();
                    continue;
                }

                if (r.PhysicalResourceId.Contains(' '))
                {
                    var charCount = 0;

                    lines = r.PhysicalResourceId.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .GroupBy(w => (charCount += w.Length + 1) / (maxLineLength - 1))
                        .Select(g => string.Join(" ", g)).ToList();
                }
                else
                {
                    lines = Regex.Matches(r.PhysicalResourceId, $".{{0,{maxLineLength}}}").Cast<Match>()
                        .Select(x => x.Value).ToList();
                }

                ui.WriteLine(lines.First());

                foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    ui.WriteLine(new string(' ', leftIndent) + line);
                }
            }

            this.LogInformation(string.Empty);
            return columnWidths;
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogDebug(string message, params object[] args)
        {
            if (this.cmdlet.MyInvocation.BoundParameters.Keys.Any(
                k => string.Compare(k, "Debug", StringComparison.OrdinalIgnoreCase) == 0))
            {
                this.cmdlet.Host.UI.WriteDebugLine(string.Format(message, args));
            }
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogError(string message, params object[] args)
        {
            this.cmdlet.Host.UI.WriteErrorLine(string.Format(message, args));
        }

        /// <summary>
        /// Log informational message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogInformation(string message, params object[] args)
        {
            this.cmdlet.Host.UI.WriteLine(string.Format(message, args));
        }

        /// <summary>
        /// Logs a stack event.
        /// </summary>
        /// <param name="event">The event.</param>
        public override void LogStackEvent(StackEvent @event)
        {
            const int TimeColWidth = 8;
            var stackColWidth = Math.Max(9, Math.Min(this.StackNameColumnWidth, 40));
            var resourceColWidth = Math.Max(11, Math.Min(this.ResourceNameColumnWidth, 40));

            var ui = this.cmdlet.Host.UI;
            var bg = ui.RawUI.BackgroundColor;

            if (this.isFirstEvent)
            {
                var eventFormatString =
                    $"{{0,-{TimeColWidth}}} {{1,-{stackColWidth}}} {{2,-{resourceColWidth}}} {{3,-{this.StatusColumnWidth}}} {{4}}";

                // Resize window to be wide enough for a reasonable amount of description per line
                this.ResizeWindow(string.Format(eventFormatString, "x", "x", "x", "x", Padding30).Length);

                this.isFirstEvent = false;
                this.LogInformation(eventFormatString, "Time", "StackName", "Logical ID", "Status", "Status Reason");
                this.LogInformation(eventFormatString, "----", "---------", "----------", "------", "-------------");
            }

            var leftIndent = GetLeftMarginForLastColumn(
                TimeColWidth,
                stackColWidth,
                resourceColWidth,
                this.StatusColumnWidth);
            var maxLineLength = ui.RawUI.WindowSize.Width - leftIndent;

            ui.Write($"{@event.Timestamp:HH:mm:ss} ");
            ui.Write(this.EllipsisString(@event.StackName, stackColWidth).PadRight(stackColWidth + 1));
            ui.Write(this.EllipsisString(@event.LogicalResourceId, resourceColWidth).PadRight(resourceColWidth + 1));

            var fg = ui.RawUI.ForegroundColor;
            var status = @event.ResourceStatus.Value;

            if (status.Contains("ROLLBACK") || ErrorStatus.IsMatch(status))
            {
                fg = ConsoleColor.Red;
            }
            else if (status.EndsWith("IN_PROGRESS"))
            {
                fg = ConsoleColor.Cyan;
            }
            else if (status.EndsWith("COMPLETE"))
            {
                fg = ConsoleColor.Green;
            }

            ui.Write(fg, bg, status.PadRight(this.StatusColumnWidth + 1));

            if (@event.ResourceStatusReason == null)
            {
                ui.WriteLine("-");
            }
            else
            {
                fg = ErrorStatus.IsMatch(status) ? ConsoleColor.Red : ui.RawUI.ForegroundColor;

                // Split text to fit in space we have in the window
                var charCount = 0;

                var lines = @event.ResourceStatusReason.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .GroupBy(w => (charCount += w.Length + 1) / (maxLineLength - 1)).Select(g => string.Join(" ", g))
                    .ToList();

                ui.WriteLine(fg, bg, lines.First());

                foreach (var line in lines.Skip(1))
                {
                    ui.WriteLine(fg, bg, new string(' ', leftIndent) + line);
                }
            }
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogVerbose(string message, params object[] args)
        {
            if (this.cmdlet.MyInvocation.BoundParameters.Keys.Any(
                k => string.Compare(k, "Verbose", StringComparison.OrdinalIgnoreCase) == 0))
            {
                this.cmdlet.Host.UI.WriteVerboseLine(string.Format(message, args));
            }
        }

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        public override void LogWarning(string message, params object[] args)
        {
            this.cmdlet.Host.UI.WriteWarningLine(string.Format(message, args));
        }

        /// <summary>
        /// Registers the changeset logger.
        /// </summary>
        /// <param name="changesetLoggerValue">The changeset logger passed as an argument.</param>
        public void RegisterChangesetLogger(string changesetLoggerValue)
        {
            this.changesetLogger = changesetLoggerValue;
        }

        /// <summary>
        /// Gets the left margin for for the last column for word wrapping
        /// </summary>
        /// <param name="columnWidths">The column widths.</param>
        /// <returns>Left margin</returns>
        private static int GetLeftMarginForLastColumn(params int[] columnWidths)
        {
            return columnWidths.Sum() + columnWidths.Length;
        }

        /// <summary>
        /// Resizes the window.
        /// </summary>
        /// <param name="minWidth">The minimum width.</param>
        private void ResizeWindow(int minWidth)
        {
            var rawUi = this.cmdlet.Host.UI.RawUI;

            if (rawUi.BufferSize.Width < minWidth)
            {
                var buf = rawUi.BufferSize;
                buf.Width = minWidth;
                rawUi.BufferSize = buf;
            }

            if (rawUi.WindowSize.Width < minWidth)
            {
                var buf = rawUi.WindowSize;
                try
                {
                    buf.Width = minWidth;
                    rawUi.WindowSize = buf;
                }
                catch (PSArgumentOutOfRangeException e)
                {
                    var re = new Regex(@"^Window cannot be wider than (?<width>\d+)\.");
                    var m = re.Match(e.Message);

                    if (m.Success)
                    {
                        buf.Width = int.Parse(m.Groups["width"].Value);
                        rawUi.WindowSize = buf;
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="JsonConverter"/> class to squash <see cref="ConstantClass"/> values into a <see cref="JValue"/>
        /// </summary>
        /// <seealso cref="Newtonsoft.Json.JsonConverter" />
        private class ConstantClassJsonConverter : JsonConverter
        {
            /// <summary>
            /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.
            /// </value>
            public override bool CanRead => false;

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                new JValue(((ConstantClass)value).Value).WriteTo(writer);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>
            /// The object value.
            /// </returns>
            /// <exception cref="NotImplementedException">Unnecessary because CanRead is false. The type will skip the converter.</exception>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            }

            /// <summary>
            /// Determines whether this instance can convert the specified object type.
            /// </summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns>
            /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
            /// </returns>
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsSubclassOf(typeof(ConstantClass));
            }
        }
    }
}