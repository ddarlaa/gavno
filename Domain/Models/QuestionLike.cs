// Domain/QuestionLike.cs

using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Domain;

public class QuestionLike: BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Question? Question { get; private set; }
    public User? User { get; private set; }
}