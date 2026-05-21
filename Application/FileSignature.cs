using Microsoft.AspNetCore.Http;

namespace IceBreakerApp.Application;

public static class FileSignature
{
    private static readonly Dictionary<string, byte[]> Signatures = new()
    {
        // Изображения
        { "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        { "image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } }, // GIF8
        { "image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 } }, // RIFF....WEBP

        // Документы
        { "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } }, // %PDF-
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // .docx
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // .xlsx
    };

    private static readonly Dictionary<string, string> Extensions = new()
    {
        { "image/jpeg", ".jpg,.jpeg" },
        { "image/png", ".png" },
        { "image/gif", ".gif" },
        { "image/webp", ".webp" },
        { "application/pdf", ".pdf" },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".doc,.docx" },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xls,.xlsx" },
    };

    public static (bool IsValid, string DetectedContentType) Validate(IFormFile file)
    {
        if (file.Length == 0) return (false, null!);

        using var stream = file.OpenReadStream();
        var buffer = new byte[12];
        var read = stream.Read(buffer, 0, buffer.Length);
        stream.Position = 0;

        foreach (var (contentType, signature) in Signatures)
        {
            if (read < signature.Length) continue;

            var match = true;
            for (int i = 0; i < signature.Length; i++)
            {
                if (buffer[i] != signature[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return (true, contentType);
            }
        }

        return (false, null!);
    }

    public static bool IsAllowedExtension(string fileName, string contentType)
    {
        if (!Extensions.TryGetValue(contentType, out var allowed))
            return false;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return allowed.Split(',').Contains(ext);
    }
}