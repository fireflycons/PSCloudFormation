// ReSharper disable UnassignedGetOnlyAutoProperty
namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Firefly.CloudFormationParser;

    internal class DummyParameter : IParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DummyParameter"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public DummyParameter(string name)
        {
            this.Name = name;
        }

        /// <inheritdoc />
        public Regex AllowedPattern { get; }

        /// <inheritdoc />
        public List<string> AllowedValues { get; }

        /// <inheritdoc />
        public string ConstraintDescription { get; }

        /// <inheritdoc />
        public object CurrentValue { get; }

        /// <inheritdoc />
        public string Default { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public bool HasMaxLength { get; }

        /// <inheritdoc />
        public bool HasMaxValue { get; }

        /// <inheritdoc />
        public bool HasMinLength { get; }

        /// <inheritdoc />
        public bool HasMinValue { get; }

        /// <inheritdoc />
        public bool IsSsmParameter { get; }

        /// <inheritdoc />
        public int? MaxLength { get; }

        /// <inheritdoc />
        public double? MaxValue { get; }

        /// <inheritdoc />
        public int? MinLength { get; }

        /// <inheritdoc />
        public double? MinValue { get; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public bool NoEcho { get; }

        /// <inheritdoc />
        public ITemplate Template { get; set; }

        /// <inheritdoc />
        public string Type { get; }

        /// <inheritdoc />
        public Type GetClrType()
        {
            return typeof(string);
        }

        /// <inheritdoc />
        public object GetCurrentValue()
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public void SetCurrentValue(IDictionary<string, object> parameterValues)
        {
        }
    }
}