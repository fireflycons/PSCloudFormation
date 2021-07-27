namespace Firefly.PSCloudFormation.ChangeVisualisation
{
    using System.Diagnostics;

    using Amazon.CloudFormation.Model;

    [DebuggerDisplay("Resource: {Name}")]

    internal class ResourceVertex : IChangeVertex
    {
        public ResourceVertex(Change change)
        {
            this.Change = change;
        }

        public Change Change { get;  }

        public string Name => this.Change.ResourceChange.LogicalResourceId;

        public override string ToString()
        {
            return $"R: {this.Name}";
        }
    }
}