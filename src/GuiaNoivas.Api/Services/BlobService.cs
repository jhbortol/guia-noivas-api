using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace GuiaNoivas.Api.Services;

public interface IBlobService
{
    Task<(Uri Url, string BlobName)> GetUploadSasUriAsync(string blobName, TimeSpan expiry, string? contentType = null);
}

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;
    private readonly StorageSharedKeyCredential? _sharedKey;

    public BlobService(string connectionString, string containerName)
    {
        _client = new BlobServiceClient(connectionString);
        _containerName = string.IsNullOrWhiteSpace(containerName) ? "media" : containerName;

        // Try to extract AccountName and AccountKey from connection string
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(k => k[0].Trim(), v => v[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (parts.TryGetValue("AccountName", out var accountName) && parts.TryGetValue("AccountKey", out var accountKey))
        {
            _sharedKey = new StorageSharedKeyCredential(accountName, accountKey);
        }
    }

    public async Task<(Uri Url, string BlobName)> GetUploadSasUriAsync(string blobName, TimeSpan expiry, string? contentType = null)
    {
        var container = _client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlobClient(blobName);

        if (_sharedKey is null)
        {
            throw new InvalidOperationException("Cannot generate SAS: storage connection string does not contain AccountKey. Provide a connection string with AccountKey or configure a managed identity flow.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write | BlobSasPermissions.Read);

        var sas = sasBuilder.ToSasQueryParameters(_sharedKey).ToString();
        var sasUri = new UriBuilder(blob.Uri) { Query = sas }.Uri;

        return (sasUri, blobName);
    }
}
