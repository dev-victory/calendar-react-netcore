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
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(1),
                    Timezone = "",
                    Location = "",
                    CreatedBy = "abc123456",
                    Invitees = new List<EventInvitation>
                    {
                        new EventInvitation
                        {
                            InviteeEmailId = "test@g.com",
                            CreatedBy = "abc123456"
                        }
                    },
                    Notifications = new List<EventNotification>
                    {
                        new EventNotification
                        {
                            NotificationDate = DateTime.Now.AddDays(15),
                            CreatedBy = "abc123456"
                        }
                    }
                }
            };
        }
    }
}
