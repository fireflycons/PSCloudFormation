namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A reference type that expands to a terraform built-in function.
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.State.Reference" />
    internal class FunctionReference : Reference
    {
        /// <summary>
        /// The function arguments
        /// </summary>
        private readonly List<object> functionArguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="functionArguments">The function arguments.</param>
        /// <remarks>
        /// <para>
        /// The arguments should be arranged as the per the terraform function's arguments, e.g.
        /// join function has two arguments, a separator and a list of things to join.
        /// Thus, <paramref name="functionArguments"/> should be an enumerable of two items,
        /// the first being string, and the second being a list of objects.
        /// </para>
        /// <para>
        /// Where a function argument is another <see cref="Reference"/>, call <see cref="Reference.ToJConstructor"/>
        /// on it first and add the JConstructor object to the argument list.
        /// </para>
        /// </remarks>
        public FunctionReference(string functionName, IEnumerable<object> functionArguments)
        : this(functionName, functionArguments, -1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="functionArguments">The function arguments.</param>
        /// <param name="index">Indexer to apply to function call if >= 0</param>
        /// <remarks>
        /// <para>
        /// The arguments should be arranged as the per the terraform function's arguments, e.g.
        /// join function has two arguments, a separator and a list of things to join.
        /// Thus, <paramref name="functionArguments"/> should be an enumerable of two items,
        /// the first being string, and the second being a list of objects.
        /// </para>
        /// <para>
        /// Where a function argument is another <see cref="Reference"/>, call <see cref="Reference.ToJConstructor"/>
        /// on it first and add the JConstructor object to the argument list.
        /// </para>
        /// </remarks>
        public FunctionReference(string functionName, IEnumerable<object> functionArguments, int index)
            : base(functionName, index)
        {
            this.functionArguments = functionArguments.ToList();
            this.ReferenceExpression = $"{functionName}({ProcessArguments(this.functionArguments)}){(this.Index >= 0 ? $"[{this.Index}]" : string.Empty )}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="index">The index.</param>
        protected FunctionReference(string functionName, int index)
            : base(functionName, index)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        protected FunctionReference(string functionName)
            : base(functionName)
        {
        }

        /// <inheritdoc />
        public override string ReferenceExpression { get; }

        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        /// <value>
        /// The name of the function.
        /// </value>
        private string FunctionName => this.ObjectAddress;

        /// <inheritdoc />
        public override JConstructor ToJConstructor()
        {
            return this.Index < 0
                       ? new JConstructor(
                           JConstructorName,
                           this.GetType().FullName,
                           this.FunctionName,
                           JArray.FromObject(this.functionArguments))
                       : new JConstructor(
                           JConstructorName,
                           this.GetType().FullName,
                           this.FunctionName,
                           JArray.FromObject(this.functionArguments),
                           this.Index);
        }

        /// <summary>
        /// Processes the arguments, rendering them as an HCL function argument string.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <returns>Argument string for function call.</returns>
        /// <remarks>
        /// When user constructed, arguments will be mostly C# types.
        /// When constructed by <see cref="Reference.FromJConstructor"/> they will all be JToken types.
        /// </remarks>
        private static string ProcessArguments(List<object> arguments)
        {
            var formattedArgs = new List<string>();
            foreach (var item in arguments)
            {
                switch (item)
                {
                    case JValue jv:

                        switch (jv.Type)
                        {
                            case JTokenType.String:

                                formattedArgs.Add($"\"{jv.Value<string>()}\"");
                                break;

                            case JTokenType.Integer:
                            case JTokenType.Float:
                            case JTokenType.Boolean:

                                formattedArgs.Add(jv.Value<string>().ToLowerInvariant());
                                break;

                            default:

                                throw new InvalidOperationException($"Unexpected JTokenType.{jv.Type}");
                        }

                        break;

                    case string s:

                        formattedArgs.Add($"\"{s}\"");
                        break;

                    case int i:

                        formattedArgs.Add(i.ToString());
                        break;

                    case double d:

                        formattedArgs.Add(d.ToString());
                        break;

                    case bool b:

                        formattedArgs.Add(b.ToString().ToLowerInvariant());
                        break;

                    case List<object> list:

                        formattedArgs.Add($"[{ProcessArguments(list)}]");
                        break;

                    case JConstructor con:

                        formattedArgs.Add(FromJConstructor(con).ReferenceExpression);
                        break;

                    case JArray ja:

                        formattedArgs.Add($"[{ProcessArguments(ja.Values<object>().ToList())}]");
                        break;

                    default:

                        throw new InvalidOperationException($"Unexpected type.{item.GetType().FullName}");
                }
            }

            return string.Join(", ", formattedArgs);
        }
    }
}