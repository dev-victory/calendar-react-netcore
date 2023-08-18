namespace EventService.Application.Models
{
    public class RedisSettings
    {
        public int CacheExpiryInMinutes { get; set; }
        public string ConnectionString { get; set; }
        public int ConnectionTimeoutInMilliseconds { get; set; }
        public int MaxRetryCount { get; set; }
    }
}
