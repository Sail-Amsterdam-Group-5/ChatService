namespace Chat.Infrastructure.Configuration;

public class BlobStorageSettings
{
    public string ConnectionString { get; set; } = null!;
    public string ContainerName { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public string[] AllowedContentTypes { get; set; } = { "image/jpeg", "image/png", "image/gif" };
}