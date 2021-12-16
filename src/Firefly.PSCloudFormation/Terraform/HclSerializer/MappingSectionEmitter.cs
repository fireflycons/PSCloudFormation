namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.CloudFormationParser.TemplateObjects.Traversal;
    using Firefly.CloudFormationParser.TemplateObjects.Traversal.AcceptExtensions;

    /// <summary>
    /// Generates an HCL locals block from the content of the CloudFormation Mappings section
    /// </summary>
    internal class MappingSectionEmitter
    {
        /// <summary>
        /// The CloudFormation mappings
        /// </summary>
        private readonly MappingSection mappings;

        /// <summary>
        /// The output stream
        /// </summary>
        private readonly TextWriter output;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingSectionEmitter"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="mappings">The mappings.</param>
        public MappingSectionEmitter(TextWriter output, MappingSection mappings)
        {
            this.mappings = mappings;
            this.output = output;
        }

        /// <summary>
        /// Emits CloudFormation mappings as a terraform locals block.
        /// </summary>
        public void Emit()
        {
            if (this.mappings == null)
            {
                return;
            }

            this.output.WriteLine("locals {");
            this.output.Write("  mappings = ");
            this.mappings.Accept(
                new MappingEmitterVisitor(this.mappings.Template),
                new MappingEmitterContext(this.output));
            this.output.WriteLine("}");
            this.output.WriteLine();
        }

        /// <summary>
        /// Context object for mapping emitter
        /// </summary>
        private class MappingEmitterContext : ITemplateObjectVisitorContext<MappingEmitterContext>
        {
            /// <summary>
            /// Stack of previous indentations
            /// </summary>
            private readonly Stack<int> indents = new Stack<int>();

            /// <summary>
            /// The output stream
            /// </summary>
            private readonly TextWriter output;

            /// <summary>
            /// Stack of state transitions
            /// </summary>
            private readonly Stack<State> states = new Stack<State>();

            /// <summary>
            /// The current indentation level.
            /// </summary>
            private int indent = 2;

            /// <summary>
            /// Initializes a new instance of the <see cref="MappingEmitterContext"/> class.
            /// </summary>
            /// <param name="output">The output stream.</param>
            public MappingEmitterContext(TextWriter output)
            {
                this.output = output;
            }

            /// <summary>
            /// State of the emitter
            /// </summary>
            public enum State
            {
                /// <summary>
                /// Emitting a map - no consideration for values
                /// </summary>
                Map,

                /// <summary>
                /// Emitting a list - values require indentation and comma suffix
                /// </summary>
                List
            }

            /// <summary>
            /// Gets the current state.
            /// </summary>
            /// <value>
            /// The the current state.
            /// </value>
            public State CurrentState { get; private set; } = State.Map;

            /// <summary>
            /// Enters a list.
            /// </summary>
            public void EnterList()
            {
                this.states.Push(this.CurrentState);
                this.CurrentState = State.List;
                this.WriteLine("[");
                this.IncreaseIndent();
            }

            /// <summary>
            /// Enters a map.
            /// </summary>
            public void EnterMap()
            {
                this.states.Push(this.CurrentState);
                this.CurrentState = State.Map;
                this.WriteLine("{");
                this.IncreaseIndent();
            }

            /// <summary>
            /// Exits a list.
            /// </summary>
            public void ExitList()
            {
                this.CurrentState = this.states.Pop();
                this.DecreaseIndent();
                this.WriteIndent();
                this.WriteLine("]");
            }

            /// <summary>
            /// Exits a map.
            /// </summary>
            public void ExitMap()
            {
                this.CurrentState = this.states.Pop();
                this.DecreaseIndent();
                this.WriteIndent();
                this.WriteLine("}");
            }

            /// <summary>
            /// Gets the next context for an item in a list.
            /// </summary>
            /// <param name="index">Index in current list</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
            public MappingEmitterContext Next(int index) => this;

            /// <summary>
            /// Gets the next context for an entry in a dictionary
            /// </summary>
            /// <param name="name">Name of property.</param>
            /// <returns>
            /// Current or new context.
            /// </returns>
            public MappingEmitterContext Next(string name) => this;

            /// <summary>
            /// Writes the specified text to the output stream.
            /// </summary>
            /// <param name="text">The text.</param>
            public void Write(string text)
            {
                this.output.Write(text);
            }

            /// <summary>
            /// Writes whitespace to current indentation level.
            /// </summary>
            public void WriteIndent()
            {
                this.output.Write(new string(' ', this.indent));
            }

            /// <summary>
            /// Writes a line with line break to the output stream.
            /// </summary>
            /// <param name="text">The text.</param>
            public void WriteLine(string text)
            {
                this.output.WriteLine(text);
            }

            /// <summary>
            /// Decreases the indent.
            /// </summary>
            private void DecreaseIndent()
            {
                this.indent = this.indents.Pop();
            }

            /// <summary>
            /// Increases the indent.
            /// </summary>
            private void IncreaseIndent()
            {
                this.indents.Push(this.indent);
                this.indent += 2;
            }
        }

        /// <summary>
        /// Visits the CloudFormation template's mappings section emitting HCL
        /// </summary>
        private class MappingEmitterVisitor : TemplateObjectVisitor<MappingEmitterContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MappingEmitterVisitor"/> class.
            /// </summary>
            /// <param name="template">The parsed CloudFormation template.</param>
            public MappingEmitterVisitor(ITemplate template)
                : base(template)
            {
            }

            /// <summary>
            /// Visits the specified dictionary.
            /// </summary>
            /// <typeparam name="TKey">The type of the key. This should be either string or object(string).</typeparam>
            /// <param name="dict">The dictionary.</param>
            /// <param name="context">The context.</param>
            protected override void Visit<TKey>(IDictionary<TKey, object> dict, MappingEmitterContext context)
            {
                context.EnterMap();
                base.Visit(dict, context);
                context.ExitMap();
            }

            /// <summary>
            /// Visits the specified list.
            /// </summary>
            /// <typeparam name="TItem">The type of the list item. This should be dictionary, list, intrinsic or any acceptable value type for CloudFormation.</typeparam>
            /// <param name="list">The list.</param>
            /// <param name="context">The context.</param>
            protected override void Visit<TItem>(IList<TItem> list, MappingEmitterContext context)
            {
                context.EnterList();
                base.Visit(list, context);
                context.ExitList();
            }

            /// <summary>
            /// Visits the specified property, i.e. <see cref="T:System.Collections.Generic.KeyValuePair`2" /> of a dictionary object.
            /// </summary>
            /// <typeparam name="TKey">The type of the key. This should be either string or object(string).</typeparam>
            /// <param name="property">The property.</param>
            /// <param name="context">The context.</param>
            protected override void Visit<TKey>(KeyValuePair<TKey, object> property, MappingEmitterContext context)
            {
                context.WriteIndent();
                context.Write($"{FormatKey(property.Key.ToString())} = ");
                base.Visit(property, context);
            }

            /// <summary>
            /// Visits the specified string value.
            /// </summary>
            /// <param name="stringValue">The string value.</param>
            /// <param name="context">The context.</param>
            protected override void Visit(string stringValue, MappingEmitterContext context)
            {
                WriteValue($"\"{stringValue}\"", context);
            }

            /// <summary>
            /// Visits the specified integer value.
            /// </summary>
            /// <param name="integerValue">The integer value.</param>
            /// <param name="context">The context.</param>
            protected override void Visit(int integerValue, MappingEmitterContext context)
            {
                WriteValue(integerValue.ToString(), context);
            }

            /// <summary>
            /// Visits the specified double value.
            /// </summary>
            /// <param name="doubleValue">The double value.</param>
            /// <param name="context">The context.</param>
            protected override void Visit(double doubleValue, MappingEmitterContext context)
            {
                WriteValue(doubleValue.ToString(CultureInfo.InvariantCulture), context);
            }

            /// <summary>
            /// Visits the specified boolean value.
            /// </summary>
            /// <param name="booleanValue">if set to <c>true</c> [boolean value].</param>
            /// <param name="context">The context.</param>
            protected override void Visit(bool booleanValue, MappingEmitterContext context)
            {
                WriteValue(booleanValue.ToString().ToLowerInvariant(), context);
            }

            /// <summary>
            /// Formats a map key, by quoting it if it contains punctuation or is all digits.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>Map key, quoted if necessary</returns>
            private static string FormatKey(string key)
            {
                return key.Any(char.IsPunctuation) || key.All(char.IsDigit) ? $"\"{key}\"" : key;
            }

            /// <summary>
            /// Writes a pre-formatted value. How this is written depends on whether the current state is Map or List
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="context">The context.</param>
            private static void WriteValue(string value, MappingEmitterContext context)
            {
                var isList = context.CurrentState == MappingEmitterContext.State.List;

                if (isList)
                {
                    context.WriteIndent();
                }

                context.WriteLine($"{value}{(isList ? "," : string.Empty)}");
            }
        }
    }
}