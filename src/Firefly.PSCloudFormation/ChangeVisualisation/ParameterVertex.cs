namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    [DebuggerDisplay("Parameter: {Name}")]

    internal class ParameterVertex : IChangeVertex
    {
        public ParameterVertex(string parameterName)
        {
            this.Name = parameterName;
        }

        public string Name { get; }

        public override string ToString()
        {
            return $"P: {this.Name}";
        }
    }
}
