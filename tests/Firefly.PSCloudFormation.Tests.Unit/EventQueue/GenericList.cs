namespace Firefly.PSCloudFormation.Tests.Unit.EventQueue
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class GenericList
    {
        [Fact]
        public void Add_IsAddToBack()
        {
            var equeue1 = new EmitterEventQueue<int>(new[] { 1, 2 });
            var equeue2 = new EmitterEventQueue<int>(new[] { 1, 2 });
            ((ICollection<int>)equeue1).Add(3);
            equeue2.AddToBack(3);
            equeue1.Should().BeEquivalentTo(equeue2);

            // Assert.IsTrue(equeue1.SequenceEqual(equeue2));
        }

        [Fact]
        public void Contains_ItemNotPresent_ReturnsFalse()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            equeue.Contains(3).Should().BeFalse();
        }

        [Fact]
        public void Contains_ItemPresent_ReturnsTrue()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            equeue.Contains(2).Should().BeTrue();
        }

        [Fact]
        public void CopyTo_CopiesItems()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            ((ICollection<int>)equeue).CopyTo(results, 0);
        }

        [Fact]
        public void CopyTo_InsufficientSpace_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            var action = new Action(() => { ((ICollection<int>)equeue).CopyTo(results, 1); });

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CopyTo_NegativeOffset_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var results = new int[3];
            var action = new Action(() => { ((ICollection<int>)equeue).CopyTo(results, -1); });

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CopyTo_NullArray_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => { ((ICollection<int>)equeue).CopyTo(null, 0); });

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenericIsReadOnly_ReturnsFalse()
        {
            var equeue = new EmitterEventQueue<int>();
            (((ICollection<int>)equeue).IsReadOnly).Should().BeFalse();
        }

        [Fact]
        public void IndexOf_ItemNotPresent_ReturnsNegativeOne()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            var result = equeue.IndexOf(3);
            result.Should().Be(-1);
        }

        [Fact]
        public void IndexOf_ItemPresent_ReturnsItemIndex()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            var result = equeue.IndexOf(2);
            result.Should().Be(1);
        }

        [Fact]
        public void NonGenericEnumerator_EnumeratesItems()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2 });
            var results = new List<object>();
            var objEnum = ((System.Collections.IEnumerable)equeue).GetEnumerator();
            while (objEnum.MoveNext())
            {
                results.Add(objEnum.Current);
            }

            equeue.Should().BeEquivalentTo(results.Cast<int>());

            // Assert.IsTrue(equeue.SequenceEqual(results.Cast<int>()));
        }
    }
}