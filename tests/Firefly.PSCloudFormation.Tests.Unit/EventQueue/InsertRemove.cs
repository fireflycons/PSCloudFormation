namespace Firefly.PSCloudFormation.Tests.Unit.EventQueue
{
    using System;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class InsertRemove
    {
        [Fact]
        public void Clear_DoesNotChangeCapacity()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            equeue.Capacity.Should().Be(3);
            equeue.Clear();
            equeue.Capacity.Should().Be(3);
        }

        [Fact]
        public void Clear_EmptiesAllItems()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            equeue.Clear();

            equeue.Should().HaveCount(0).And.BeEquivalentTo(new int[] { });
        }

        [Fact]
        public void Insert_AtCount_IsSameAsAddToBack()
        {
            var equeue1 = new EmitterEventQueue<int>(new[] { 1, 2 });
            var equeue2 = new EmitterEventQueue<int>(new[] { 1, 2 });
            equeue1.Insert(equeue1.Count, 0);
            equeue2.AddToBack(0);
            equeue1.Should().BeEquivalentTo(equeue2);
        }

        [Fact]
        public void Insert_IndexTooLarge_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue.Insert(equeue.Count + 1, 0));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Insert_InsertsElementAtIndex()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            equeue.Insert(1, 13);
            equeue.Should().BeEquivalentTo(new[] { 1, 13, 2 });
        }

        [Fact]
        public void Insert_NegativeIndex_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue.Insert(-1, 0));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Remove_ItemNotPresent_KeepsItemsReturnsFalse()
        {
            var sequence = new[] { 1, 2, 3, 4 };

            var equeue = new EmitterEventQueue<int>(sequence);
            equeue.Remove(5).Should().BeFalse();
            equeue.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void Remove_ItemPresent_RemovesItemAndReturnsTrue()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3, 4 });
            equeue.Remove(3).Should().BeTrue();
            equeue.Should().BeEquivalentTo(new[] { 1, 2, 4 });
        }

        [Fact]
        public void RemoveAt_Index0_IsSameAsRemoveFromFront()
        {
            var equeue1 = new EmitterEventQueue<int>(new[] { 1, 2 });
            var equeue2 = new EmitterEventQueue<int>(new[] { 1, 2 });
            equeue1.RemoveAt(0);
            equeue2.RemoveFromFront();
            equeue1.Should().BeEquivalentTo(equeue2);
        }

        [Fact]
        public void RemoveAt_IndexTooLarge_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue.RemoveAt(equeue.Count));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveAt_NegativeIndex_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue.RemoveAt(-1));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveFromFront_Empty_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>();
            var action = new Func<int>(() => equeue.RemoveFromFront());

            action.Should().Throw<InvalidOperationException>();
        }
    }
}