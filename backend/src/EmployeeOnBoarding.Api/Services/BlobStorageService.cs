
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EmployeeOnBoarding.Api.Models;
using Microsoft.Extensions.Options;

namespace EmployeeOnBoarding.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadProfilePictureAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteProfilePictureAsync(
        string? blobUrl,
        CancellationToken cancellationToken);
}

public sealed class BlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<StorageOptions> options,
    ILogger<BlobStorageService> logger) : IBlobStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> UploadProfilePictureAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var container = blobServiceClient.GetBlobContainerClient(
            _options.ContainerName);

        // Create the container if it does not already exist.
        await container.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var blobName =
            $"employees/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);

        logger.LogInformation(
            "Uploaded profile picture to blob {BlobName}",
            blobName);

        return blob.Uri.ToString();
    }

    public async Task DeleteProfilePictureAsync(
        string? blobUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobUrl) ||
            !Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        var blobUri = new BlobUriBuilder(uri);

        if (!string.Equals(
            blobUri.BlobContainerName,
            _options.ContainerName,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var container = blobServiceClient.GetBlobContainerClient(
            _options.ContainerName);

        var blob = container.GetBlobClient(blobUri.BlobName);

        try
        {
            await blob.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Deleted profile picture blob {BlobName}",
                blobUri.BlobName);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not delete blob {BlobName}",
                blobUri.BlobName);
        }
    }
}
