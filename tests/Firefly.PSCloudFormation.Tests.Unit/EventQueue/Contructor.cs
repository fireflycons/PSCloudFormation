namespace Firefly.PSCloudFormation.Tests.Unit.EventQueue
{
    using System;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class Constructor
    {
        // Implementation detail: the default capacity.
        const int DefaultCapacity = 8;

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CapacityLessThan1_ThrowsException(int capacity)
        {
            var action = new Func<EmitterEventQueue<int>>(() => new EmitterEventQueue<int>(capacity));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FromSequence_InitializesFromSequence()
        {
            var sequence = new int[] { 1, 2, 3 };
            var equeue = new EmitterEventQueue<int>(sequence);

            equeue.Capacity.Should().Be(3);
            equeue.Should().BeEquivalentTo(sequence).And.HaveCount(3);
        }

        [Fact]
        public void NegativeCapacity_ThrowsException()
        {
            var action = new Func<EmitterEventQueue<int>>(() => new EmitterEventQueue<int>(-1));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithoutExplicitCapacity_UsesDefaultCapacity()
        {
            var equeue = new EmitterEventQueue<int>();
            equeue.Capacity.Should().Be(DefaultCapacity);
        }
    }
}