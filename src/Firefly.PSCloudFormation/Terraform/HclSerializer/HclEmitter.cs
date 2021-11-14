namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;
    using Firefly.PSCloudFormation.Utils;

    /// <summary>
    /// HCL Emitter. Inspired by YamlDotNet emitter
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.IHclEmitter" />
    internal class HclEmitter : IHclEmitter
    {
        /// <summary>
        /// Matches tokens embedded in scalars that should not be treated as interpolations.
        /// </summary>
        private static readonly Regex NonInterpolatedTokenRegex = new Regex(@"(?<token>\$\{[^.\}]+\})");

        private readonly ResourceTraitsCollection allResourceTraits;

        /// <summary>
        /// Queue of events to process
        /// </summary>
        private readonly Queue<HclEvent> events = new Queue<HclEvent>();

        /// <summary>
        /// Stack of indent levels
        /// </summary>
        private readonly Stack<int> indents = new Stack<int>();

        /// <summary>
        /// Sink for HCL output
        /// </summary>
        private readonly TextWriter output;

        /// <summary>
        /// Stack used to create resource attribute path (e.g. <c>root_block_device.iops</c> in an EC2 instance).
        /// </summary>
        private readonly Stack<string> path = new Stack<string>();

        /// <summary>
        /// Stack of states processed as emitter descends object graph
        /// </summary>
        private readonly Stack<EmitterState> states = new Stack<EmitterState>();

        /// <summary>
        /// The current column number in the output
        /// </summary>
        private int column;

        /// <summary>
        /// The current resource path attribute (e.g. <c>iops</c>)
        /// </summary>
        private string currentKey;

        /// <summary>
        /// The current block key if emitting a block
        /// </summary>
        private string currentBlockKey;

        /// <summary>
        /// The current resource name
        /// </summary>
        private string currentResourceName;

        /// <summary>
        /// The current resource type
        /// </summary>
        private string currentResourceType;

        /// <summary>
        /// The current indent level (in chars)
        /// </summary>
        private int indent;

        /// <summary>
        /// Whether the last thing emitted was an indentation
        /// </summary>
        private bool isIndentation;

        /// <summary>
        /// Whether the last thing emitted was whitespace.
        /// </summary>
        private bool isWhitespace;

        /// <summary>
        /// The resource traits for the current resource being serialized.
        /// </summary>
        private IResourceTraits resourceTraits;

        /// <summary>
        /// Current state of the emitter.
        /// </summary>
        private EmitterState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="HclEmitter"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public HclEmitter(TextWriter output)
        {
            this.output = output;
            this.state = EmitterState.None;
            this.allResourceTraits = ResourceTraitsCollection.Load();
            this.resourceTraits = this.allResourceTraits.TraitsAll;
        }

        /// <summary>
        /// States the emitter can be in
        /// </summary>
        private enum EmitterState
        {
            /// <summary>
            /// Undefined
            /// </summary>
            None,

            /// <summary>
            /// In a regular mapping type, like tags.
            /// </summary>
            Mapping,

            /// <summary>
            /// In a sequence
            /// </summary>
            Sequence,

            /// <summary>
            /// In a JSON block (<c>jsonencode</c>)
            /// </summary>
            Json,

            /// <summary>
            /// In a block definition.
            /// </summary>
            Block
        }

        /// <summary>
        /// Gets the path of the current attribute (e.g. <c>root_block_device.iops</c> in an EC2 instance).
        /// Where there is a sequence in the path, this is represented by <c>*</c>.
        /// </summary>
        /// <value>
        /// The current path.
        /// </value>
        private string CurrentPath => string.Join(".", this.path.Where(p => p != null).Reverse());

        /// <summary>
        /// Emits the next event.
        /// Events are queued until an entire resource is collected, the that resource is written out.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Emit(HclEvent @event)
        {
            this.events.Enqueue(@event);

            if (@event.Type != EventType.ResourceEnd)
            {
                return;
            }

            this.state = EmitterState.None;
            while (this.events.Any())
            {
                this.EmitNode(this.events.Dequeue());
            }
        }

        /// <summary>
        /// Asserts an event is of the requested type and casts to it.
        /// </summary>
        /// <typeparam name="T">Expected subtype of the event passed as argument.</typeparam>
        /// <param name="event">The event.</param>
        /// <returns>Type casted event.</returns>
        /// <exception cref="System.ArgumentException">Expected {typeof(T).Name} - event</exception>
        private static T GetTypedEvent<T>(HclEvent @event)
            where T : HclEvent
        {
            if (!(@event is T hclEvent))
            {
                throw new ArgumentException($"Expected {typeof(T).Name}", nameof(@event));
            }

            return hclEvent;
        }

        /// <summary>
        /// Analyzes an attribute's value to see whether it has a value, is null or is ann empty collection.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>Result of analysis.</returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected MappingStart, SequenceStart or PolicyStart. Got {nextEvent.GetType().Name}</exception>
        private AttributeContent AnalyzeAttribute(MappingKey @event)
        {
            var nextEvent = this.events.Peek();

            if (nextEvent is Scalar scalar)
            {
                if (scalar.Value == null)
                {
                    return AttributeContent.Null;
                }

                if (bool.TryParse(scalar.Value, out var boolValue) && !boolValue)
                {
                    return AttributeContent.BooleanFalse;
                }

                return string.IsNullOrWhiteSpace(scalar.Value)
                           ? AttributeContent.EmptyString
                           : AttributeContent.HasValue;
            }

            if (!(nextEvent is CollectionStart))
            {
                throw new HclSerializerException(
                    $"Expected MappingStart, SequenceStart or PolicyStart. Got {nextEvent.GetType().Name}");
            }

            var level = 0;
            var ind = 0;
            var evts = this.events.ToList();

            do
            {
                nextEvent = evts[ind];

                if (nextEvent is Scalar)
                {
                    return AttributeContent.HasValue;
                }

                level += nextEvent.NestingIncrease;
                ++ind;
            }
            while (level > 0);

            return AttributeContent.EmptyCollection;
        }

        /// <summary>
        /// Consumes an attribute's value from the event queue without emitting it,.
        /// </summary>
        private void ConsumeAttribute()
        {
            var nextEvent = this.events.Peek();

            if (nextEvent is Scalar scalar)
            {
                this.events.Dequeue();
                return;
            }

            var level = 0;

            do
            {
                var evt = this.events.Dequeue();
                level += evt.NestingIncrease;
            }
            while (level > 0);
        }

        /// <summary>
        /// Emits a policy end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitJsonEnd(HclEvent @event)
        {
            var p = GetTypedEvent<JsonEnd>(@event);

            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
            this.WriteIndent();
            this.WriteIndicator(")", false, false, false);

            // Pop path at end of JSON blocks
            this.path.Pop();

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }
        }

        /// <summary>
        /// Emits a policy start.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitJsonStart(HclEvent @event)
        {
            var p = GetTypedEvent<JsonStart>(@event);

            this.states.Push(this.state);
            this.state = EmitterState.Json;
            this.WriteIndicator("jsonencode(", true, false, true);
            this.IncreaseIndent();
            this.WriteIndent();
        }

        /// <summary>
        /// Emits a mapping end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitMappingEnd(HclEvent @event)
        {
            var m = GetTypedEvent<MappingEnd>(@event);
            var previousState = this.state = this.states.Pop();

            this.indent = this.indents.Pop();

            if (this.state == EmitterState.Mapping && this.path.Any())
            {
                // Extra pop for nested mappings - path may be empty when coming to the end of a sequence of blocks (must be able to do this better!)
                this.path.Pop();
            }

            this.WriteIndent();
            this.WriteIndicator("}", false, false, true);

            var mappingStart = this.events.Peek() as MappingStart;

            if (previousState == EmitterState.Block && mappingStart != null)
            {
                this.indents.Pop();
                this.path.Pop(); // #
                this.path.Pop(); // block key
                this.WriteIndent();
                this.EmitMappingKey(new MappingKey(this.currentBlockKey, true));
                this.IncreaseIndent();
                return;
            }

            if (this.state == EmitterState.Sequence && mappingStart != null)
            {
                this.Write(',');
                this.WriteIndent();
            }
        }

        /// <summary>
        /// Emits a mapping key.
        /// </summary>
        /// <param name="event">A <see cref="MappingKey"/> event.</param>
        private void EmitMappingKey(HclEvent @event)
        {
            var key = GetTypedEvent<MappingKey>(@event);
            var lastKey = this.currentKey;

            this.currentKey = key.Value;
            this.path.Push(this.currentKey);

            if (key.IsBlockKey)
            {
                this.path.Push("#"); // Still effectively within sequence
            }

            if (!this.resourceTraits.ShouldEmitAttribute(this.CurrentPath, this.AnalyzeAttribute(key)))
            {
                this.ConsumeAttribute();
                this.currentKey = lastKey;
                this.path.Pop();
                return;
            }

            this.WriteIndent();
            this.EmitScalar(@event);

            var isPotentiallyBlock = key.IsBlockKey || (this.events.Count >= 2 && this.events.First() is SequenceStart
                                                                               && this.events.Skip(1).First() is
                                                                                   MappingStart
                                                                               && !this.states.Contains(
                                                                                   EmitterState.Json));

            if (isPotentiallyBlock && !this.resourceTraits.NonBlockTypeAttributes.Contains(key.Value))
            {
                this.states.Push(this.state);
                this.state = EmitterState.Block;
                this.currentBlockKey = key.Value;
            }
            else
            {
                this.WriteIndicator("=", true, false, false);
            }
        }

        /// <summary>
        /// Emits a mapping start.
        /// </summary>
        /// <param name="event">A <see cref="MappingStart"/> event.</param>
        private void EmitMappingStart(HclEvent @event)
        {
            var m = GetTypedEvent<MappingStart>(@event);

            this.states.Push(this.state);
            this.state = EmitterState.Mapping;
            this.WriteIndicator("{", true, false, false);
            this.IncreaseIndent();
            this.WriteIndent();
        }

        /// <summary>
        /// Emits the next node.
        /// </summary>
        /// <param name="event">The event to write.</param>
        private void EmitNode(HclEvent @event)
        {
            switch (@event.Type)
            {
                case EventType.ResourceStart:

                    this.EmitResourceStart(@event);
                    break;

                case EventType.MappingKey:

                    this.EmitMappingKey(@event);
                    break;

                case EventType.ScalarValue:

                    this.EmitScalarValue(@event);
                    break;

                case EventType.SequenceStart:

                    this.EmitSequenceStart(@event);
                    break;

                case EventType.SequenceEnd:

                    this.EmitSequenceEnd(@event);
                    break;

                case EventType.MappingStart:

                    this.EmitMappingStart(@event);
                    break;

                case EventType.MappingEnd:

                    this.EmitMappingEnd(@event);
                    break;

                case EventType.JsonStart:

                    this.EmitJsonStart(@event);
                    break;

                case EventType.JsonEnd:

                    this.EmitJsonEnd(@event);
                    break;

                case EventType.ResourceEnd:

                    if (this.path.Count > 0)
                    {
                        throw new HclSerializerException(
                            $"Internal error. Resource \"{this.currentResourceType}.{this.currentResourceName}. Path not empty: {this.CurrentPath}");
                    }

                    this.indents.Clear();
                    this.column = 0;
                    this.indent = 0;
                    this.resourceTraits = this.allResourceTraits.TraitsAll;
                    this.currentKey = null;
                    this.WriteBreak();
                    this.WriteBreak();
                    break;
            }
        }

        /// <summary>
        /// Emits the resource start.
        /// </summary>
        /// <param name="event">A <see cref="ResourceStart"/> event.</param>
        private void EmitResourceStart(HclEvent @event)
        {
            var rs = GetTypedEvent<ResourceStart>(@event);

            this.Write("resource");
            this.isWhitespace = false;
            this.resourceTraits = this.allResourceTraits[rs.ResourceType];
            this.currentResourceName = rs.ResourceName;
            this.currentResourceType = rs.ResourceType;
            this.EmitScalar(new Scalar(rs.ResourceType, true));
            this.EmitScalar(new Scalar(rs.ResourceName, true));
        }

        /// <summary>
        /// Emits a scalar.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitScalar(HclEvent @event)
        {
            var scalar = GetTypedEvent<Scalar>(@event);

            if (!this.isWhitespace)
            {
                this.Write(' ');
            }

            if (scalar.IsQuoted)
            {
                this.Write('"');
            }

            this.Write(NonInterpolatedTokenRegex.Replace(scalar.Value, match => "$" + match.Groups["token"].Value));

            if (scalar.IsQuoted)
            {
                this.Write('"');
            }

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }

            this.isWhitespace = false;

            if ((this.state == EmitterState.Mapping || this.state == EmitterState.Block) && @event is ScalarValue)
            {
                // When emitting a scalar mapping value, pop this value's key from the path
                this.path.Pop();
            }
        }

        /// <summary>
        /// Emits a scalar value.
        /// </summary>
        /// <param name="event">A <see cref="ScalarValue"/> event.</param>
        private void EmitScalarValue(HclEvent @event)
        {
            var scalar = GetTypedEvent<ScalarValue>(@event);

            if (this.state == EmitterState.Sequence)
            {
                this.WriteIndent();
            }

            this.EmitScalar(this.resourceTraits.ApplyDefaultValue(this.CurrentPath, scalar));
        }

        /// <summary>
        /// Emits a sequence end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitSequenceEnd(HclEvent @event)
        {
            var se = GetTypedEvent<SequenceEnd>(@event);

            this.state = this.states.Pop();

            // Pop twice - once for sequence and once for sequence's mapping key
            this.path.Pop();
            this.path.Pop();

            if (this.state == EmitterState.Block)
            {
                this.state = this.states.Pop();
                return;
            }

            this.indent = this.indents.Pop();
            this.WriteIndent();
            this.WriteIndicator("]", false, false, false);

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }
        }

        /// <summary>
        /// Emits a sequence start.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitSequenceStart(HclEvent @event)
        {
            var ss = GetTypedEvent<SequenceStart>(@event);

            if (this.events.Peek() is SequenceEnd)
            {
                // Pop key for this empty block as we are consuming SequenceEnd
                this.path.Pop();

                // Write empty sequence
                this.WriteIndicator("[]", true, false, true);
                this.WriteIndent();
                this.events.Dequeue();
                return;
            }

            this.states.Push(this.state);
            this.path.Push("#");

            if (this.state == EmitterState.Block)
            {
                return;
            }

            this.WriteIndicator("[", true, false, true);
            this.IncreaseIndent();
            this.WriteIndent();
            this.state = EmitterState.Sequence;
        }

        /// <summary>
        /// Increases the indentation level.
        /// </summary>
        private void IncreaseIndent()
        {
            this.indents.Push(this.indent);
            this.indent += 2;
        }

        /// <summary>
        /// Writes a character to the output stream
        /// </summary>
        /// <param name="value">The value.</param>
        private void Write(char value)
        {
            this.output.Write(value);
            ++this.column;
        }

        /// <summary>
        /// Writes a string to the output stream.
        /// </summary>
        /// <param name="value">The value.</param>
        private void Write(string value)
        {
            this.output.Write(value);
            this.column += value.Length;
        }

        /// <summary>
        /// Writes a line break to the output stream.
        /// </summary>
        /// <param name="breakCharacter">The break character.</param>
        private void WriteBreak(char breakCharacter = '\n')
        {
            if (breakCharacter == '\n')
            {
                this.output.WriteLine();
            }
            else
            {
                this.output.Write(breakCharacter);
            }

            this.column = 0;
        }

        /// <summary>
        /// Writes indentation to the output stream.
        /// </summary>
        private void WriteIndent()
        {
            var currentIndent = Math.Max(this.indent, 0);

            var isBreakRequired = !this.isIndentation || this.column > currentIndent
                                                      || (this.column == currentIndent && !this.isWhitespace);

            if (isBreakRequired)
            {
                this.WriteBreak();
            }

            while (this.column < currentIndent)
            {
                this.Write(' ');
            }

            this.isWhitespace = true;
            this.isIndentation = true;
        }

        /// <summary>
        /// Writes an indicator (syntactic sugar) to the output stream.
        /// </summary>
        /// <param name="indicator">The indicator.</param>
        /// <param name="needWhitespace">if set to <c>true</c> whitespace should be prepended.</param>
        /// <param name="whitespace">if set to <c>true</c> whitespace is being output.</param>
        /// <param name="indentation">if set to <c>true</c> indentation is being output.</param>
        private void WriteIndicator(string indicator, bool needWhitespace, bool whitespace, bool indentation)
        {
            if (needWhitespace && !this.isWhitespace)
            {
                this.Write(' ');
            }

            this.Write(indicator);

            this.isWhitespace = whitespace;
            this.isIndentation &= indentation;
        }
    }
}