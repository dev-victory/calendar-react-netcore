using EventService.Application.Constants;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Features.Events.Queries.GetEventById;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Tests.Features.Events.Commands.DeleteEvent
{
    public class DeleteEventCommandHandlerTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<ILogger<DeleteEventCommandHandler>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockRedisCache;
        private readonly Mock<IOptions<RedisSettings>> _mockRedisSettings;

        public DeleteEventCommandHandlerTests()
        {
            _mockEventRepository = new();
            _mockRedisCache = new();
            _mockLogger = new();
            _mockRedisSettings = new();
        }

        [Fact]
        public async Task Handle_DeleteEvent_Should_ReturnForbiddenAccessError_WhenUserIsUnauthorisedToDelete()
        {
            //Arrange
            var eventId = Guid.NewGuid();
            _mockRedisSettings
                .SetupGet(x => x.Value)
                .Returns(new RedisSettings
                {
                    CacheExpiryInMinutes = 60
                });

            var request = new DeleteEventCommand
            {
                EventId = eventId,
                UserId = "any"
            };

            var calEvent = new Event 
            {
                EventId = eventId,
                CreatedBy = "new"
            };

            _mockEventRepository
                .Setup(x => x.GetEvent(It.IsAny<Guid>()))
                .ReturnsAsync(calEvent);

            var handler = new DeleteEventCommandHandler(
                _mockEventRepository.Object,
                _mockLogger.Object,
                _mockRedisCache.Object,
                _mockRedisSettings.Object);

            //Act
            var result = await Record.ExceptionAsync(
                async () => await handler.Handle(request, default));

            //Assert
            Assert.NotNull(result);
            Assert.IsType<ForbiddenAccessException>(result);
        }
    }
}
