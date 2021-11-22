﻿namespace Firefly.PSCloudFormation.Terraform.HclSerializer
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

        private readonly ResourceTraitsCollection allResourceTraits;

        /// <summary>
        /// Queue of events to process
        /// </summary>
        private readonly Deque<HclEvent> events = new Deque<HclEvent>(256);

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
        /// Stack of nested block keys
        /// </summary>
        private readonly Stack<string> blockKeys = new Stack<string>();

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

        /// <summary>
        /// Initializes a new instance of the <see cref="HclEmitter"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public HclEmitter(TextWriter output)
        {
            this.output = output;
            this.state = EmitterState.Resource;
            this.allResourceTraits = ResourceTraitsCollection.Load();
            this.resourceTraits = this.allResourceTraits.TraitsAll;
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
        /// Gets the path of the current attribute (e.g. <c>root_block_device.iops</c> in an EC2 instance).
        /// Where there is a sequence in the path, this is represented by <c>*</c>.
        /// </summary>
        /// <value>
        /// The current path.
        /// </value>
        private string CurrentPath => string.Join(".", this.path.Where(p => p != null).Reverse());

        /// <summary>
        /// Gets a value indicating whether the current attribute is a top level attribute.
        /// Used in determining whether to emit a block or a mapping when a mapping key is encountered.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the current attribute is a top level attribute; otherwise, <c>false</c>.
        /// </value>
        private bool IsTopLevelAttribute => this.path.Count == 1;

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
        /// Analyzes an attribute's value to see whether it has a value, is null or is ann empty collection.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns>Result of analysis.</returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected MappingStart, SequenceStart or PolicyStart. Got {nextEvent.GetType().Name}</exception>
        private AttributeContent AnalyzeAttribute(MappingKey @event)
        {
            var nextEvent = this.events.Peek();

            switch (nextEvent)
            {
                case Scalar scalar:

                    return AnalyzeScalar(scalar);

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

            var key = @event.Value;
            var currentAnalysis = AttributeContent.None;

            var level = 0;
            var lastEvent = HclEvent.None;
#if DEBUG
            var collection = this.events.PeekUntil(new CollectionPeeker().Done, true).ToList();
#endif

            foreach (var evt in this.events.PeekUntil(new CollectionPeeker().Done, true))
            {
                level += evt.NestingIncrease;

                if (level == 0 && evt is CollectionEnd && lastEvent is CollectionStart)
                {
                    // Map or sequence with no elements
                    return AttributeContent.EmptyCollection;
                }

                if (currentAnalysis == AttributeContent.BlockObject || currentAnalysis == AttributeContent.BlockList)
                {
                    switch (evt)
                    {
                        case MappingStart _:
                        case SequenceStart _:
                        case ScalarValue scalarValue when !scalarValue.IsEmpty:

                            // Block mapping contains an object, a list or a non empty scalar then we want to emit it.
                            return currentAnalysis;

                        case MappingEnd _:

                            // Found no populated values, so skip it.
                            return AttributeContent.EmptyCollection;
                    }

                    continue;
                }

                if (lastEvent is SequenceStart && evt is Scalar)
                {
                    return AttributeContent.Sequence;
                }

                if (lastEvent is MappingStart)
                {
                    return AttributeContent.Mapping;
                }

                if (!(this.isJson || this.resourceTraits.IsNonBlockAttribute(key)))
                {
                    if (lastEvent is SequenceStart && evt is MappingStart)
                    {
                        // There's always going to be _something_ so return now
                        currentAnalysis = AttributeContent.BlockList;
                    }

                    if (evt is MappingStart && this.resourceTraits.IsBlockObject(key))
                    {
                        // Continue reading map values to determine if they have values.
                        currentAnalysis = AttributeContent.BlockObject;
                    }
                }

                lastEvent = evt;
            }

            // We should not get here.
            return AttributeContent.Value;
        }

        private class CollectionPeeker
        {
            private int level;

            public bool Done(HclEvent @event)
            {
                this.level += @event.NestingIncrease;

                return this.level == 0;
            }
        }

        /// <summary>
        /// Analyzes content of a scalar.
        /// </summary>
        /// <param name="scalar">The scalar.</param>
        /// <returns>Result of analysis.</returns>
        private static AttributeContent AnalyzeScalar(Scalar scalar)
        {
            if (scalar.Value == null)
            {
                return AttributeContent.Null;
            }

            if (bool.TryParse(scalar.Value, out var boolValue) && !boolValue)
            {
                return AttributeContent.BooleanFalse;
            }

            if (double.TryParse(scalar.Value, out var doubleVal) && doubleVal == 0)
            {
                // As this stands, all zeros are treated as empty, so if we want a zero emitted
                // would have to list as a required attribute.
                // If this is too much hassle, then need to refactor resource traits
                // to have conditions.
                return AttributeContent.Empty;
            }

            return string.IsNullOrWhiteSpace(scalar.Value) ? AttributeContent.EmptyString : AttributeContent.Value;
        }

        /// <summary>
        /// Emits a policy end.
        /// </summary>
        /// <param name="event">The event.</param>
        private void EmitJsonEnd(HclEvent @event)
        {
            var p = GetTypedEvent<JsonEnd>(@event);

            this.isJson = false;
            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
            this.WriteIndent();
            this.WriteIndicator(")", false, false, false);

            // Pop path at end of JSON block
            // JSON block is a mapping value so analogous to popping key after scalar value
            this.PopPath();

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

            this.isJson = true;
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
            var previousState = this.state;

            if (this.state != EmitterState.Resource)
            {
                this.state = this.states.Pop();
            }

            this.indent = this.indents.Pop();

            if (this.state == EmitterState.Mapping && this.path.Any())
            {
                // Extra pop for nested mappings - path may be empty when coming to the end of a sequence of blocks (must be able to do this better!)
                this.PopPath();
            }

            this.WriteIndent();
            this.WriteIndicator("}", false, false, true);

            var mappingStart = this.events.Peek() as MappingStart;

            if (this.state == EmitterState.BlockList && mappingStart != null)
            {
                // Next element in a block list
                this.WriteIndent();
                this.indent = this.indents.Pop();
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
            AttributeContent analysis;
            this.currentKey = key.Value;

            // Don't push path for repeating block key
            if (key.IsBlockKey)
            {
                analysis = AttributeContent.BlockList;
            }
            else
            {
                this.path.Push(this.currentKey);
                analysis = this.AnalyzeAttribute(key);

                if (!this.resourceTraits.ShouldEmitAttribute(this.CurrentPath, analysis))
                {
                    this.events.ConsumeUntil(new CollectionPeeker().Done, true);
                    this.currentKey = lastKey;
                    this.PopPath();
                    return;
                }

                this.WriteIndent();

                switch (analysis)
                {
                    case AttributeContent.BlockList:

                        this.blockKeys.Push(this.currentBlockKey);
                        this.currentBlockKey = key.Value;
                        this.states.Push(this.state);
                        this.state = EmitterState.BlockList;
                        break;

                    case AttributeContent.BlockObject:

                        this.state = EmitterState.BlockObject;
                        break;

                    default:

                        this.state = EmitterState.Mapping;
                        break;
                }
            }

            this.EmitScalar(@event);

            if (this.state == EmitterState.Mapping)
            {
                this.WriteIndicator("=", true, false, false);
            }
        }

        private EmitterState NextState(AttributeContent analysis)
        {
            switch (analysis)
            {
                case AttributeContent.Mapping:

                    return EmitterState.Mapping;

                case AttributeContent.Sequence:

                    return EmitterState.Sequence;

                case AttributeContent.BlockObject:

                    return EmitterState.BlockObject;

                case AttributeContent.BlockList:

                    return EmitterState.BlockList;

                default:

                    return EmitterState.Resource;
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
                            this.currentResourceName,
                            this.currentResourceType,
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

            var value = NonInterpolatedTokenRegex.Replace(scalar.Value, match => "$" + match.Groups["token"].Value);

            if (value.Any(char.IsControl))
            {
                // The Unicode standard classifies the characters \u000A (LF), \u000C (FF), and \u000D (CR) as control characters
                // Emit as here doc
                this.Write("<<-EOT");
                foreach (var line in value.Split(new[] { '\r', '\n' }))
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

            if (new[] { EmitterState.Mapping, EmitterState.BlockList, EmitterState.BlockObject, EmitterState.Resource }
                    .Contains(this.state) && @event is ScalarValue)
            {
                // When emitting a scalar mapping value, pop this value's key from the path
                this.PopPath();
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

            // Pop sequence (#)
            this.PopPath();

            if (this.events.Peek().Type != EventType.JsonEnd)
            {
                // Pop Mapping key for this sequence
                // Analogous to popping mapping key after a scalar value
                this.PopPath();
            }

            if (this.state == EmitterState.BlockList)
            {
                // End of block list
                this.state = this.states.Pop();
                this.currentBlockKey = this.blockKeys.Pop();
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
                this.PopPath();

                // Write empty sequence
                this.WriteIndicator("[]", true, false, true);
                this.WriteIndent();

                // ...and remove SequenceEnd
                this.events.Dequeue();
                return;
            }

            this.states.Push(this.state);
            this.path.Push("#");

            if (this.state == EmitterState.BlockList)
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
        /// Pops the path in one place for easier breakpoint.
        /// </summary>
        private void PopPath()
        {
            this.path.Pop();
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