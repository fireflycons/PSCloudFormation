namespace Firefly.PSCloudFormation.Tests.Unit.DequeTests
{
    using System;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    public class Capacity
    {
        [Fact]
        public void Set_PreservesData()
        {
            var sequence = new int[] { 1, 2, 3 };

            var deque = new EmitterEventQueue<int>(sequence);
            deque.Capacity.Should().Be(3);

            deque.Capacity = 7;
            deque.Capacity.Should().Be(7);
            deque.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void Set_SmallerThanCount_ThrowsException()
        {
            var sequence = new int[] { 1, 2, 3 };
            var expected = new[] { 2, 3, 4 };

            var deque = new EmitterEventQueue<int>(sequence);
            deque.Capacity.Should().Be(3);

            var action = new Action(() => { deque.Capacity = 2; });

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Set_ToItself_DoesNothing()
        {
            var deque = new EmitterEventQueue<int>(13);
            deque.Capacity.Should().Be(13);
            deque.Capacity = 13;
            deque.Capacity.Should().Be(13);
        }

        [Fact]
        public void Set_WhenSplit_PreservesData()
        {
            var sequence = new int[] { 1, 2, 3 };
            var expected = new[] { 2, 3, 4 };

            var deque = new EmitterEventQueue<int>(sequence);
            deque.RemoveFromFront();
            deque.AddToBack(4);
            deque.Capacity.Should().Be(3);
            deque.Capacity = 7;
            deque.Capacity.Should().Be(7);
            deque.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void SetLarger_UsesSpecifiedCapacity()
        {
            var deque = new EmitterEventQueue<int>(1);
            deque.Capacity.Should().Be(1);
            deque.Capacity = 17;
            deque.Capacity.Should().Be(17);
        }

        [Fact]
        public void SetSmaller_UsesSpecifiedCapacity()
        {
            var deque = new EmitterEventQueue<int>(13);
            deque.Capacity.Should().Be(13);
            deque.Capacity = 7;
            deque.Capacity.Should().Be(7);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetToLessThan1_ThrowsException(int capacity)
        {
            var deque = new EmitterEventQueue<int>();

            var action = new Action(() => { deque.Capacity = capacity; });

            action.Should().Throw<ArgumentException>();
        }
    }
}