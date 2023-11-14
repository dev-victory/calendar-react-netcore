using AutoMapper;
using EventBus.Message.Messages;
using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Services;
using EventService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EventService.Application.Tests.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandlerTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageProducerService<NewCalendarEventMessage>> _mockMessageProducerService;
        private readonly Mock<ILogger<CreateEventCommandHandler>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockRedisCache;
        private readonly Mock<IOptions<RedisSettings>> _mockRedisSettings;

        public CreateEventCommandHandlerTests()
        {
            _mockEventRepository = new();
            _mockMapper = new();
            _mockRedisCache = new();
            _mockLogger = new();
            _mockRedisSettings = new();
            _mockMessageProducerService = new();
        }

        [Fact]
        public async Task Handle_CreateEvent_Should_SendNotificationsForEachInvitee_WhenEventIsCreated()
        {
            //Arrange
            var request = new CreateEventCommand
            {
                Name = "test",
            };

            var inviteesCount = 3;
            var invitees = new List<EventInvitation>();

            for (int i = 0; i < inviteesCount; i++)
            {
                invitees.Add(new EventInvitation { });
            }

            var calEvent = new Event
            {
                EventId = Guid.Empty,
                Timezone = "Asia/Calcutta",
                Invitees = invitees
            };

            _mockRedisSettings.SetupGet(x => x.Value).Returns(new RedisSettings
            {
                CacheExpiryInMinutes = 60
            });

            _mockMessageProducerService.Setup(m => m.SendNewEventMessage(
                It.IsAny<NewCalendarEventMessage>(), It.IsAny<string>())).Verifiable();

            _mockMapper.Setup(
                        x => x.Map<Event>(It.IsAny<CreateEventCommand>()))
                        .Returns(calEvent);
            _mockEventRepository.Setup(x => x.AddAsync(calEvent)).ReturnsAsync(calEvent);

            //Act
            var handler = new CreateEventCommandHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockMessageProducerService.Object,
                _mockLogger.Object,
                _mockRedisCache.Object,
                _mockRedisSettings.Object);

            var result = await handler.Handle(request, default);


            //Assert
            _mockMessageProducerService.Verify(x => x.SendNewEventMessage(
                It.IsAny<NewCalendarEventMessage>(), It.IsAny<string>()), Times.AtLeast(inviteesCount));
        }
    }
}
