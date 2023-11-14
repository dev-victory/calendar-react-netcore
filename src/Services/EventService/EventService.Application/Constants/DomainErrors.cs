namespace EventService.Application.Constants
{
    internal static class DomainErrors
    {
        internal static string SomethingWentWrong = "Something went wrong";
        internal static string EventNotFound = "Event with Id {0} was not found";
        internal static string EventUserForbiddenAccess = "Forbidden: User {0} doesn't have access to event ID: {1}";
        internal static string RedisCacheTimeout = "Connection to redis cache timed out, details: \n{0}";
        internal static string RedisCacheConnectionError = "Error connecting to redis cache, details: \n{0}";
        internal static string EventFetchError = "Events for user {0} could not be fetched, details: \n{1}";
        internal static string EventModifyError = "Event {0} could not be {1}, details: \n{2}";
        internal static string EventModifyDatabaseError = "Event {0} could not be {1} in the database, details: \n{2}";
    }
}
