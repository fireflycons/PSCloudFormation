namespace Firefly.PSCloudFormation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Xsl;

    using Amazon.CloudFormation.Model;
    using Amazon.Runtime;

    using Firefly.CloudFormation;
    using Firefly.CloudFormation.Utils;
    using Firefly.EmbeddedResourceLoader;
    using Firefly.EmbeddedResourceLoader.Materialization;
    using Firefly.PSCloudFormation.ChangeVisualisation;
    using Firefly.PSCloudFormation.Utils;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Formatting = Newtonsoft.Json.Formatting;

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
        /// Stylesheet for transforming changeset to HTML, loaded from embedded resources
        /// </summary>
        [EmbeddedResource("ChangesetFormatter.xslt")]
        // ReSharper disable once StyleCop.SA1650
#pragma warning disable 649
        private static XDocument changesetFormatter;
#pragma warning restore 649

        /// <summary>
        /// SVG renderer to use
        /// </summary>
        private readonly ISvgRenderer svgRenderer = new QuickChartSvgRenderer();

        /// <summary>
        /// The <see cref="PSCmdlet"/> object.
        /// </summary>
        private readonly PSCmdlet cmdlet;

        /// <summary>
        /// The changeset details
        /// </summary>
        private readonly List<ChangesetDetails> changesetDetails = new List<ChangesetDetails>();

        /// <summary>
        /// Flag to output event headers
        /// </summary>
        private bool isFirstEvent = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSLogger"/> class.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet"/> object.</param>
        public PSLogger(PSCmdlet cmdlet)
        {
            this.cmdlet = cmdlet;
            ResourceLoader.LoadResources(this);
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

            this.changesetDetails.Add(
                new ChangesetDetails
                    {
                        Changes = changes.Changes,
                        ChangeSetName = changes.ChangeSetName,
                        CreationTime = changes.CreationTime,
                        Description = changes.Description,
                        StackName = changes.StackName
                    });

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
            const int ScopeColWidth = 9;

            var changeFormatString =
                $"{{0,-{actionColWidth}}} {{1,-{logicalResourceIdColWidth}}} {{2,-{resourceTypeColWidth}}} {{3,-{replacementColWidth}}} {{4,-{ScopeColWidth}}} {{5}}";

            // Resize window to be wide enough for a reasonable amount of Physical ID per line
            this.ResizeWindow(string.Format(changeFormatString, "x", "x", "x", "x", "x", Padding30).Length);

            var leftIndent = GetLeftMarginForLastColumn(
                actionColWidth,
                logicalResourceIdColWidth,
                resourceTypeColWidth,
                replacementColWidth,
                ScopeColWidth);

            var maxLineLength = ui.RawUI.WindowSize.Width - leftIndent;

            this.LogInformation("Changes to be made...\n");
            this.LogInformation(changeFormatString, "Action", "Logical ID", "Type", "Replacement", "Scope", "Physical ID");
            this.LogInformation(changeFormatString, "------", "----------", "----", "-----------", "-----", "-----------");

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

                if (r.Scope != null && r.Scope.Any())
                {
                    ui.Write(
                        string.Join(",", r.Scope.Select(sc => sc[0].ToString().ToUpperInvariant()))
                            .PadRight(ScopeColWidth + 1));
                }
                else
                {
                    ui.Write(" ".PadRight(ScopeColWidth + 1));
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

            if (string.IsNullOrEmpty(@event.ResourceStatusReason))
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
        /// Determines whether this instance [can view changeset in browser].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance [can view changeset in browser]; otherwise, <c>false</c>.
        /// </returns>
        public bool CanViewChangesetInBrowser()
        {
            var os = new OSInfo().OSPlatform;

            try
            {
                if (os == OSPlatform.Windows)
                {
                    const string RegKey = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
                    const string ItemName = "InstallationType";

                    // Check version is Client or Server, i.e. excludes Server Core and Nano
                    // Read registry via provider system as Registry class not available in net standard
                    var value = this.cmdlet.SessionState.InvokeProvider.Property
                        .Get(RegKey, new Collection<string> { ItemName }).First().Members
                        .Match(ItemName, PSMemberTypes.NoteProperty).First().Value.ToString();

                    return value == "Client" || value == "Server";
                }

                if (os == OSPlatform.Linux)
                {
                    // Check for desktop (env var XDG_CURRENT_DESKTOP)
                    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
                }

                if (os == OSPlatform.OSX)
                {
                    // Always have a desktop
                    return true;
                }
            }
            catch (Exception e)
            {
                this.LogDebug($"Unable to determine if changeset can be viewed in browser: {e.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// View detailed changeset info in default browser
        /// </summary>
        /// <returns>Task to wait on</returns>
        public async Task ViewChangesetInBrowserAsync()
        {
            // Get JSON changes as XML
            var xmlChanges = JsonConvert.DeserializeXmlNode($"{{\"Stack\": {this.GetJsonChanges()} }}", "Stacks");

            if (xmlChanges == null)
            {
                this.LogWarning("Unable to convert changes to HTML. Would be the case when there are only changes to e.g. Outputs.");
                return;
            }

            var changesXDocument = xmlChanges.ToXDocument();

            switch (await this.svgRenderer.GetStatus())
            {
                case RendererStatus.Ok:

                    this.LogInformation($"Building change graph{(this.changesetDetails.Count > 1 ? "s" : string.Empty)}");
                    foreach (var details in this.changesetDetails)
                    {
                        var stack = details.StackName;
                        var elem = changesXDocument.Descendants("StackName").FirstOrDefault(e => e.Value == stack);

                        if (elem != null)
                        {
                            var graph = await details.RenderSvg(this.svgRenderer);
                            var graphNode = new XElement("Graph", graph);

                            // ReSharper disable once PossibleNullReferenceException - if this node exists, by definition of the schema it has a parent
                            elem.Parent.Add(graphNode);
                        }
                    }

                    break;

                case RendererStatus.ClientError:
                    this.LogWarning("SVG Renderer API returned 4xx error");
                    break;

                case RendererStatus.ServerError:
                    this.LogWarning("SVG Renderer API returned 5xx error");
                    break;

                case RendererStatus.NotFound:
                    this.LogWarning("SVG Renderer API endpoint not found");
                    break;

                case RendererStatus.ConnectionError:
                    this.LogWarning("SVG Renderer API could not be contacted");
                    break;
            }

            using (var ms = new MemoryStream())
            {
                // Transform to HTML
                var xslt = new XslCompiledTransform();
                xslt.Load(changesetFormatter.CreateReader());

                xslt.Transform(changesXDocument.CreateReader(), null, ms);
                ms.Seek(0, SeekOrigin.Begin);

                // Write HTML to temporary file and launch default browser
                using (var tempFile = new TempFile(ms, ".html"))
                {
                    var os = new OSInfo().OSPlatform;

                    if (os == OSPlatform.Windows)
                    {
                        // Windows shell open will open the file directly in default browser
                        this.cmdlet.SessionState.InvokeCommand.InvokeScript(
                            true,
                            ScriptBlock.Create($". {tempFile.FullPath}"),
                            null);
                    }
                    else if (os == OSPlatform.Linux)
                    {
                        // exec xdg-open ...
                        this.cmdlet.SessionState.InvokeCommand.InvokeScript(
                            true,
                            ScriptBlock.Create($"xdg-open {tempFile.FullPath}"),
                            null);
                    }
                    else if (os == OSPlatform.OSX)
                    {
                        // exec open ...
                        this.cmdlet.SessionState.InvokeCommand.InvokeScript(
                            true,
                            ScriptBlock.Create($"open {tempFile.FullPath}"),
                            null);
                    }

                    // Give browser time to open and read the file before it gets deleted
                    Thread.Sleep(5000);
                    this.LogDebug("Browser should have opened by now");
                }
            }
        }

        /// <summary>
        /// Write out changeset details if any to given output.
        /// </summary>
        /// <param name="filePath">Path to file to write JSON changes to</param>
        public void WriteChangesetDetails(string filePath)
        {
            if (!this.changesetDetails.Any() || filePath == null)
            {
                return;
            }

            var di = new DirectoryInfo(Path.GetDirectoryName(filePath));

            di.CreateIfNotExists();

            var json = this.GetJsonChanges();

            File.WriteAllText(filePath, json);
            this.LogInformation($"Changeset detail written to '{filePath}'");
        }

        /// <summary>
        /// Converts change detail to JSON
        /// </summary>
        /// <returns>JSON string</returns>
        public string GetJsonChanges()
        {
            return JsonConvert.SerializeObject(
                this.changesetDetails,
                Formatting.Indented,
                new ConstantClassJsonConverter());
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