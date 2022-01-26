namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;

    using FluentAssertions;

    using Xunit;

    public class EventQueueTests
    {
        private readonly EventQueue Q;

        public EventQueueTests()
        {
            this.Q = new EventQueue(EventQueues.Ec2Events);
        }

        [Fact]
        public void ShouldFindKeyByPath()
        {
            var k = this.Q.FindKeyByPath("root_block_device.0.throughput");
            k.Should().NotBeNull();
        }

        [Fact]
        public void ShouldFindKeyByReference()
        {
            var key = EventQueues.Ec2Events.First(e => e is MappingKey);

            this.Q.Find(key).Should().NotBeNull();
        }

        [Fact]
        public void ShouldFindKeyByValue()
        {
            var key = new MappingKey(
                "user_data_base64",
                new AttributePath("user_data_base64"),
                EventQueues.Ec2InstanceSchema.GetAttributeByPath("user_data_base64"));

            this.Q.Find(key).Should().NotBeNull();
        }

        [Fact]
        public void ShouldRemoveCollectionValue()
        {
            var initalLength = this.Q.Count;
            var keyToRemove = this.Q.GetKeys().First(k => k.Path == "timeouts");

            this.Q.ConsumeKey(keyToRemove);

            this.Q.Count.Should().Be(initalLength - 9);
        }

        [Fact]
        public void ShouldRemoveScalarValue()
        {
            var initalLength = this.Q.Count;
            var keyToRemove = this.Q.GetKeys().First(k => k.Path == "id");

            this.Q.ConsumeKey(keyToRemove);

            this.Q.Count.Should().Be(initalLength - 2);
        }

        [Fact]
        public void ShouldReturnAllConflictingKeys()
        {
            const string AttributeName = "network_interface";
            var attribute = this.Q.GetKeys().First(k => k.Path == AttributeName);
            var conflictsWithAttribute = new[]
                                             {
                                                 "associate_public_ip_address", "subnet_id", "private_ip",
                                                 "secondary_private_ips", "vpc_security_group_ids", "security_groups",
                                                 "ipv6_addresses", "ipv6_address_count", "source_dest_check"
                                             };

            this.Q.GetConflictingAttributes(attribute).Select(k => k.Path).Should().Contain(conflictsWithAttribute);
        }

        [Fact]
        public void ShouldReturnAllConflictingKeysWhenTopLevelKeyConflictsWithBlockKeys()
        {
            const string AttributeName = "root_block_device.0.tags";
            var attribute = this.Q.GetKeys().First(k => k.Path == AttributeName);
            var conflictsWithAttribute = new[] { "volume_tags" };

            this.Q.GetConflictingAttributes(attribute).Select(k => k.Path).Should().Contain(conflictsWithAttribute);
        }

        [Fact]
        public void TestQueuePop()
        {
            var len = this.Q.Count;

            var e = this.Q.Dequeue();
            this.Q.Count.Should().Be(len - 1);
            e.Should().BeOfType<ResourceStart>();
        }
    }
}