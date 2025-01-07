namespace Chat.Core.Interfaces;

public interface IBlobStorageService
{
    Task<(string blobUrl, long size, string contentType)> UploadImageAsync(
        Stream imageStream,
        string contentType,
        string fileName);

    Task DeleteImageAsync(string blobUrl);

    bool IsValidImage(string contentType, long size);
}