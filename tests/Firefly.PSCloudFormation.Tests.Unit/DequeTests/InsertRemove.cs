namespace Firefly.PSCloudFormation.Tests.Unit.DequeTests
{
    using System;

    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    public class InsertRemove
    {
        [Fact]
        public void Clear_DoesNotChangeCapacity()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.Capacity.Should().Be(3);
            deque.Clear();
            deque.Capacity.Should().Be(3);
        }

        [Fact]
        public void Clear_EmptiesAllItems()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.Clear();

            deque.Should().HaveCount(0).And.BeEquivalentTo(new int[] { });
        }

        [Fact]
        public void Insert_AtCount_IsSameAsAddToBack()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.Insert(deque1.Count, 0);
            deque2.AddToBack(0);
            deque1.Should().BeEquivalentTo(deque2);
        }

        [Fact]
        public void Insert_AtIndex0_IsSameAsAddToFront()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.Insert(0, 0);
            deque2.AddToFront(0);
            deque1.Should().BeEquivalentTo(deque2);
        }

        [Fact]
        public void Insert_IndexTooLarge_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque.Insert(deque.Count + 1, 0));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Insert_InsertsElementAtIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            deque.Insert(1, 13);
            deque.Should().BeEquivalentTo(new[] { 1, 13, 2 });
        }

        [Fact]
        public void Insert_NegativeIndex_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque.Insert(-1, 0));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Remove_ItemNotPresent_KeepsItemsReturnsFalse()
        {
            var sequence = new[] { 1, 2, 3, 4 };

            var deque = new Deque<int>(sequence);
            deque.Remove(5).Should().BeFalse();
            deque.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void Remove_ItemPresent_RemovesItemAndReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3, 4 });
            deque.Remove(3).Should().BeTrue();
            deque.Should().BeEquivalentTo(new[] { 1, 2, 4 });
        }

        [Fact]
        public void RemoveAt_Index0_IsSameAsRemoveFromFront()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.RemoveAt(0);
            deque2.RemoveFromFront();
            deque1.Should().BeEquivalentTo(deque2);
        }

        [Fact]
        public void RemoveAt_IndexTooLarge_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque.RemoveAt(deque.Count));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveAt_LastIndex_IsSameAsRemoveFromFront()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            deque1.RemoveAt(1);
            deque2.RemoveFromBack();
            deque1.Should().BeEquivalentTo(deque2);
        }

        [Fact]
        public void RemoveAt_NegativeIndex_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque.RemoveAt(-1));

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveAt_RemovesElementAtIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            deque.RemoveAt(1);

            deque.Should().BeEquivalentTo(new[] { 0, 2 });
        }

        [Fact]
        public void RemoveFromBack_Empty_ThrowsException()
        {
            var deque = new Deque<int>();
            var action = new Func<int>(() => deque.RemoveFromBack());

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RemoveFromFront_Empty_ThrowsException()
        {
            var deque = new Deque<int>();
            var action = new Func<int>(() => deque.RemoveFromFront());

            action.Should().Throw<InvalidOperationException>();
        }
    }
}