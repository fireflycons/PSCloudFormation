namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Traits;

    internal class HclEmitter : IHclEmitter
    {
        private readonly TextWriter output;

        private int column;

        private readonly Stack<EmitterState> states = new Stack<EmitterState>();

        private readonly Queue<HclEvent> events = new Queue<HclEvent>();

        private readonly Stack<int> indents = new Stack<int>();

        private readonly Stack<string> path = new Stack<string>();

        private string currentKey;

        private string currentResourceName;

        private string currentResourceType;

        private int indent;

        private bool isIndentation;

        private bool isWhitespace;

        private EmitterState state;

        private ResourceTraits resourceTraits = new ResourceTraits();

        public HclEmitter(TextWriter output)
        {
            this.output = output;
            this.state = EmitterState.None;
        }

        private enum EmitterState
        {
            None,

            Mapping,

            Sequence,

            Policy,

            Block
        }

        private enum AttributeContent
        {
            Null,

            EmptyString,

            EmptyCollection,

            HasValue
        }

        private string CurrentPath =>
            string.Join(".", this.path.Where(p => p != null).Concat(new[] { this.currentKey }).ToList());

        private int Depth => this.path.Where(p => p != null).Count();

        private void IncreaseIndent()
        {
            this.indents.Push(this.indent);
            this.indent += 2;
        }

        public void Emit(HclEvent evt)
        {
            this.events.Enqueue(evt);

            if (evt.Type != EventType.ResourceEnd)
            {
                return;
            }

            this.state = EmitterState.None;
            while (this.events.Any())
            {
                evt = this.events.Dequeue();

                if (evt is MappingKey key && this.resourceTraits.IgnoredAttributes.Contains(key.Value))
                {
                    // Swallow this event and its descendents.
                    var level = 0;

                    do
                    {
                        evt = this.events.Dequeue();
                        level += evt.NestingIncrease;
                    }
                    while (level > 0);
                }
                else
                {
                    this.EmitNode(evt);
                }
            }
        }

        /// <summary>
        /// Expect a node.
        /// </summary>
        private void EmitNode(HclEvent evt)
        {
            switch (evt.Type)
            {
                case EventType.ResourceStart:

                    this.EmitResourceStart(evt);
                    break;

                case EventType.MappingKey:

                    this.EmitMappingKey(evt, false);
                    break;

                case EventType.ScalarValue:

                    this.EmitScalarValue(evt);
                    break;

                case EventType.SequenceStart:

                    this.EmitSequenceStart(evt);
                    break;

                case EventType.SequenceEnd:

                    this.EmitSequenceEnd(evt);
                    break;

                case EventType.MappingStart:

                    this.EmitMappingStart(evt);
                    break;

                case EventType.MappingEnd:

                    this.EmitMappingEnd(evt);
                    break;

                case EventType.PolicyStart:

                    this.EmitPolicyStart(evt);
                    break;

                case EventType.PolicyEnd:

                    this.EmitPolicyEnd(evt);
                    break;

                case EventType.ResourceEnd:

                    this.indents.Clear();
                    this.column = 0;
                    this.indent = 0;
                    this.resourceTraits = new ResourceTraits();
                    this.currentKey = null;
                    this.WriteBreak();
                    this.WriteBreak();
                    break;
            }
        }

        private void EmitResourceStart(HclEvent evt)
        {
            if (!(evt is ResourceStart rs))
            {
                throw new ArgumentException("Expected RESOURCE-START.", nameof(evt));
            }

            this.Write("resource");
            this.isWhitespace = false;
            this.resourceTraits = ResourceTraits.GetTraits(rs.ResourceType);
            this.currentResourceName = rs.ResourceName;
            this.currentResourceType = rs.ResourceType;
            this.EmitScalar(new Scalar(rs.ResourceType, true));
            this.EmitScalar(new Scalar(rs.ResourceName, true));
        }

        private void EmitMappingStart(HclEvent evt)
        {
            this.states.Push(this.state);
            this.path.Push(this.currentKey);
            this.state = EmitterState.Mapping;
            this.WriteIndicator("{", true, false, false);
            this.IncreaseIndent();
            this.WriteIndent();
        }

        private void EmitMappingKey(HclEvent evt, bool isFirst)
        {
            if (!(evt is MappingKey scalar))
            {
                throw new HclSerializerException($"Expected MAPPING-KEY, got {evt.GetType().Name}");
            }

            var lastKey = this.currentKey;
            this.currentKey = scalar.Value;

            var analysis = this.AnalyzeAttribute(scalar);

            if (analysis != AttributeContent.HasValue && !this.resourceTraits.ShouldEmitAttribute(this.CurrentPath))
            {
                this.ConsumeAttribute();
                this.currentKey = lastKey;
                return;
            }

            this.WriteIndent();
            this.EmitScalar(evt);

            var isPotentiallyBlock = this.events.Count >= 2 && this.events.First() is SequenceStart
                                                            && this.events.Skip(1).First() is MappingStart
                                                            && !this.states.Contains(EmitterState.Policy);

            if (isPotentiallyBlock && !this.resourceTraits.NonBlockTypeAttributes.Contains(scalar.Value))
            {
                this.states.Push(this.state);
                this.state = EmitterState.Block;
            }
            else
            {
                this.WriteIndicator("=", true, false, false);
            }
        }

        private void EmitScalarValue(HclEvent evt)
        {
            if (!(evt is Scalar scalar))
            {
                throw new HclSerializerException($"Expected SCALAR. Got {evt.GetType().Name}");
            }


            if (this.state == EmitterState.Sequence)
            {
                this.WriteIndent();
            }

            this.EmitScalar(this.resourceTraits.ApplyDefaultValue(this.CurrentPath, scalar));
        }

        private void EmitMappingEnd(HclEvent evt)
        {
            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
            this.path.Pop();
            this.WriteIndent();
            this.WriteIndicator("}", false, false, true);

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }
        }

        private void EmitSequenceStart(HclEvent evt)
        {
            if (this.events.Peek() is SequenceEnd)
            {
                // Write empty sequence
                this.WriteIndicator("[]", true, false, true);
                this.WriteIndent();
                this.events.Dequeue();
                return;
            }

            this.states.Push(this.state);
            this.path.Push("*");

            if (this.state == EmitterState.Block)
            {
                return;
            }

            this.WriteIndicator("[", true, false, true);
            this.IncreaseIndent();
            this.WriteIndent();
            this.state = EmitterState.Sequence;
        }

        private void EmitSequenceEnd(HclEvent evt)
        {
            this.state = this.states.Pop();
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

        private void EmitPolicyStart(HclEvent evt)
        {
            this.states.Push(this.state);
            this.state = EmitterState.Policy;
            this.WriteIndicator("jsonencode(", true, false, true);
            this.IncreaseIndent();
            this.WriteIndent();
        }

        private void EmitPolicyEnd(HclEvent evt)
        {
            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
            this.WriteIndent();
            this.WriteIndicator(")", false, false, false);

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }
        }

        private void EmitScalar(HclEvent evt)
        {
            if (!(evt is Scalar scalar))
            {
                throw new ArgumentException("Expected SCALAR.", nameof(evt));
            }

            if (!this.isWhitespace)
            {
                this.Write(' ');
            }

            if (scalar.IsQuoted)
            {
                this.Write('"');
            }

            this.Write(scalar.Value);

            if (scalar.IsQuoted)
            {
                this.Write('"');
            }

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }

            this.isWhitespace = false;
        }

        private AttributeContent AnalyzeAttribute(MappingKey evt)
        {
            var nextEvent = this.events.Peek();

            if (nextEvent is Scalar scalar)
            {
                if (scalar.Value == null)
                {
                    return AttributeContent.Null;
                }
                
                return string.IsNullOrWhiteSpace(scalar.Value) ? AttributeContent.EmptyString : AttributeContent.HasValue;
            }

            if (!(nextEvent is CollectionStart))
            {
                throw new HclSerializerException($"Expected MAPPING-START, SEQUENCE-START or POLICY-START. Got {nextEvent.GetType().Name}");
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

        private void Write(char value)
        {
            this.output.Write(value);
            ++this.column;
        }

        private void Write(string value)
        {
            this.output.Write(value);
            this.column += value.Length;
        }

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