namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    internal class HclEmitter : IHclEmitter
    {
        private readonly TextWriter output;

        private int column;

        private readonly Stack<EmitterState> states = new Stack<EmitterState>();

        private readonly Queue<HclEvent> events = new Queue<HclEvent>();

        private readonly Stack<int> indents = new Stack<int>();

        private int indent;

        private bool isIndentation;

        private bool isWhitespace;

        private EmitterState state;

        private string currentResourceType = null;

        private Quirks quirks = new Quirks();

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

            OmitSequence
        }

        private void IncreaseIndent()
        {
            this.indents.Push(this.indent);
            this.indent += 4;
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
                this.EmitNode(evt);
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
                    this.currentResourceType = null;
                    this.quirks = new Quirks();
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
            this.currentResourceType = rs.ResourceType;
            this.quirks = Quirks.GetQuirks(rs.ResourceType);
            this.EmitScalar(new Scalar(rs.ResourceType, true));
            this.EmitScalar(new Scalar(rs.ResourceName, true));
        }

        private void EmitMappingStart(HclEvent evt)
        {
            this.states.Push(this.state);
            this.state = EmitterState.Mapping;
            this.WriteIndicator("{", true, false, false);
            this.IncreaseIndent();
            this.WriteIndent();
        }

        private void EmitMappingKey(HclEvent evt, bool isFirst)
        {
            if (!(evt is Scalar scalar))
            {
                throw new HclSerializerException($"Expected SCALAR, got {evt.GetType().Name}");
            }

            this.WriteIndent();
            this.EmitScalar(evt);

            var keyQuirks = this.quirks.GetQuirksForMappingKey(scalar.Value);

            if ((keyQuirks & QuirkType.OmitOuterSequence) != 0)
            {
                this.state = EmitterState.OmitSequence;
            }

            if ((keyQuirks & QuirkType.KeyWithoutEquals) != 0)
            {
                this.Write(' ');
            }
            else
            {
                this.WriteIndicator("=", true, false, false);
            }
        }

        private void EmitScalarValue(HclEvent evt)
        {
            if (this.state == EmitterState.Sequence)
            {
                this.WriteIndent();
            }

            this.EmitScalar(evt);
        }

        private void EmitMappingEnd(HclEvent evt)
        {
            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
            this.WriteIndent();
            this.WriteIndicator("}", false, false, true);

            if (this.state == EmitterState.Sequence)
            {
                this.Write(',');
            }
        }

        private void EmitSequenceStart(HclEvent evt)
        {
            this.states.Push(this.state);

            if (this.state == EmitterState.OmitSequence)
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

            if (this.state == EmitterState.OmitSequence)
            {
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