using System;

namespace IceBreakerApp.Domain.Models
{
    public class TopicAttachment
    {
        public Guid TopicId { get; set; }
        public Guid FileId { get; set; }
        public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
        public int Order { get; set; } // Для порядка отображения

        // Navigation properties
        public virtual Topic Topic { get; set; } = null!;
        public virtual FileMetadata File { get; set; } = null!;
    }
}
