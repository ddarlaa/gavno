using Microsoft.AspNetCore.Http;
using System;

namespace IceBreakerApp.Application.DTOs
{
    public class ChunkUploadRequest
    {
        public IFormFile Chunk { get; set; } = null!;
        public Guid UploadId { get; set; } 
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;

        // !!! Добавьте эти поля !!!
        public bool IsPublic { get; set; } = false;
        public DateTime? ExpiresAt { get; set; }
    }

}
