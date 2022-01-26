namespace Firefly.PSCloudFormation.Terraform.DependencyResolver
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Firefly.CloudFormationParser.Intrinsics;
    using Firefly.CloudFormationParser.Intrinsics.Functions;
    using Firefly.CloudFormationParser.Utils;
    using Firefly.PSCloudFormation.Terraform.CloudFormationParser;
    using Firefly.PSCloudFormation.Terraform.Hcl;

    /// <summary>
    /// Object to store intrinsic functions extracted by visiting the CloudFormation resource object
    /// </summary>
    [DebuggerDisplay("{PropertyPath.Path}: {Intrinsic}")]
    internal class IntrinsicInfo
    {
        /// <summary>
        /// The intrinsic
        /// </summary>
        protected IIntrinsic intrinsic;

        /// <summary>
        /// The target resource
        /// </summary>
        protected ResourceMapping targetResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrinsicInfo"/> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="intrinsic">The intrinsic.</param>
        /// <param name="resourceMapping">Summary info of the resource targeted by this intrinsic.</param>
        /// <param name="evaluation">The evaluation.</param>
        public IntrinsicInfo(
            PropertyPath propertyPath,
            IIntrinsic intrinsic,
            ResourceMapping resourceMapping,
            object evaluation)
        {
            this.targetResource = resourceMapping;
            this.intrinsic = intrinsic;
            this.PropertyPath = propertyPath.Clone();
            this.InitialEvaluation = evaluation;
            this.intrinsic.ExtraData = this;
        }

        /// <summary>
        /// Gets the evaluation.
        /// </summary>
        /// <value>
        /// The evaluation.
        /// </value>
        public virtual object Evaluation => this.Intrinsic.Evaluate(this);

        /// <summary>
        /// Gets the initial evaluation.
        /// </summary>
        /// <value>
        /// The initial evaluation.
        /// </value>
        public object InitialEvaluation { get; }

        /// <summary>
        /// Gets the intrinsic.
        /// </summary>
        /// <value>
        /// The intrinsic.
        /// </value>
        public virtual IIntrinsic Intrinsic => this.intrinsic;

        /// <summary>
        /// Gets the list of nested intrinsic
        /// </summary>
        /// <value>
        /// The list of nested intrinsic.
        /// </value>
        // ReSharper disable once CollectionNeverQueried.Local - Really only here for use when debugging to see what was read.
        public IList<IntrinsicInfo> NestedIntrinsics { get; } = new List<IntrinsicInfo>();

        /// <summary>
        /// Gets the property path.
        /// </summary>
        /// <value>
        /// The property path.
        /// </value>
        public PropertyPath PropertyPath { get; }

        /// <summary>
        /// Gets the summary info of the resource targeted by this intrinsic.
        /// </summary>
        /// <value>
        /// The targeted resource. Will be <c>null</c> when reference is not to a resource.
        /// </value>
        public virtual ResourceMapping TargetResource => this.targetResource;

        /// <summary>
        /// Gets the type object targeted by the current intrinsic.
        /// </summary>
        /// <value>
        /// The type of the target.
        /// </value>
        public IntrinsicTargetType TargetType
        {
            get
            {
                if (this.TargetResource == null && this.Intrinsic is RefIntrinsic)
                {
                    return IntrinsicTargetType.Input;
                }

                if (this.TargetResource?.Module != null)
                {
                    return IntrinsicTargetType.Module;
                }

                return this.TargetResource != null ? IntrinsicTargetType.Resource : IntrinsicTargetType.Unknown;
            }
        }
    }
}