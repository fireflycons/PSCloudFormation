namespace Firefly.PSCloudFormation.Utils.JsonTraversal
{
    /// <summary>
    /// A empty implementation of the <see cref="IJsonVisitorContext{TContext}"/>.
    /// This just returns it self as the json is traversed and otherwise does nothing.
    /// </summary>
    internal class NullJsonVisitorContext : IJsonVisitorContext<NullJsonVisitorContext>
    {
        /// <inheritdoc />
        public NullJsonVisitorContext Next(int index) => this;

        /// <inheritdoc />
        public NullJsonVisitorContext Next(string name) => this;
    }
}