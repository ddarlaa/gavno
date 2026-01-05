using System;

namespace IceBreakerApp.Domain.Models
{
    public class QuestionAttachment
    {
        public Guid QuestionId { get; set; }
        public Guid FileId { get; set; }
        public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
        public int Order { get; set; } // Для порядка отображения

        // Navigation properties
        public virtual Question Question { get; set; } = null!;
        public virtual FileMetadata File { get; set; } = null!;
    }
}
