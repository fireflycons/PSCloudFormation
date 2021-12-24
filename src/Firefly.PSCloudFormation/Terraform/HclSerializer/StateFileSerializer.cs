namespace Firefly.PSCloudFormation.Terraform.HclSerializer
{
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.State;
    using Firefly.PSCloudFormation.Utils.JsonTraversal;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Serializes a terraform state file to HCL.
    /// </summary>
    internal class StateFileSerializer
    {
        /// <summary>
        /// The emitter
        /// </summary>
        private readonly IHclEmitter emitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateFileSerializer"/> class.
        /// </summary>
        /// <param name="emitter">The emitter.</param>
        public StateFileSerializer(IHclEmitter emitter)
        {
            this.emitter = emitter;
        }

        /// <summary>
        /// Tests <paramref name="text"/> to see if it is JSON.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="requirePolicy">if set to <c>true</c> require the JSON to be a policy document.</param>
        /// <param name="resourceName">Name of the resource being serialized.</param>
        /// <param name="resourceType">Type of the resource being serialized.</param>
        /// <param name="jsonDocument">The JSON document.</param>
        /// <returns><c>true</c> if the value is JSON and meets the policy conditions; else <c>false</c></returns>
        /// <exception cref="Firefly.PSCloudFormation.Terraform.HclSerializer.HclSerializerException">Expected policy document and got JSON that is not a policy</exception>
        public static bool TryGetJson(
            string text,
            bool requirePolicy,
            string resourceName,
            string resourceType,
            out JContainer jsonDocument)
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
                jsonDocument = firstChar == '{' ? (JContainer)JObject.Parse(text) : JArray.Parse(text);

                if (requirePolicy && jsonDocument is JObject jo && !jo.ContainsKey("Statement"))
                {
                    throw new HclSerializerException(resourceName, resourceType, "Expected policy document and got JSON that is not a policy");
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
            this.Serialize(stateFile, null);
        }

        /// <summary>
        /// Serializes the specified state file to HCL.
        /// </summary>
        /// <param name="stateFile">The state file.</param>
        /// <param name="moduleName">Name of module whose resources to serialize</param>
        public void Serialize(StateFile stateFile, string moduleName)
        {
            foreach (var r in stateFile.FilteredResources(moduleName))
            {
                this.emitter.Emit(new ResourceStart(r.Type, r.Name));
                r.Instances.First().Attributes.Accept(
                    new EmitterVistor(),
                    new EmitterContext(this.emitter, r.Type, r.Name));
                this.emitter.Emit(new ResourceEnd());
            }
        }

        /// <summary>
        /// Context for visiting a resource definition in the state file.
        /// </summary>
        private class EmitterContext : IJsonVisitorContext<EmitterContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EmitterContext"/> class.
            /// </summary>
            /// <param name="emitter">The emitter.</param>
            /// <param name="currentResourceName">Name of the current resource.</param>
            /// <param name="currentResourceType">Type of the current resource.</param>
            public EmitterContext(IHclEmitter emitter, string currentResourceName, string currentResourceType)
            {
                this.Emitter = emitter;
                this.CurrentResourceName = currentResourceName;
                this.CurrentResourceType = currentResourceType;
            }

            /// <summary>
            /// Gets the emitter.
            /// </summary>
            /// <value>
            /// The emitter.
            /// </value>
            public IHclEmitter Emitter { get; }

            /// <summary>
            /// Gets the name of the current resource.
            /// </summary>
            /// <value>
            /// The name of the current resource.
            /// </value>
            public string CurrentResourceName { get; }

            /// <summary>
            /// Gets the type of the current resource.
            /// </summary>
            /// <value>
            /// The type of the current resource.
            /// </value>
            public string CurrentResourceType { get; }

            /// <inheritdoc />
            public EmitterContext Next(int index)
            {
                return this;
            }

            /// <inheritdoc />
            public EmitterContext Next(string name)
            {
                return this;
            }
        }

        /// <summary>
        /// Visitor that generates an event queue for the HCL emitter from a resource definition in the state file. 
        /// </summary>
        private class EmitterVistor : JsonVisitor<EmitterContext>
        {
            /// <inheritdoc />
            protected override void Visit(JArray json, EmitterContext context)
            {
                context.Emitter.Emit(new SequenceStart());
                base.Visit(json, context);
                context.Emitter.Emit(new SequenceEnd());
            }

            /// <inheritdoc />
            protected override void Visit(JObject json, EmitterContext context)
            {
                context.Emitter.Emit(new MappingStart());
                base.Visit(json, context);
                context.Emitter.Emit(new MappingEnd());
            }

            /// <inheritdoc />
            protected override void Visit(JConstructor json, EmitterContext context)
            {
                context.Emitter.Emit(new ScalarValue(Reference.FromJConstructor(json)));
            }

            /// <inheritdoc />
            protected override void Visit(JProperty json, EmitterContext context)
            {
                context.Emitter.Emit(new MappingKey(json.Name));
                base.Visit(json, context);
            }

            /// <inheritdoc />
            protected override void Visit(JValue json, EmitterContext context)
            {
                switch (json.Type)
                {
                    case JTokenType.Property:
                    case JTokenType.Undefined:
                    case JTokenType.None:
                    case JTokenType.Raw:
                    case JTokenType.Bytes:

                        throw new HclSerializerException(context.CurrentResourceName, context.CurrentResourceType, $"Unexpected token {json.Type} in state file.");

                    default:

                        var scalar = new ScalarValue(json.Value);

                        if (scalar.IsJsonDocument)
                        {
                            var nestedJson = scalar.JsonDocumentType == JTokenType.Object ? (JContainer)JObject.Parse(scalar.Value) : JArray.Parse(scalar.Value);
                            context.Emitter.Emit(new JsonStart());
                            nestedJson.Accept(this, context);
                            context.Emitter.Emit(new JsonEnd());
                        }
                        else
                        {
                            context.Emitter.Emit(scalar);
                        }

                        break;
                }
            }
        }
    }
}