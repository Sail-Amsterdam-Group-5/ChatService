using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Chat.Core.Interfaces;
using Chat.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly BlobStorageSettings _settings;

    public BlobStorageService(IOptions<BlobStorageSettings> settings)
    {
        _settings = settings.Value;
        var blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
    }

    public async Task<(string blobUrl, long size, string contentType)> UploadImageAsync(
        Stream imageStream,
        string contentType,
        string fileName)
    {
        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(imageStream, headers);

        var properties = await blobClient.GetPropertiesAsync();

        return (blobClient.Uri.ToString(), properties.Value.ContentLength, contentType);
    }

    public async Task DeleteImageAsync(string blobUrl)
    {
        if (Uri.TryCreate(blobUrl, UriKind.Absolute, out Uri? uri))
        {
            string blobName = Path.GetFileName(uri.LocalPath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }

    public bool IsValidImage(string contentType, long size)
    {
        return _settings.AllowedContentTypes.Contains(contentType.ToLower()) &&
               size <= _settings.MaxSizeBytes;
    }
}