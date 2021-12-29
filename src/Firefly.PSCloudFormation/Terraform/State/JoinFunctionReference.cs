using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Terraform.State
{
    using Firefly.CloudFormationParser;
    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.DependencyResolver;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    internal class JoinFunctionReference : FunctionReference
    {
        public JoinFunctionReference(JoinIntrinsic joinIntrinsic, ITemplate template, IList<InputVariable> inputs)
        : this("join", FunctionArguments(joinIntrinsic, template, inputs))
        {

        }
        public JoinFunctionReference(string functionName, IEnumerable<object> functionArguments)
            : base(functionName, functionArguments)
        {
        }

        public JoinFunctionReference(string functionName, IEnumerable<object> functionArguments, int index)
            : base(functionName, functionArguments, index)
        {
        }

        protected JoinFunctionReference(string functionName, int index)
            : base(functionName, index)
        {
        }

        protected JoinFunctionReference(string functionName)
            : base(functionName)
        {
        }

        private static IEnumerable<object> FunctionArguments(JoinIntrinsic joinIntrinsic, ITemplate template, IList<InputVariable> inputs)
        {
            // Build up a join() function reference
            var joinArguments = new List<object> { joinIntrinsic.Separator };
            var joinList = new List<object>();
            var intrinsicInfo = (IntrinsicInfo)joinIntrinsic.ExtraData;

            foreach (var item in joinIntrinsic.Items)
            {
                switch (item)
                {
                    case IIntrinsic nestedIntrinsic:
                        
                        joinList.Add(nestedIntrinsic.Render(template, intrinsicInfo.TargetResource, inputs).ToJConstructor());
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
