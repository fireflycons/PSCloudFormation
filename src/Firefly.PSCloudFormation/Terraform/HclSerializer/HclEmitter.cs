﻿namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Amazon.Runtime.EventStreams;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    /// <summary>
    /// HCL Emitter. Inspired by YamlDotNet emitter.
    /// Handles emitting of resources to HCL from events generated by reading the state file.
    /// - All mappings at top level in a resource are blocks, unless explicitly _not_ blocks (e.g. "ingress", "egress" in SG)
    /// - Blocks can be nested
    /// - All other mappings are mappings
    /// - Blocks may or may not contain a sequence in state file:
    ///   - Block, Object (e.g. "timeouts") = no sequence
    ///   - Block, List|Set = sequence
    /// - Multiple sequence values for a block are emitted as multiple instances of the block
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.HclSerializer.IHclEmitter" />
    internal class HclEmitter : IHclEmitter
    {
        /// <summary>
        /// Matches tokens embedded in scalars that should not be treated as interpolations.
        /// </summary>
        private static readonly Regex NonInterpolatedTokenRegex = new Regex(@"(?<token>\$\{[^.\}]+\})");

        /// <summary>
        /// Queue of events to process
        /// </summary>
        private readonly EmitterEventQueue<HclEvent> events = new EmitterEventQueue<HclEvent>(256);

        /// <summary>
        /// Stack of indent levels
        /// </summary>
        private readonly Stack<int> indents = new Stack<int>();

        /// <summary>
        /// Sink for HCL output
        /// </summary>
        private readonly TextWriter output;

        /// <summary>
        /// Stack of states processed as emitter descends object graph
        /// </summary>
        private readonly Stack<EmitterState> states = new Stack<EmitterState>();

        /// <summary>
        /// Stack of nested block keys
        /// </summary>
        private readonly Stack<MappingKey> blockKeys = new Stack<MappingKey>();

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
        private MappingKey currentBlockKey;

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
        /// <c>true</c> when emitting a <c>jsonencode</c> block
        /// </summary>
        private bool isJson;

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

        private EmitterState previousState;

        private string currentPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="HclEmitter"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public HclEmitter(TextWriter output)
        {
            this.output = output;
            this.state = EmitterState.Resource;
            this.resourceTraits = AwsSchema.TraitsAll;
        }

        /// <summary>
        /// States the emitter can be in
        /// </summary>
        private enum EmitterState
        {
            /// <summary>
            /// At top level resource attributes
            /// </summary>
            Resource,

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
            /// In a block set/list definition where there is a sequence of repeating blocks.
            /// </summary>
            BlockList,

            /// <summary>
            /// In a block mapping that has no embedded sequence component, like 'timeouts'
            /// </summary>
            BlockObject
        }

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

            try
            {
                this.state = EmitterState.Resource;
                while (this.events.Any())
                {
                    this.EmitNode(this.events.Dequeue());
                }
            }
            catch (HclSerializerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new HclSerializerException($"Internal error: {e.Message}", this.currentResourceName, this.currentResourceType, e);
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
        /// Analyzes an attribute's value to see whether it has a value, is null or is an empty collection.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>Result of analysis.</returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected MappingStart, SequenceStart or PolicyStart. Got {nextEvent.GetType().Name}</exception>
        private AttributeContent AnalyzeAttribute(MappingKey @event)
        {
            var nextEvent = this.events.Peek();
            var currentAnalysis = @event.InitialAnalysis;

            switch (nextEvent)
            {
                case Scalar scalar:

                    return this.AnalyzeScalar(@event, scalar);

                case JsonStart _:

                    return AttributeContent.Value;
            }

            if (this.isJson)
            {
                // When reading embedded JSON, emit it all
                switch (nextEvent)
                {
                    case MappingStart _:

                        return AttributeContent.Mapping;

                    case SequenceStart _:

                        return AttributeContent.Sequence;

                    default:

                        return AttributeContent.Value;
                }
            }

            if (!(nextEvent is CollectionStart))
            {
                throw new HclSerializerException(
                    this.currentResourceName,
                    this.currentResourceType,
                    $"Expected MappingStart, SequenceStart or JsonStart. Got {nextEvent.GetType().Name}");
            }

            // Read ahead the entire collection
            var collection = this.events.PeekUntil(new CollectionPeeker().Done, true).ToList();

            return collection.Any(e => e is ScalarValue sv && !sv.IsEmpty)
                       ? currentAnalysis
                       : AttributeContent.EmptyCollection;
        }

        /// <summary>
        /// Analyzes content of a scalar.
        /// </summary>
        /// <param name="key">Key of the value being checked.</param>
        /// <param name="scalarValue">Value to analyze.</param>
        /// <returns>Result of analysis.</returns>
        private AttributeContent AnalyzeScalar(MappingKey key, Scalar scalarValue)
        {
            if (!key.Schema.Optional)
            {
                return string.IsNullOrWhiteSpace(scalarValue.Value) ? AttributeContent.Empty : AttributeContent.Value;
            }

            var value = scalarValue.Value;

            if (value == null)
            {
                return AttributeContent.ValueDefault;
            }

            if (this.resourceTraits.IsOmittedConditionalAttrbute(key.Path, value))
            {
                return AttributeContent.Empty;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (key.Schema.Type)
            {
                case SchemaValueType.TypeBool:

                    if (bool.TryParse(value, out var boolValue) && boolValue)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeInt:

                    if (int.TryParse(value, out var intValue) && intValue != 0)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeFloat:

                    if (double.TryParse(value, out var doubleValue) && doubleValue != 0)
                    {
                        return AttributeContent.Value;
                    }

                    break;

                case SchemaValueType.TypeString:

                    return string.IsNullOrEmpty(value) ? AttributeContent.ValueDefault : AttributeContent.Value;

                default:

                    throw new InvalidOperationException($"Invalid \"{key.Schema.Type}\" for scalar value at \"{key.Path}\"");
            }

            if (value.Length > 0 && char.IsLetter(value.First()) && value.Contains("."))
            {
                // A reference
                return AttributeContent.Value;
            }

            return AttributeContent.Empty;

        }

        /// <summary>
        /// Emits a policy end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitJsonEnd(HclEvent @event)
        {
            GetTypedEvent<JsonEnd>(@event);

            this.isJson = false;
            this.indent = this.indents.Pop();
            this.state = this.PopState();
            this.WriteIndent();
            this.WriteIndicator(")", false, false, false);

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
            GetTypedEvent<JsonStart>(@event);

            this.isJson = true;
            this.PushState(this.state);
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
            GetTypedEvent<MappingEnd>(@event);

            if (this.state != EmitterState.Resource)
            {
                this.state = this.PopState();
            }

            this.indent = this.indents.Pop();

            this.WriteIndent();
            this.WriteIndicator("}", false, false, true);

            var mappingStart = this.events.Peek() as MappingStart;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (this.state)
            {
                case EmitterState.BlockList when mappingStart != null:

                    // Next element in a block list
                    this.WriteIndent();
                    this.indent = this.indents.Pop();
                    this.EmitMappingKey(this.currentBlockKey);
                    this.IncreaseIndent();

                    return;

                case EmitterState.Sequence when mappingStart != null:

                    this.Write(',');
                    this.WriteIndent();
                    break;
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
            var analysis = this.AnalyzeAttribute(key);

            if (analysis == AttributeContent.EmptyCollection)
            {
                this.events.ConsumeUntil(new CollectionPeeker().Done, true);
                return;
            }

            this.currentKey = key.Value;

            // Don't push path for repeating block key
            if (!key.IsBlockKey)
            {
                if (!this.isJson)
                {
                    if (!key.ShouldEmitAttribute(analysis) || this.resourceTraits.IsConflictingArgument(key.Path))
                    {
                        this.events.ConsumeUntil(new CollectionPeeker().Done, true);
                        this.currentKey = lastKey;
                        return;
                    }

                    this.currentPath = key.Path;
                }

                this.WriteIndent();
            }
            else
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (analysis)
                {
                    case AttributeContent.BlockList:

                        this.blockKeys.Push(key);
                        this.currentBlockKey = key;
                        this.PushState(this.state);
                        this.state = EmitterState.BlockList;
                        break;

                    case AttributeContent.BlockObject:

                        this.state = EmitterState.BlockObject;
                        break;

                    default:

                        this.state = EmitterState.Mapping;
                        break;
                }

                this.WriteBreak();
                this.WriteIndent();
            }

            this.EmitScalar(@event);

            if (this.state == EmitterState.Mapping)
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
            GetTypedEvent<MappingStart>(@event);

            this.PushState(this.state);
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
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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

                    this.indents.Clear();
                    this.column = 0;
                    this.indent = 0;
                    this.resourceTraits = AwsSchema.TraitsAll;
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
            this.resourceTraits = AwsSchema.GetResourceTraits(rs.ResourceType);
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

            var value = NonInterpolatedTokenRegex.Replace(scalar.Value, match => "$" + match.Groups["token"].Value);

            if (value.Any(char.IsControl))
            {
                // The Unicode standard classifies the characters \u000A (LF), \u000C (FF), and \u000D (CR) as control characters
                // Emit as here doc
                this.Write("<<-EOT");
                foreach (var line in value.Split('\r', '\n'))
                {
                    this.WriteIndent();
                    this.Write(line);
                }

                this.WriteIndent();
                this.Write("EOT");
            }
            else
            {
                if (scalar.IsQuoted)
                {
                    this.Write('"');
                }

                this.Write(value);

                if (scalar.IsQuoted)
                {
                    this.Write('"');
                }
            }

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }

            this.isWhitespace = false;
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

            this.EmitScalar(this.resourceTraits.ApplyDefaultValue(this.currentPath, scalar));
        }

        /// <summary>
        /// Emits a sequence end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitSequenceEnd(HclEvent @event)
        {
            GetTypedEvent<SequenceEnd>(@event);

            this.state = this.PopState();

            if (this.previousState == EmitterState.BlockList)
            {
                // End of block list
                while (this.state == EmitterState.BlockList)
                {
                    this.state = this.PopState();
                }

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
            GetTypedEvent<SequenceStart>(@event);

            if (this.events.Peek() is SequenceEnd)
            {
                // Write empty sequence
                this.WriteIndicator("[]", true, false, true);
                this.WriteIndent();

                // ...and remove SequenceEnd
                this.events.Dequeue();
                return;
            }

            if (this.state == EmitterState.BlockList)
            {
                // BlockList state will be pushed at start of mapping
                return;
            }

            this.PushState(this.state);

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

        /// <summary>
        /// Push the current emitter state onto the state stack.
        /// </summary>
        /// <param name="currentState">The current state</param>
        private void PushState(EmitterState currentState)
        {
            this.states.Push(currentState);
        }

        /// <summary>
        /// Pop the emitter state and store the previous state.
        /// </summary>
        /// <returns>The next emitter state.</returns>
        private EmitterState PopState()
        {
            this.previousState = this.state;
            return this.states.Pop();
        }

        /// <summary>
        /// Provides a stateful predicate to <see cref="EmitterEventQueue{T}.PeekUntil"/>
        /// to locate the end of a nested group.
        /// </summary>
        // ReSharper disable once StyleCop.SA1650
        private class CollectionPeeker
        {
            /// <summary>
            /// The current nesting level
            /// </summary>
            private int level;

            /// <summary>
            /// Determines when the end of a nested block is found in the event queue (nesting level returns to zero)
            /// </summary>
            /// <param name="event">The event.</param>
            /// <returns><c>true</c> when end of nesting located.</returns>
            public bool Done(HclEvent @event)
            {
                this.level += @event.NestingIncrease;

                return this.level == 0;
            }
        }
    }
}