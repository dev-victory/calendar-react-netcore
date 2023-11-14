using AutoMapper;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EventService.Application.Tests.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventCommandHandlerTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<ILogger<UpdateEventCommandHandler>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockRedisCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IOptions<RedisSettings>> _mockRedisSettings;

        public UpdateEventCommandHandlerTests()
        {
            _mockEventRepository = new();
            _mockRedisCache = new();
            _mockLogger = new();
            _mockRedisSettings = new();
            _mockMapper = new();
        }

        [Fact]
        public async Task Handle_UpdateEvent_Should_ReturnForbiddenAccessError_WhenUserIsUnauthorisedToUpdate()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockRedisSettings
                .SetupGet(x => x.Value)
                .Returns(new RedisSettings
                {
                    CacheExpiryInMinutes = 60
                });

            var request = new UpdateEventCommand
            {
                EventId = eventId,
                ModifiedBy = "any"
            };

            var calEvent = new Event
            {
                EventId = eventId,
                CreatedBy = "new"
            };

            _mockEventRepository
                .Setup(x => x.GetEvent(It.IsAny<Guid>()))
                .ReturnsAsync(calEvent);

            var handler = new UpdateEventCommandHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockRedisCache.Object,
                _mockRedisSettings.Object);

            // Act
            var result = await Record.ExceptionAsync(
                async () => await handler.Handle(request, default));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ForbiddenAccessException>(result);
        }

        [Fact]
        public async Task Handle_UpdateEvent_Should_ReturnExpectedNotificationsCount_WhenNotificationsAreModified()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var notificationDates = new List<DateTime>();
            var requestNotificationDate = DateTime.Now.AddDays(-1);
            var request = new UpdateEventCommand
            {
                EventId = eventId,
                ModifiedBy = "new"
            };

            for (int i = 0; i < 3; i++)
            {
                notificationDates.Add(DateTime.Now.AddDays(i));
            }

            var calEvent = new Event
            {
                EventId = eventId,
                Timezone = "Asia/Kolkata",
                CreatedBy = "new"
            };

            foreach (var date in notificationDates)
            {
                calEvent.Notifications.Add(new EventNotification
                {
                    NotificationDate = date
                });
            }

            _mockRedisSettings
                .SetupGet(x => x.Value)
                .Returns(new RedisSettings
                {
                    CacheExpiryInMinutes = 60
                });

            _mockEventRepository
                .Setup(x => x.GetEvent(It.IsAny<Guid>()))
                .ReturnsAsync(calEvent);

            _mockMapper.Setup(x => x
            .Map<Event>(It.IsAny<UpdateEventCommand>()))
                .Returns(new Event
                {
                    EventId = eventId,
                    CreatedBy = "new",
                    Timezone = "Asia/Kolkata",
                    Notifications = new List<EventNotification>
                    {
                        new EventNotification
                        {
                            NotificationDate = requestNotificationDate
                        }
                    }
                });

            var handler = new UpdateEventCommandHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockRedisCache.Object,
                _mockRedisSettings.Object);


            // Act
            var result = await handler.MapToUpdateEventModel(request);

            // Assert
            Assert.NotNull(result);

            // 1 new notification in request
            Assert.Equal(1, result.AddNotifications.Count);

            // 3 old notifications to be removed since request notifications doesn't contain them
            Assert.Equal(3, result.RemoveNotifications.Count);
        }
    }
}
