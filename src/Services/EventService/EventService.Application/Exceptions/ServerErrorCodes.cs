namespace EventService.Application.Exceptions
{
    public enum ServerErrorCodes
    {
        Unknown = 0,
        DatabaseError = 100,
        RedisCacheError = 200,
        PermissionDenied = 300
    }
}
