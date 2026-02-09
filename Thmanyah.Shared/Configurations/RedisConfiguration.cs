namespace Thmanyah.Shared.Configurations;

public class RedisConfiguration
{
    public string Connection { get; set; }
}

public class RateLimitingConfiguration
{
    public int TokenLimit { get; set; }
    public int TokensPerPeriod { get; set; }
    public int ReplenishmentPeriodSeconds { get; set; }
    public int QueueLimit { get; set; }
}

public class DiscoveryCacheConfiguration
{
    public int ProgramsTtlSeconds { get; set; }
    public int EpisodesTtlSeconds { get; set; }
}
