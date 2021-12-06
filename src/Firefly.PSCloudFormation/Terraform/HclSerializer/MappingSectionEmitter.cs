namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.TemplateObjects;
    using Firefly.CloudFormationParser.Utils;

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
            this.output.WriteLine("  mappings = {");
            this.mappings.Accept(new MappingEmitterVisitor(this.output));
            this.output.WriteLine("  }");
            this.output.WriteLine("}");
            this.output.WriteLine();
        }

        /// <summary>
        /// Visitor class to emit the mappings
        /// </summary>
        /// <seealso cref="Firefly.CloudFormationParser.TemplateObjects.ITemplateObjectVisitor" />
        private class MappingEmitterVisitor : TemplateObjectVisitor
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
            /// The indentation
            /// </summary>
            private int indent = 4;

            /// <summary>
            /// Initializes a new instance of the <see cref="MappingEmitterVisitor"/> class.
            /// </summary>
            /// <param name="output">The output stream.</param>
            public MappingEmitterVisitor(TextWriter output)
            {
                this.output = output;
            }

            /// <summary>
            /// Called at the end of the enumeration of a list.
            /// </summary>
            /// <typeparam name="T">Type of item in list. Should be string or object.</typeparam>
            /// <param name="templateObject">The template object being visited</param>
            /// <param name="path">The current path in the property walk.</param>
            /// <param name="item">The list being visited.</param>
            public override void AfterVisitList<T>(ITemplateObject templateObject, PropertyPath path, IList<T> item)
            {
                this.indent = this.indents.Pop();
                this.WriteIndent();
                this.output.WriteLine("]");
            }

            /// <summary>
            /// Called at the end of the enumeration of a on object (dictionary( item ).
            /// </summary>
            /// <typeparam name="T">Type of item in list. Should be string or object.</typeparam>
            /// <param name="templateObject">The template object.</param>
            /// <param name="path">The path.</param>
            /// <param name="item">The dictionary item being visited.</param>
            public override void AfterVisitObject<T>(
                ITemplateObject templateObject,
                PropertyPath path,
                IDictionary<T, object> item)
            {
                this.indent = this.indents.Pop();
                this.WriteIndent();
                this.output.WriteLine("}");
            }

            /// <summary>
            /// Called when a list is about to be visited (before list traversal).
            /// </summary>
            /// <typeparam name="T">Type of item in list. Should be string or object.</typeparam>
            /// <param name="templateObject">The template object being visited</param>
            /// <param name="path">The current path in the property walk.</param>
            /// <param name="item">The list being visited.</param>
            public override void BeforeVisitList<T>(ITemplateObject templateObject, PropertyPath path, IList<T> item)
            {
                this.output.WriteLine($"{FormatKey(path.Peek())} = [");
                this.IncreaseIndent();
            }

            /// <summary>
            /// Called when an object (dictionary) is about to be visited.
            /// </summary>
            /// <typeparam name="T">Type of item in list. Should be string or object.</typeparam>
            /// <param name="templateObject">The template object being visited</param>
            /// <param name="path">The current path in the property walk.</param>
            /// <param name="item">The dictionary being visited.</param>
            public override void BeforeVisitObject<T>(
                ITemplateObject templateObject,
                PropertyPath path,
                IDictionary<T, object> item)
            {
                this.WriteIndent();
                this.output.WriteLine($"{FormatKey(path.Peek())} = {{");

                this.IncreaseIndent();
            }

            /// <summary>
            /// Called when a scalar list item is being visited.
            /// </summary>
            /// <param name="templateObject">The template object being visited</param>
            /// <param name="path">The current path in the property walk.</param>
            /// <param name="item">The list item being visited.</param>
            public override void VisitListItem(ITemplateObject templateObject, PropertyPath path, object item)
            {
                switch (item)
                {
                    case string _:

                        this.WriteIndent();
                        this.output.WriteLine($"\"{item}\",");
                        break;

                    case int _:
                    case double _:

                        this.WriteIndent();
                        this.output.WriteLine($"{item},");
                        break;

                    case bool _:

                        this.WriteIndent();
                        this.output.WriteLine($"{item.ToString().ToLowerInvariant()},");
                        break;
                }
            }

            /// <summary>
            /// Called when a dictionary item is visited.
            /// </summary>
            /// <typeparam name="T">Type of item in list. Should be string or object.</typeparam>
            /// <param name="templateObject">The template object.</param>
            /// <param name="path">The path.</param>
            /// <param name="item">The dictionary item being visited.</param>
            public override void VisitProperty<T>(
                ITemplateObject templateObject,
                PropertyPath path,
                KeyValuePair<T, object> item)
            {
                switch (item.Value)
                {
                    case string _:

                        this.WriteIndent();
                        this.output.WriteLine($"{FormatKey(path.Peek())} = \"{item.Value}\"");
                        break;

                    case int _:
                    case double _:

                        this.WriteIndent();
                        this.output.WriteLine($"{FormatKey(path.Peek())} = {item.Value}");
                        break;

                    case bool _:

                        this.WriteIndent();
                        this.output.WriteLine($"{FormatKey(path.Peek())} = {item.Value.ToString().ToLowerInvariant()}");
                        break;
                }
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
            /// Increases the indent.
            /// </summary>
            private void IncreaseIndent()
            {
                this.indents.Push(this.indent);
                this.indent += 2;
            }

            /// <summary>
            /// Writes the indent.
            /// </summary>
            private void WriteIndent()
            {
                this.output.Write(new string(' ', this.indent));
            }
        }
    }
}