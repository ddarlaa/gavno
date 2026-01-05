using Microsoft.AspNetCore.Http;

namespace IceBreakerApp.Application.DTOs.Create;

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public bool IsPublic { get; set; } = false;
    public DateTime? ExpiresAt { get; set; } = null;
}
