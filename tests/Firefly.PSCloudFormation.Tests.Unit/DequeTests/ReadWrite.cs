namespace Firefly.PSCloudFormation.Tests.Unit.DequeTests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Firefly.PSCloudFormation.Utils;

    using FluentAssertions;

    using Xunit;

    public class ReadWrite
    {
        [Fact]
        public void GetItem_IndexTooLarge_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Func<int>(() => deque[3]);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetItem_NegativeIndex_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Func<int>(() => deque[-1]);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetItem_ReadsElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });

            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
            deque[2].Should().Be(3);
        }

        [Fact]
        public void GetItem_Split_ReadsElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);

            deque[0].Should().Be(0);
            deque[1].Should().Be(1);
            deque[2].Should().Be(2);
        }

        [Fact]
        public void GetItem_EmptyQueue_ThrowsException()
        {
            var deque = new Deque<int>();

            var action = new Func<int>(() => deque[0]);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_IndexTooLarge_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque[3] = 13);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_NegativeIndex_ThrowsException()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var action = new Action(() => deque[-1] = 13);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_Split_WritesElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.RemoveFromBack();
            deque.AddToFront(0);
            deque[0] = 7;
            deque[1] = 11;
            deque[2] = 13;
            deque.Should().BeEquivalentTo(new[] { 7, 11, 13 });
        }

        [Fact]
        public void SetItem_WritesElements()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque[0] = 7;
            deque[1] = 11;
            deque[2] = 13;
            deque.Should().BeEquivalentTo(new[] { 7, 11, 13 });
        }

        [Fact]
        public void Peek_ReturnsItemAtFromt()
        {
            var sequence = new[] { 1, 2, 3 };

            var deque = new Deque<int>(sequence);

            deque.Peek().Should().Be(1);
            deque.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void PeekUntil_ReturnsItemsTillConditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 2, 3 };
            var deque = new Deque<int>(sequence);

            deque.PeekUntil(i => i > 3).ToList().Should().BeEquivalentTo(expected);
            deque.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void PeekUntil_WithEmitLastItem_ReturnsItemsTillAndIncludingConditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 2, 3, 4 };
            var deque = new Deque<int>(sequence);

            deque.PeekUntil(i => i > 3, true).ToList().Should().BeEquivalentTo(expected);
            deque.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void PeekUntil_ShouldThrowIfQueueIsExhaustedBeforeConditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var deque = new Deque<int>(sequence);

            var action = new Func<List<int>>(() => deque.PeekUntil(i => i > 7).ToList());

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ConsumeUntil_ShouldRemoveItemsUntilComditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 4, 5 };
            var deque = new Deque<int>(sequence);

            deque.ConsumeUntil(i => i > 3);

            deque.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ConsumeUntil_WithConsumeLastItem_ShouldRemoveItemsTillAndIncludingComditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 5 };
            var deque = new Deque<int>(sequence);

            deque.ConsumeUntil(i => i > 3, true);

            deque.Should().BeEquivalentTo(expected);
        }
    }
}