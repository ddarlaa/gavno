using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.IO;

public static class MultipartRequestHelper
{
    // Content-Type: multipart/form-data; boundary="----WebKitFormBoundary..."
    // Используем MediaTypeHeaderValue из Microsoft.Net.Http.Headers
    public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        // contentType.Boundary может быть StringSegment, HeaderUtilities.RemoveQuotes тоже возвращает StringSegment
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }

    public static bool IsMultipartContentType(string? contentType)
    {
        return !string.IsNullOrEmpty(contentType) &&
               contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ContentDispositionHeaderValue берется из Microsoft.Net.Http.Headers
    public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && (!StringSegment.IsNullOrEmpty(contentDisposition.FileName)
                   || !StringSegment.IsNullOrEmpty(contentDisposition.FileNameStar));
    }

    public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
    {
        // Content-Disposition: form-data; name="description"
        // (без filename)
        return contentDisposition != null
               && contentDisposition.DispositionType.Equals("form-data")
               && StringSegment.IsNullOrEmpty(contentDisposition.FileName)
               && StringSegment.IsNullOrEmpty(contentDisposition.FileNameStar);
    }
}