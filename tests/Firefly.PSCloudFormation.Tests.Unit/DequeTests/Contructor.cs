namespace Firefly.PSCloudFormation.Tests.Unit.DequeTests
{
    using System;

    using Firefly.PSCloudFormation.Utils;

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
            var deque = new EmitterEventQueue<int>(sequence);

            deque.Capacity.Should().Be(3);
            deque.Should().BeEquivalentTo(sequence).And.HaveCount(3);
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
            var deque = new EmitterEventQueue<int>();
            deque.Capacity.Should().Be(DefaultCapacity);
        }
    }
}