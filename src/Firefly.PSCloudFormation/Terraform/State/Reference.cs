namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    internal abstract class Reference
    {
        private static readonly Regex constructorNameRegex = new Regex(@"^(?<type>[\w\.]+):(?<address>.*)");
        public Reference(string objectAddress)
        {
            this.ObjectAddress = objectAddress;
        }

        protected string ObjectAddress { get;  }

        public abstract string ReferenceExpression { get; }

        public JConstructor ToJConstructor()
        {
            return new JConstructor($"{this.GetType().FullName}:{this.ObjectAddress}");
        }

        public static Reference FromJConstructor(JConstructor constructor)
        {
            var mc = constructorNameRegex.Match(constructor.Name);

            var type = Assembly.GetCallingAssembly().GetType(mc.Groups["type"].Value);
            var reference = (Reference)Activator.CreateInstance(type, mc.Groups["address"].Value);

            return reference;
        }
    }
}
