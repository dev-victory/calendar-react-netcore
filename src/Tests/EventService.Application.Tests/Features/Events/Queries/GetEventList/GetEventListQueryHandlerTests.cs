using AutoMapper;
using EventService.Application.Features.Events.Queries.GetEventList;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace EventService.Application.Tests.Features.Events.Queries.GetEventList
{
    public class GetEventListQueryHandlerTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDistributedCache> _mockRedisCache;
        private readonly Mock<ILogger<GetEventListQueryHandler>> _mockLogger;
        private readonly Mock<IOptions<RedisSettings>> _mockRedisSettings;

        public GetEventListQueryHandlerTests()
        {
            _mockEventRepository = new();
            _mockMapper = new();
            _mockRedisCache = new();
            _mockLogger = new();
            _mockRedisSettings = new();
        }

        [Fact]
        public async void Handle_Should_ReturnResultsFromDb_WhenRedisCacheReturnsNoData()
        {
            //Arrange
            var request = new GetEventListQuery("any", true);
            _mockRedisSettings.SetupGet(x => x.Value).Returns(new RedisSettings
            {
                CacheExpiryInMinutes = 60
            });

            var handler = new GetEventListQueryHandler(
                _mockEventRepository.Object,
                _mockMapper.Object,
                _mockRedisCache.Object,
                _mockLogger.Object,
                _mockRedisSettings.Object
                );

            _mockEventRepository.Setup(m => m.GetEvents(
                It.IsAny<string>(), true)).ReturnsAsync(new List<Event>()).Verifiable();

            _mockRedisCache.Setup(x => x.GetAsync(
                It.IsAny<string>(), default)).Returns(Task.FromResult(Encoding.ASCII.GetBytes("[]")));
            
            _mockMapper.Setup(
                        x => x.Map<List<EventVm>>(It.IsAny<IEnumerable<Event>>()))
                        .Returns(new List<EventVm> { new EventVm { EventId = Guid.Empty, Timezone = "Asia/Calcutta" } });

            //Act
            var result = await handler.Handle(request, default);

            //Assert
            _mockEventRepository.Verify(m=> m.GetEvents(It.IsAny<string>(), true), Times.Once);
        }
    }
}
