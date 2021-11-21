namespace Firefly.PSCloudFormation.Tests.Unit.DequeTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    public class GenericList
    {
        [Fact]
        public void Add_IsAddToBack()
        {
            var deque1 = new Deque<int>(new[] { 1, 2 });
            var deque2 = new Deque<int>(new[] { 1, 2 });
            ((ICollection<int>)deque1).Add(3);
            deque2.AddToBack(3);
            deque1.Should().BeEquivalentTo(deque2);

            // Assert.IsTrue(deque1.SequenceEqual(deque2));
        }

        [Fact]
        public void Contains_ItemNotPresent_ReturnsFalse()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            deque.Contains(3).Should().BeFalse();
        }

        [Fact]
        public void Contains_ItemPresent_ReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            deque.Contains(2).Should().BeTrue();
        }

        [Fact]
        public void Contains_ItemPresentAndSplit_ReturnsTrue()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);

            deque.Contains(0).Should().BeTrue();
            deque.Contains(1).Should().BeTrue();
            deque.Contains(2).Should().BeTrue();
            deque.Contains(3).Should().BeFalse();
        }

        [Fact]
        public void CopyTo_CopiesItems()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            ((ICollection<int>)deque).CopyTo(results, 0);
        }

        [Fact]
        public void CopyTo_InsufficientSpace_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            var action = new Action(() => { ((ICollection<int>)deque).CopyTo(results, 1); });

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CopyTo_NegativeOffset_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            var action = new Action(() => { ((ICollection<int>)deque).CopyTo(results, -1); });

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CopyTo_NullArray_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => { ((ICollection<int>)deque).CopyTo(null, 0); });

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenericIsReadOnly_ReturnsFalse()
        {
            var deque = new Deque<int>();
            (((ICollection<int>)deque).IsReadOnly).Should().BeFalse();
        }

        [Fact]
        public void IndexOf_ItemNotPresent_ReturnsNegativeOne()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var result = deque.IndexOf(3);
            result.Should().Be(-1);
        }

        [Fact]
        public void IndexOf_ItemPresent_ReturnsItemIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var result = deque.IndexOf(2);
            result.Should().Be(1);
        }

        [Fact]
        public void IndexOf_ItemPresentAndSplit_ReturnsItemIndex()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            deque.IndexOf(0).Should().Be(0);
            deque.IndexOf(1).Should().Be(1);
            deque.IndexOf(2).Should().Be(2);
        }

        [Fact]
        public void NonGenericEnumerator_EnumeratesItems()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var results = new List<object>();
            var objEnum = ((System.Collections.IEnumerable)deque).GetEnumerator();
            while (objEnum.MoveNext())
            {
                results.Add(objEnum.Current);
            }

            deque.Should().BeEquivalentTo(results.Cast<int>());

            // Assert.IsTrue(deque.SequenceEqual(results.Cast<int>()));
        }
    }
}