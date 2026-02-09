namespace Thmanyah.Shared.Configurations;

public class StorageConfiguration
{
    public LocalStorageConfiguration Local { get; set; }
    public AzureStorageConfiguration AzureConfiguration { get; set; }
    public AwsStorageConfiguration AWSConfiguration { get; set; }
}

public class LocalStorageConfiguration
{
    public string Path { get; set; }
}

public class AzureStorageConfiguration
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
}

public class AwsStorageConfiguration
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Region { get; set; }
    public string BucketName { get; set; }
}

