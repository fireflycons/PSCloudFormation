namespace Firefly.PSCloudFormation.Terraform.State
{
    using System.Collections.Generic;

    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Subclass of FunctionReference with handling for <c>!Join</c>
    /// </summary>
    /// <seealso cref="Firefly.PSCloudFormation.Terraform.State.FunctionReference" />
    internal class JoinFunctionReference : FunctionReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFunctionReference"/> class.
        /// </summary>
        /// <param name="joinIntrinsic">The join intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="inputs">The inputs.</param>
        public JoinFunctionReference(JoinIntrinsic joinIntrinsic, ITemplate template, IList<InputVariable> inputs)
            : this("join", FunctionArguments(joinIntrinsic, template, inputs))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="functionArguments">The function arguments.</param>
        /// <remarks>
        /// <para>
        /// The arguments should be arranged as the per the terraform function's arguments, e.g.
        /// join function has two arguments, a separator and a list of things to join.
        /// Thus, <paramref name="functionArguments" /> should be an enumerable of two items,
        /// the first being string, and the second being a list of objects.
        /// </para>
        /// <para>
        /// Where a function argument is another <see cref="Reference" />, call <see cref="Reference.ToJConstructor" />
        /// on it first and add the JConstructor object to the argument list.
        /// </para>
        /// </remarks>
        public JoinFunctionReference(string functionName, IEnumerable<object> functionArguments)
            : base(functionName, functionArguments)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="functionArguments">The function arguments.</param>
        /// <param name="index">Indexer to apply to function call if &gt;= 0</param>
        /// <remarks>
        /// <para>
        /// The arguments should be arranged as the per the terraform function's arguments, e.g.
        /// join function has two arguments, a separator and a list of things to join.
        /// Thus, <paramref name="functionArguments" /> should be an enumerable of two items,
        /// the first being string, and the second being a list of objects.
        /// </para>
        /// <para>
        /// Where a function argument is another <see cref="Reference" />, call <see cref="Reference.ToJConstructor" />
        /// on it first and add the JConstructor object to the argument list.
        /// </para>
        /// </remarks>
        public JoinFunctionReference(string functionName, IEnumerable<object> functionArguments, int index)
            : base(functionName, functionArguments, index)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="index">The index.</param>
        protected JoinFunctionReference(string functionName, int index)
            : base(functionName, index)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFunctionReference"/> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        protected JoinFunctionReference(string functionName)
            : base(functionName)
        {
        }

        /// <summary>
        /// Renders the join function arguments as a list that can be passed to the base class constructor.
        /// </summary>
        /// <param name="joinIntrinsic">The join intrinsic.</param>
        /// <param name="template">The template.</param>
        /// <param name="inputs">The inputs.</param>
        /// <returns>List of join arguments, with intrinsic functions rendered.</returns>
        private static IEnumerable<object> FunctionArguments(
            JoinIntrinsic joinIntrinsic,
            ITemplate template,
            IList<InputVariable> inputs)
        {
            // Build up a join() function reference
            var joinArguments = new List<object> { joinIntrinsic.Separator };
            var joinList = new List<object>();

            foreach (var item in joinIntrinsic.Items)
            {
                switch (item)
                {
                    case IIntrinsic nestedIntrinsic:

                        joinList.Add(
                            nestedIntrinsic.Render(template, nestedIntrinsic.GetInfo().TargetResource, inputs).ToJConstructor());
                        break;

                    default:

                        // join() is a string function - all args are therefore string
                        joinList.Add(item.ToString());
                        break;
                }
            }

            joinArguments.Add(joinList);

            return joinArguments;
        }
    }
}