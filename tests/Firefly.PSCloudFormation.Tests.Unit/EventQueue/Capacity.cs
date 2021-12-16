namespace Firefly.PSCloudFormation.Tests.Unit.EventQueue
{
    using System;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class Capacity
    {
        [Fact]
        public void Set_PreservesData()
        {
            var sequence = new int[] { 1, 2, 3 };

            var equeue = new EmitterEventQueue<int>(sequence);
            equeue.Capacity.Should().Be(3);

            equeue.Capacity = 7;
            equeue.Capacity.Should().Be(7);
            equeue.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void Set_SmallerThanCount_ThrowsException()
        {
            var sequence = new int[] { 1, 2, 3 };
            var expected = new[] { 2, 3, 4 };

            var equeue = new EmitterEventQueue<int>(sequence);
            equeue.Capacity.Should().Be(3);

            var action = new Action(() => { equeue.Capacity = 2; });

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Set_ToItself_DoesNothing()
        {
            var equeue = new EmitterEventQueue<int>(13);
            equeue.Capacity.Should().Be(13);
            equeue.Capacity = 13;
            equeue.Capacity.Should().Be(13);
        }

        [Fact]
        public void Set_WhenSplit_PreservesData()
        {
            var sequence = new int[] { 1, 2, 3 };
            var expected = new[] { 2, 3, 4 };

            var equeue = new EmitterEventQueue<int>(sequence);
            equeue.RemoveFromFront();
            equeue.AddToBack(4);
            equeue.Capacity.Should().Be(3);
            equeue.Capacity = 7;
            equeue.Capacity.Should().Be(7);
            equeue.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void SetLarger_UsesSpecifiedCapacity()
        {
            var equeue = new EmitterEventQueue<int>(1);
            equeue.Capacity.Should().Be(1);
            equeue.Capacity = 17;
            equeue.Capacity.Should().Be(17);
        }

        [Fact]
        public void SetSmaller_UsesSpecifiedCapacity()
        {
            var equeue = new EmitterEventQueue<int>(13);
            equeue.Capacity.Should().Be(13);
            equeue.Capacity = 7;
            equeue.Capacity.Should().Be(7);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SetToLessThan1_ThrowsException(int capacity)
        {
            var equeue = new EmitterEventQueue<int>();

            var action = new Action(() => { equeue.Capacity = capacity; });

            action.Should().Throw<ArgumentException>();
        }
    }
}