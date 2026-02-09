namespace Thmanyah.Shared.Domain
{
    /// <summary>
    /// Enumeration of supported episode storage strategies.
    /// </summary>
    public enum EpisodeStorageType
    {
        LocalFile = 0,
        AzureStorage = 1,
        AWSStorage = 2,
        ExternalUrl = 3
    }
}
