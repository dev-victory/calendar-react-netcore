using AutoMapper;
using EventService.Application.Features.Events.Queries.GetEventById;
using EventService.Application.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using EventService.Application.Exceptions;
using EventService.Application.Models;
using EventService.Domain.Entities;
using EventService.Application.Constants;

namespace EventService.Application.Tests.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandlerTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<GetEventByIdQueryHandler>> _mockLogger;

        public GetEventByIdQueryHandlerTests()
        {
            _mockEventRepository = new();
            _mockMapper = new();
            _mockLogger = new();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenEventIsDeleted()
        {
            //Arrange
            var eventId = Guid.NewGuid();

            _mockEventRepository.Setup(
                x => x.GetEvent(It.IsAny<Guid>()))
                .ReturnsAsync(new Event
                {
                    IsDeleted = true
                });

            var handler = new GetEventByIdQueryHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            //Act
            var result = await Record.ExceptionAsync(
                async () => await handler.Handle(new GetEventByIdQuery(eventId, string.Empty), default));

            //Assert
            Assert.NotNull(result);
            Assert.IsType<NotFoundException>(result);
            Assert.Equal(string.Format(DomainErrors.EventNotFound, eventId), result.Message);
        }

        [Fact]
        public async void Handle_Should_ReturnFailure_WhenUserDoesNothaveAccess()
        {
            //Arrange
            var eventId = Guid.NewGuid();
            var createdBy = "invalid";

            _mockEventRepository.Setup(
                x => x.GetEvent(It.IsAny<Guid>()))
                .ReturnsAsync(new Event
                {
                    IsDeleted = false,
                    CreatedBy = "auth0"
                });

            var handler = new GetEventByIdQueryHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            //Act
            var result = await Record.ExceptionAsync(async () => await handler.Handle(new GetEventByIdQuery(eventId, string.Empty), default));

            //Assert
            Assert.NotNull(result);
            Assert.IsType<ForbiddenAccessException>(result);
        }

        [Fact]
        public async void Handle_Should_ReturnEvent_WhenEventIsNotDeleted()
        {
            //Arrange
            var eventId = Guid.NewGuid();
            var calEvent = new Event
            {
                IsDeleted = false,
                EventId = eventId
            };

            _mockEventRepository.Setup(
                        x => x.GetEvent(It.IsAny<Guid>()))
                        .ReturnsAsync(calEvent);

            _mockMapper.Setup(
                        x => x.Map<EventVm>(It.IsAny<Event>()))
                        .Returns(new EventVm { EventId = eventId });

            var handler = new GetEventByIdQueryHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            //Act
            var result = await handler.Handle(new GetEventByIdQuery(eventId, null), default);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result.EventId, eventId);
        }
    }
}