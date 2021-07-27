namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    [DebuggerDisplay("Direct: {Name}")]
    internal class DirectModificationVertex : IChangeVertex
    {
        public string Name => "Direct Modification";

        public override string ToString()
        {
            return $"D: {this.Name}";
        }
    }
}