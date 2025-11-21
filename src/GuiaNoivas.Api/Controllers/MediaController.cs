using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _config;

    public MediaController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("presign")]
    [Authorize]
    public IActionResult Presign([FromBody] PresignDto dto)
    {
        // Minimal implementation: return a fake presign when Azure is not configured
        var storageConnection = _config["Azure:BlobConnectionString"]; // optional
        if (string.IsNullOrEmpty(storageConnection))
        {
            // fallback: return a local upload URL (client should POST multipart to /uploads)
            var fileName = dto.Filename ?? Guid.NewGuid().ToString();
            var publicUrl = $"/uploads/{fileName}";
            return Ok(new { uploadUrl = publicUrl, publicUrl, blobName = fileName });
        }

        // If Azure configured, real implementation would generate SAS URL here.
        return Ok(new { uploadUrl = "", publicUrl = "", blobName = "" });
    }
}

public record PresignDto(string Filename, string ContentType, Guid? FornecedorId);
