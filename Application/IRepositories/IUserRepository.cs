using IceBreakerApp.Domain.Models;

namespace IceBreakerApp.Application.IRepositories
{
    public interface IUserRepository
    {
        // Get operations
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Find operations
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
        
        // Existence checks
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        
        // CRUD operations
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Pagination and search
        Task<IReadOnlyList<User>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<User>> GetPageAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default);
        
        // Batch operations
        Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
        
        // Count operations
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetCountBySearchAsync(string? search, CancellationToken cancellationToken = default);
        
        // Authentication
        Task<User?> AuthenticateAsync(string usernameOrEmail, string passwordHash, CancellationToken cancellationToken = default);
        
        // Utility
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        // Unique checks
        Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
        
        // Session management
        Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default);
        Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
        
        // User claims
        Task<IReadOnlyList<UserClaim>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserSession?> GetActiveSessionByHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
        Task<bool> RevokeSessionByHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    }
}