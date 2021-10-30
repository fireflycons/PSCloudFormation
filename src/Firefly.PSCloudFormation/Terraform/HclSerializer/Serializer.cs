namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json.Linq;

    internal class Serializer
    {
        private static readonly List<string> AttributesToIgnore = new List<string> { "arn", "id", "create_date", "unique_id", "tags_all" };

        private readonly IHclEmitter emitter;

        public Serializer(IHclEmitter emitter)
        {
            this.emitter = emitter;
        }

        /// <summary>
        /// Serializes the specified state file to HCL.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        public void Serialize(StateFile stateFile)
        {
            foreach (var r in stateFile.Resources)
            {
                this.emitter.Emit(new ResourceStart(r.Type, r.Name));

                this.WalkNode(r.Instances.First().Attributes);

                this.emitter.Emit(new ResourceEnd());
            }
        }

        /// <summary>
        /// Determines whether the current property should not be emitted.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns><c>true</c> if the property should be emitted; else <c>false</c></returns>
        private static bool SkipProperty(JProperty property)
        {
            if (AttributesToIgnore.Contains(property.Name))
            {
                return true;
            }

            switch (property.Value)
            {
                case JValue jv when jv.Value == null:

                    // Skip properties with null values
                    return true;

                case JValue jv when jv.Value is string s && s == string.Empty:
                case JArray ja when ja.Count == 0:
                case JObject jo when !jo.HasValues:
                    // Skip empty mappings
                    // and empty sequences
                    // and empty strings
                    return true;

                default:

                    return false;
            }
        }

        /// <summary>
        /// Recursively walk the properties of a <c>JToken</c>
        /// </summary>
        /// <param name="node">The starting node.</param>
        private void WalkNode(
            JToken node)
        {
            switch (node.Type)
            {
                case JTokenType.Object:

                    this.emitter.Emit(new MappingStart());

                    foreach (var child in node.Children<JProperty>())
                    {
                        if (SkipProperty(child))
                        {
                            continue;
                        }

                        this.emitter.Emit(new MappingKey(child.Name));
                        this.WalkNode(child.Value);
                    }

                    this.emitter.Emit(new MappingEnd());
                    break;

                case JTokenType.Array:

                    this.emitter.Emit(new SequenceStart());

                    foreach (var child in node.Children())
                    {
                        this.WalkNode(child);
                    }

                    this.emitter.Emit(new SequenceEnd());
                    break;

                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.String:

                    var scalar = new ScalarValue(((JValue)node).Value);

                    if (scalar.IsPolicyDocument)
                    {
                        var policy = JObject.Parse(scalar.Value);
                        this.emitter.Emit(new PolicyStart());
                        this.WalkNode(policy);
                        this.emitter.Emit(new PolicyEnd());
                    }
                    else
                    {
                        this.emitter.Emit(scalar);
                    }

                    break;

                default:

                    throw new HclSerializerException($"Unexpected token {node.Type}");
            }
        }
    }
}