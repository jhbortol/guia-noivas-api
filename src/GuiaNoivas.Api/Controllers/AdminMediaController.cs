using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using GuiaNoivas.Api.Data;
using GuiaNoivas.Api.Models;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/admin/media")]
[Authorize(Roles = "Admin")]
public class AdminMediaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;

    public AdminMediaController(AppDbContext db, BlobServiceClient? blobServiceClient = null, IConfiguration? config = null)
    {
        _db = db;
        _blobServiceClient = blobServiceClient;
        _containerName = config?["Storage:Container"] ?? "media";
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Media.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m => (m.Filename != null && m.Filename.Contains(term)) || (m.Url != null && m.Url.Contains(term)));
        }

        var total = await Task.FromResult(query.Count());
        var items = query.OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var media = await _db.Media.FindAsync(id);
        if (media == null) return NotFound();

        // Attempt to delete blob if BlobServiceClient is configured and Url present
        if (_blobServiceClient != null && !string.IsNullOrWhiteSpace(media.Url))
        {
            try
            {
                var container = _blobServiceClient.GetBlobContainerClient(_containerName);
                // derive blob name from url (last segment)
                var uri = new Uri(media.Url);
                var blobName = uri.Segments.Length > 0 ? string.Join(string.Empty, uri.Segments.Skip(1)) : uri.AbsolutePath.TrimStart('/');
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                // ignore blob delete errors but log in real app
            }
        }

        _db.Media.Remove(media);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
