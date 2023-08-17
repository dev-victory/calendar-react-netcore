using EventService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EventService.Infrastructure.Persistence
{
    public class EventContextSeed
    {
        public static async Task SeedAsync(EventContext eventContext, ILogger<EventContextSeed> logger)
        {
            if (!eventContext.Events.Any())
            {
                eventContext.Events.AddRange(GetPreconfiguredEvents());
                await eventContext.SaveChangesAsync();
                logger.LogInformation("Seed database associated with context {DbContextName}", typeof(EventContext).Name);
            }
        }

        private static IEnumerable<Event> GetPreconfiguredEvents()
        {
            return new List<Event>
            {
                new Event()
                {
                    Name = "test event",
                    Description = "test event description",
                    Status = EventStatus.Scheduled,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(1),
                    Timezone = "",
                    Location = "",
                    CreatedBy = "auth0|64b00b6a1d395800db1666ac",
                    Invitees = new List<EventInvitation>
                    {
                        new EventInvitation
                        {
                            InviteeEmailId = "test@g.com",
                            CreatedBy = "auth0|64b00b6a1d395800db1666ac"
                        }
                    },
                    Notifications = new List<EventNotification>
                    {
                        new EventNotification
                        {
                            NotificationDate = DateTime.UtcNow.AddDays(15),
                            CreatedBy = "auth0|64b00b6a1d395800db1666ac"
                        }
                    }
                }
            };
        }
    }
}
