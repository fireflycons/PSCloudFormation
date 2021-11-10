namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.State;

    using Newtonsoft.Json.Linq;

    internal class Serializer
    {
        private readonly IHclEmitter emitter;

        private string currentResourceType;

        public Serializer(IHclEmitter emitter)
        {
            this.emitter = emitter;
        }

        /// <summary>
        /// Tests <paramref name="text"/> to see if it is JSON.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="requirePolicy">if set to <c>true</c> require the JSON to be a policy document.</param>
        /// <param name="jsonDocument">The JSON document.</param>
        /// <returns><c>true</c> if the value is JSON and meets the policy conditions; else <c>false</c></returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected policy document and got JSON that is not a policy</exception>
        public static bool TryGetJson(string text, bool requirePolicy, out JObject jsonDocument)
        {
            var isJson = false;
            jsonDocument = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var firstChar = text.TrimStart().First();

            if (!(firstChar == '{' || firstChar == '['))
            {
                // Value is not JSON
                return false;
            }

            try
            {
                // If JSON, then possibly embedded policy document
                jsonDocument = JObject.Parse(text);

                if (requirePolicy && !jsonDocument.ContainsKey("Statement"))
                {
                    throw new HclSerializerException("Expected policy document and got JSON that is not a policy");
                }

                isJson = true;
            }
            catch
            {
                // Deliberately swallow. String is not valid JSON
            }

            if (!isJson)
            {
                jsonDocument = null;
            }

            return isJson;
        }

        /// <summary>
        /// Serializes the specified state file to HCL.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        public void Serialize(StateFile stateFile)
        {
            foreach (var r in stateFile.Resources)
            {
                this.currentResourceType = r.Type;
                this.emitter.Emit(new ResourceStart(r.Type, r.Name));
                this.WalkNode(r.Instances.First().Attributes);
                this.emitter.Emit(new ResourceEnd());
            }
        }

        /// <summary>
        /// Recursively walk the properties of a <c>JToken</c>
        /// </summary>
        /// <param name="node">The starting node.</param>
        private void WalkNode(JToken node)
        {
            switch (node.Type)
            {
                case JTokenType.Object:

                    this.emitter.Emit(new MappingStart());

                    foreach (var child in node.Children<JProperty>())
                    {
                        if (this.currentResourceType == "aws_iam_role" && child.Name == "inline_policy"
                                                                       && child.Value is JArray policies)
                        {
                            // Multiple inline_policy blocks need to be emitted.
                            foreach (var policy in policies)
                            {
                                this.emitter.Emit(new MappingKey("inline_policy"));
                                this.emitter.Emit(new SequenceStart());
                                this.WalkNode(policy);
                                this.emitter.Emit(new SequenceEnd());
                            }

                            continue;
                        }

                        // TODO: Block key, not mapping. HCLEmitter line 347
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

                case JTokenType.Constructor:

                    // A reference inserted by the walk through the dependency graph
                    var con = node.Value<JConstructor>();
                    var reference = Reference.FromJConstructor(con);

                    this.emitter.Emit(new ScalarValue(reference));
                    break;

                case JTokenType.Property:
                case JTokenType.Undefined:
                case JTokenType.None:
                case JTokenType.Raw:
                case JTokenType.Bytes:

                    throw new HclSerializerException($"Unexpected token {node.Type} in state file.");

                case JTokenType.Comment:

                    // Comments not expected in state file. Ignore for now.
                    break;

                default:

                    var scalar = new ScalarValue(((JValue)node).Value);

                    if (scalar.IsJsonDocument)
                    {
                        var policy = JObject.Parse(scalar.Value);
                        this.emitter.Emit(new JsonStart());
                        this.WalkNode(policy);
                        this.emitter.Emit(new JsonEnd());
                    }
                    else
                    {
                        this.emitter.Emit(scalar);
                    }

                    break;
            }
        }
    }
}