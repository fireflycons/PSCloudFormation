namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    internal abstract class Reference
    {
        private const string JConstructorName = "Reference";

        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        //private static readonly Regex constructorNameRegex = new Regex(@"^(?<type>[\w\.]+):(?<address>[^\d][\w].*\.[^\d][\w]*)(:(?<index>\d+))?");
        private static readonly Regex constructorNameRegex = new Regex(@"^(?<type>[\w\.]+):(?<address>[^\d][\w\[\]]*(\.[^\d][\w\[\]]*)*)(:(?<index>\d+))?");

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
                       ? new JConstructor(JConstructorName, this.GetType().FullName, this.ObjectAddress)
                       : new JConstructor(JConstructorName, this.GetType().FullName, this.ObjectAddress, this.Index);
        }

        public static Reference FromJConstructor(JConstructor constructor)
        {
            if (constructor.Name != JConstructorName)
            {
                throw new InvalidOperationException($"{constructor.Name} is not a valid internal reference type.");
            }

            var type = ExecutingAssembly.GetType(constructor[0].Value<string>());
            var objectAddress = constructor[1].Value<string>();
            var index = -1;

            if (constructor.Count > 2)
            {
                index = constructor[2].Value<int>();
            }

            return index == -1
                       ? (Reference)Activator.CreateInstance(type, objectAddress)
                       : (Reference)Activator.CreateInstance(type, objectAddress, index);
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
