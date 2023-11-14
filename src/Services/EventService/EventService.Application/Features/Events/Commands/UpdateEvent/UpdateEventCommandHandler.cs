using AutoMapper;
using EventService.Application.Constants;
using EventService.Application.Exceptions;
using EventService.Application.Features.Events.Commands.UpdateEvent;
using EventService.Application.Models;
using EventService.Application.Persistence;
using EventService.Application.Utilities;
using EventService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EventService.Application.Features.Events.Commands.CreateEvent
{
	public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
	{
		private readonly IEventRepository _eventRepository;
		private readonly IMapper _mapper;
		private readonly ILogger<UpdateEventCommandHandler> _logger;
		private readonly IDistributedCache _redisCache;
		private readonly int _cacheExpiryInMinutes;

		public UpdateEventCommandHandler(
			IEventRepository eventRepository,
			IMapper mapper,
			ILogger<UpdateEventCommandHandler> logger,
			IDistributedCache redisCache,
			IOptions<RedisSettings> redisSettings)
		{
			_eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
			_mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
			_cacheExpiryInMinutes = redisSettings.Value.CacheExpiryInMinutes;
		}

		public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var model = await MapToUpdateEventModel(request);
				var updatedEvent = await _eventRepository.UpdateEvent(model);
				await UpdateCache(request, updatedEvent, cancellationToken);
			}
			catch (DatabaseException dbEx)
			{
				_logger.LogError(string.Format(DomainErrors.EventModifyDatabaseError, request.EventId, "updated", dbEx.Message));

				throw new InternalErrorException((int)ServerErrorCodes.DatabaseError, DomainErrors.SomethingWentWrong);
			}
			catch (RedisTimeoutException ex)
			{
				_logger.LogError(string.Format(DomainErrors.RedisCacheTimeout, ex.Message));
			}
			catch (RedisConnectionException ex)
			{
				_logger.LogError(string.Format(DomainErrors.RedisCacheConnectionError, ex.Message));
			}
			catch (ForbiddenAccessException forEx)
			{
				throw forEx;
			}
			catch (Exception ex)
			{
				_logger.LogError(string.Format(DomainErrors.EventModifyError, request.EventId, "updated", ex.Message));

				throw new InternalErrorException((int)ServerErrorCodes.Unknown, DomainErrors.SomethingWentWrong);
			}

			_logger.LogInformation($"Event {request.EventId} is successfully updated");
		}

		private async Task UpdateCache(UpdateEventCommand request, Event updatedEvent, CancellationToken cancellationToken)
		{
			var cache = await _redisCache.GetStringAsync(updatedEvent.CreatedBy, cancellationToken);
			if (cache != null)
			{
				var cachedEvents = JsonSerializer.Deserialize<List<Event>>(cache);
				var cachedEventItem = cachedEvents.FirstOrDefault(e => e.EventId == request.EventId);
				if (cachedEventItem != null)
				{
					cachedEvents.Remove(cachedEventItem);
					cachedEvents.Add(updatedEvent);
					await _redisCache.SetStringAsync(updatedEvent.CreatedBy,
						JsonSerializer.Serialize(cachedEvents),
						new DistributedCacheEntryOptions
						{
							AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_cacheExpiryInMinutes)
						});
				}
			}
		}

		internal async Task<UpdateEventModel> MapToUpdateEventModel(UpdateEventCommand request)
		{
			var dbEntity = await _eventRepository.GetEvent(request.EventId);
			if (request.ModifiedBy != dbEntity.CreatedBy)
			{
				_logger.LogWarning(string.Format(DomainErrors.EventUserForbiddenAccess, request.ModifiedBy, request.EventId));

				throw new ForbiddenAccessException();
			}

			var mappedEntity = _mapper.Map<Event>(request);

			mappedEntity.LastModifiedBy = request.ModifiedBy;


			// unique invitees and notifications only
			mappedEntity.Invitees = mappedEntity.Invitees.GroupBy(i => i.InviteeEmailId)
				.Select(g => g.First()).ToList();
			mappedEntity.Notifications = mappedEntity.Notifications.GroupBy(n => n.NotificationDate)
				.Select(g => g.First()).ToList();

			if (dbEntity != null)
			{
				if (dbEntity.StartDate != request.StartDate)
				{
					mappedEntity.StartDate = request.StartDate.ToUtcDate(request.Timezone);
				}
				if (dbEntity.EndDate != request.EndDate)
				{
					mappedEntity.EndDate = request.EndDate.ToUtcDate(request.Timezone);
				}
			}

			dbEntity.StartDate = mappedEntity.StartDate;
			dbEntity.EndDate = mappedEntity.EndDate;
			dbEntity.Location = mappedEntity.Location;
			dbEntity.Description = mappedEntity.Description;
			dbEntity.Timezone = mappedEntity.Timezone;
			dbEntity.Name = mappedEntity.Name;
			dbEntity.LastModifiedBy = mappedEntity.LastModifiedBy;

			var notificationsToAdd = mappedEntity.Notifications
				.ExceptBy(dbEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();
			var notificationsToRemove = dbEntity.Notifications
				.ExceptBy(mappedEntity.Notifications.Select(e => e.NotificationDate), e => e.NotificationDate).ToList();
			var inviteesToAdd = mappedEntity.Invitees
				.ExceptBy(dbEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();
			var inviteesToRemove = dbEntity.Invitees
				.ExceptBy(mappedEntity.Invitees.Select(e => e.InviteeEmailId), e => e.InviteeEmailId).ToList();

			foreach (var notification in notificationsToAdd)
			{
				var utcNotificationDate = notification.NotificationDate.ToUtcDate(mappedEntity.Timezone);
				notification.NotificationDate = utcNotificationDate;
				notification.EventId = dbEntity.Id;
				notification.CreatedBy = mappedEntity.LastModifiedBy;
				notification.CreatedDate = DateTime.UtcNow;
			}

			foreach (var item in inviteesToAdd)
			{
				item.EventId = dbEntity.Id;
				item.CreatedBy = mappedEntity.LastModifiedBy;
				item.CreatedDate = DateTime.UtcNow;
			}

			return new UpdateEventModel
			{
				Event = dbEntity,
				AddNotifications = notificationsToAdd,
				RemoveNotifications = notificationsToRemove,
				AddInvitees = inviteesToAdd,
				RemoveInvitees = inviteesToRemove
			};
		}
	}
}
