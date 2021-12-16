namespace Firefly.PSCloudFormation.Tests.Unit.EventQueue
{
    using System;
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;

    using FluentAssertions;

    using Xunit;

    public class ReadWrite
    {
        [Fact]
        public void GetItem_IndexTooLarge_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Func<int>(() => equeue[3]);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetItem_NegativeIndex_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Func<int>(() => equeue[-1]);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetItem_ReadsElements()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });

            equeue[0].Should().Be(1);
            equeue[1].Should().Be(2);
            equeue[2].Should().Be(3);
        }

        [Fact]
        public void GetItem_EmptyQueue_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>();

            var action = new Func<int>(() => equeue[0]);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_IndexTooLarge_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue[3] = 13);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_NegativeIndex_ThrowsException()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            var action = new Action(() => equeue[-1] = 13);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_WritesElements()
        {
            var equeue = new EmitterEventQueue<int>(new[] { 1, 2, 3 });
            equeue[0] = 7;
            equeue[1] = 11;
            equeue[2] = 13;
            equeue.Should().BeEquivalentTo(new[] { 7, 11, 13 });
        }

        [Fact]
        public void Peek_ReturnsItemAtFromt()
        {
            var sequence = new[] { 1, 2, 3 };

            var equeue = new EmitterEventQueue<int>(sequence);

            equeue.Peek().Should().Be(1);
            equeue.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void PeekUntil_WithEmitLastItem_ReturnsItemsTillAndIncludingConditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 2, 3, 4 };
            var equeue = new EmitterEventQueue<int>(sequence);

            equeue.PeekUntil(i => i > 3, true).ToList().Should().BeEquivalentTo(expected);
            equeue.Should().BeEquivalentTo(sequence);
        }

        [Fact]
        public void ConsumeUntil_WithConsumeLastItem_ShouldRemoveItemsTillAndIncludingComditionIsMet()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 5 };
            var equeue = new EmitterEventQueue<int>(sequence);

            equeue.ConsumeUntil(i => i > 3, true);

            equeue.Should().BeEquivalentTo(expected);
        }
    }
}