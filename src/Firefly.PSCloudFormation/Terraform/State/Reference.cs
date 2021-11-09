namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    internal abstract class Reference
    {
        private static readonly Regex constructorNameRegex = new Regex(@"^(?<type>[\w\.]+):(?<address>[^\d][\w].*\.[^\d][\w]*)(:(?<index>\d+))?");

        public Reference(string objectAddress, int index)
        : this(objectAddress)
        {
            this.Index = index;
        }

        public Reference(string objectAddress)
        {
            this.ObjectAddress = objectAddress;
        }

        protected string ObjectAddress { get;  }

        protected int Index { get; } = -1;

        public abstract string ReferenceExpression { get; }

        public JConstructor ToJConstructor()
        {
            return this.Index == -1
                       ? new JConstructor($"{this.GetType().FullName}:{this.ObjectAddress}")
                       : new JConstructor($"{this.GetType().FullName}:{this.ObjectAddress}:{this.Index}");
        }

        public static Reference FromJConstructor(JConstructor constructor)
        {
            var mc = constructorNameRegex.Match(constructor.Name);

            var type = Assembly.GetCallingAssembly().GetType(mc.Groups["type"].Value);
            var index = mc.Groups["index"].Value;

            return index == string.Empty
                       ? (Reference)Activator.CreateInstance(type, mc.Groups["address"].Value)
                       : (Reference)Activator.CreateInstance(type, mc.Groups["address"].Value, int.Parse(index));
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.ReferenceExpression;
        }
    }
}
