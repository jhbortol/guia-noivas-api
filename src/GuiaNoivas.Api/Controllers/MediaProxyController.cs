using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaProxyController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly AppDbContext _db;

    public MediaProxyController(BlobServiceClient blobServiceClient, IConfiguration config, AppDbContext db)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = config["Storage:Container"] ?? "media";
        _db = db;
    }

    [HttpPost("upload/proxy")]
    [Authorize]
    [RequestSizeLimit(134217728)] // 128 MB by default
    public async Task<IActionResult> UploadProxy(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        const long maxBytes = 134217728; // 128 MB
        if (file.Length > maxBytes)
            return BadRequest(new { error = "File too large." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "Unsupported content type." });

        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var blobClient = container.GetBlobClient(blobName);

        try
        {
            await using var stream = file.OpenReadStream();
            var headers = new BlobHttpHeaders { ContentType = file.ContentType };
            await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);

            var media = new Media
            {
                Id = Guid.NewGuid(),
                FornecedorId = null,
                Url = blobClient.Uri.ToString(),
                Filename = file.FileName,
                ContentType = file.ContentType,
                Width = null,
                Height = null,
                IsPrimary = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Media.Add(media);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { blobName, url = blobClient.Uri.ToString(), mediaId = media.Id });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499); // client closed request
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Upload failed.", details = ex.Message });
        }
    }
}
