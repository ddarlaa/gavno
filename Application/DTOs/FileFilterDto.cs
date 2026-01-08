namespace IceBreakerApp.Application.DTOs
{
    public class FileFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? ContentType { get; set; } // "image", "document", "other"
        public string SortBy { get; set; } = "UploadedAt"; // "UploadedAt", "OriginalFileName", "Size"
        public bool SortOrder { get; set; } // "asc", "desc"
        public string? Search { get; set; } // Поиск по OriginalFileName
    }
}
