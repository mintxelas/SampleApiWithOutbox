using Example.Domain;
using Example.Infrastructure.SqLite;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using Xunit;

namespace Example.Infrastructure.Tests
{
    public class BusSubscriptionsWithOutboxShould
    {
        private readonly OutboxSqLiteRepository outboxRepository;
        private readonly RepeatingTimer timer;
        private readonly BusSubscriptionsWithOutbox busSubscriptions;

        public BusSubscriptionsWithOutboxShould()
        {
            var logger = Substitute.For<ILogger<BusSubscriptionsWithOutbox>>();
            outboxRepository = Substitute.For<OutboxSqLiteRepository>(new object[]{null});
            timer = new RepeatingTimer(-1, -1);
            busSubscriptions = new BusSubscriptionsWithOutbox(logger, outboxRepository, timer);
        }

        [Fact]
        public void contain_references_to_subscribers_in_bus_reader()
        {
            GivenDomainEventInOutbox();
            var invokedTimes = 0;
            void Handler(MockEvent evt) => invokedTimes += 1;

            busSubscriptions.Subscribe((Action<MockEvent>)Handler);
            timer.OnTick();

            Assert.Equal(1, invokedTimes);
        }

        [Fact]
        public void invoke_subscribers_from_bus_reader_when_events_are_published_by_bus_writer()
        {
            GivenTwoDomainEventsInOutbox();
            var invokedTimes = 0;
            void Handler(MockEvent evt) => invokedTimes += 1;

            busSubscriptions.Subscribe((Action<MockEvent>)Handler);
            timer.OnTick();

            Assert.Equal(2, invokedTimes);
        }

        private void GivenTwoDomainEventsInOutbox()
        {
            var domainEvent = new MockEvent();
            outboxRepository.PendingEvents().Returns(new[] {domainEvent, domainEvent});
        }

        private void GivenDomainEventInOutbox()
        {
            var domainEvent = new MockEvent();
            outboxRepository.PendingEvents().Returns(new[] {domainEvent});
        }

        private class MockEvent:DomainEvent { }
    }
}
