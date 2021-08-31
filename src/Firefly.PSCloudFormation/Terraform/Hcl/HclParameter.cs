namespace Firefly.PSCloudFormation.Terraform.Hcl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Amazon.CloudFormation.Model;

    internal abstract class HclParameter
    {
        public const string DefaultDecalaration = "  default     = ";

        protected HclParameter(ParameterDeclaration stackParameter)
        {
            this.Name = stackParameter.ParameterKey;
            this.Description = stackParameter.Description;
            this.Sensitive = stackParameter.NoEcho;
            this.DefaultValue = stackParameter.DefaultValue;
        }

        public bool Sensitive { get; }

        public string Name { get; }

        public string Description { get; }

        public abstract string Type { get; }

        public string DefaultValue { get; }

        public bool IsScalar => !this.Type.StartsWith("list(");

        public static HclParameter CreateParameter(ParameterDeclaration stackParameter)
        {
            switch (stackParameter.ParameterType)
            {
                case "String":
                case "AWS::EC2::AvailabilityZone::Name":
                case "AWS::EC2::Image::Id":
                case "AWS::EC2::Instance::Id":
                case "AWS::EC2::KeyPair::KeyName":
                case "AWS::EC2::SecurityGroup::GroupName":
                case "AWS::EC2::SecurityGroup::Id":
                case "AWS::EC2::Subnet::Id":
                case "AWS::EC2::Volume::Id":
                case "AWS::EC2::VPC::Id":
                case "AWS::Route53::HostedZone::Id":
                    
                    return new HclStringParameter(stackParameter);

                case "List<String>":
                case "CommaDelimitedList":
                case "List<AWS::EC2::AvailabilityZone::Name>":
                case "List<AWS::EC2::Image::Id>":
                case "List<AWS::EC2::Instance::Id>":
                case "List<AWS::EC2::SecurityGroup::GroupName>":
                case "List<AWS::EC2::SecurityGroup::Id>":
                case "List<AWS::EC2::Subnet::Id>":
                case "List<AWS::EC2::Volume::Id":
                case "List<AWS::EC2::VPC::Id>":
                case "List<AWS::Route53::HostedZone::Id>":

                    return new HclStringListParameter(stackParameter);

                case "Number":

                    return new HclNumberParameter(stackParameter);

                case "List<Number>":

                    return new HclNumberListParameter(stackParameter);
            }

            return null;
        }

        public string GenerateHcl()
        {
            var hcl = new StringBuilder();

            hcl.AppendLine($"variable \"{this.Name}\" {{");
            hcl.AppendLine($"  type        = {this.Type}");

            if (!string.IsNullOrEmpty(this.Description))
            {
                hcl.AppendLine($"  description = \"{this.Description}\"");
            }

            if (this.Sensitive)
            {
                hcl.AppendLine("  sensitive = true");
            }

            hcl.AppendLine(this.GeneratetDefaultStanza());
            hcl.AppendLine("}");

            return hcl.ToString();
        }

        protected abstract string GeneratetDefaultStanza();
    }

    internal class HclStringParameter : HclParameter
    {
        public HclStringParameter(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        public override string Type => "string";

        protected override string GeneratetDefaultStanza()
        {
            var defaultValue = string.IsNullOrEmpty(this.DefaultValue) ? string.Empty : this.DefaultValue;

            return $"{DefaultDecalaration}\"{defaultValue}\"";
        }
    }

    internal class HclStringListParameter : HclParameter
    {
        public HclStringListParameter(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        public override string Type => "list(string)";

        protected override string GeneratetDefaultStanza()
        {
            var hcl = new StringBuilder();

            var defaultValue = string.IsNullOrEmpty(this.DefaultValue)
                                   ? new List<string> { string.Empty }
                                   : this.DefaultValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            hcl.AppendLine($"{DefaultDecalaration}[");
            foreach (var val in defaultValue)
            {
                hcl.AppendLine($"    \"{val}\",");
            }

            hcl.AppendLine("  ]");

            return hcl.ToString();
        }
    }

    internal class HclNumberParameter : HclParameter
    {
        public HclNumberParameter(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        public override string Type => "number";

        protected override string GeneratetDefaultStanza()
        {
            double defaultValue = 0;

            if (!string.IsNullOrEmpty(this.DefaultValue))
            {
                defaultValue = double.Parse(this.DefaultValue);
            }

            return $"{ DefaultDecalaration}{ defaultValue}";
        }

    }

    internal class HclNumberListParameter : HclParameter
    {
        public HclNumberListParameter(ParameterDeclaration stackParameter)
            : base(stackParameter)
        {
        }

        public override string Type => "list(number)";

        protected override string GeneratetDefaultStanza()
        {
            var hcl = new StringBuilder();

            var defaultValue = string.IsNullOrEmpty(this.DefaultValue)
                                   ? new List<double> { 0 }
                                   : this.DefaultValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToList();

            hcl.AppendLine($"{DefaultDecalaration}[");
            foreach (var val in defaultValue)
            {
                hcl.AppendLine($"    {val},");
            }

            hcl.AppendLine("  ]");

            return hcl.ToString();
        }
    }
}
