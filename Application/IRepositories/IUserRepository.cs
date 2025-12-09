using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Application.IRepositories;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<User>> GetPageAsync(int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken ct);
}