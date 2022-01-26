namespace Firefly.PSCloudFormation.Terraform.HclSerializer.Events
{
    internal class ResourceStart : HclEvent
    {
        public ResourceStart(string type, string name)
        {
            this.ResourceType = type;
            this.ResourceName = name;
        }

        public string ResourceName { get; }

        public string ResourceType { get; }

        /// <inheritdoc />
        internal override EventType Type => EventType.ResourceStart;

        public override string Repr()
        {
            return $"new {this.GetType().Name}(\"{this.ResourceType}\", \"{this.ResourceName}\")";
        }
    }
}