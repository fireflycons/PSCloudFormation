namespace Firefly.PSCloudFormation.Terraform.State
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    internal abstract class Reference
    {
        protected const string JConstructorName = "Reference";

        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

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

        public static implicit operator JConstructor(Reference reference)
        {
            return reference.ToJConstructor();
        }

        public virtual JConstructor ToJConstructor()
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

            if (constructor.Count < 2)
            {
                throw new InvalidOperationException("Invalid JConstructor found for Reference type. Too few arguments.");
            }

            if (constructor.Count == 2)
            {
                return (Reference)Activator.CreateInstance(type, objectAddress);
            }

            switch (constructor[2].Type)
            {
                case JTokenType.Integer:

                    return (Reference)Activator.CreateInstance(type, objectAddress, constructor[2].Value<int>());

                case JTokenType.Array:

                    return (Reference)Activator.CreateInstance(
                        type,
                        objectAddress,
                        constructor[2].Values<object>().ToList());

                default:

                    throw new InvalidOperationException($"Invalid JConstructor found for Reference type. Unexpected JToken '{constructor[2].Type}' found for argument #2.");
            }
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
