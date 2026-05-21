using AutoMapper;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class QuestionLikeService : IQuestionLikeService
    {
        private readonly IQuestionLikeRepository _likeRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionLikeService> _logger;

        public QuestionLikeService(
            IQuestionLikeRepository likeRepository,
            IQuestionRepository questionRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<QuestionLikeService> logger)
        {
            _likeRepository = likeRepository;
            _questionRepository = questionRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> AddLikeAsync(Guid questionId, Guid userId, CancellationToken ct = default)
        {
            try
            {
                // Проверяем существование вопроса и пользователя
                var question = await _questionRepository.GetByIdAsync(questionId, ct);
                if (question == null)
                {
                    _logger.LogWarning("Question not found: {QuestionId}", questionId);
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId, ct);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                // Проверяем, не лайкнул ли уже пользователь этот вопрос
                var existingLike = await _likeRepository.GetByQuestionAndUserAsync(questionId, userId, ct);
                if (existingLike != null)
                {
                    _logger.LogInformation("User {UserId} already liked question {QuestionId}", userId, questionId);
                    return false;
                }

                // Создаем новый лайк
                var like = new QuestionLike
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _likeRepository.AddAsync(like, ct);

                // Обновляем счетчик лайков у вопроса
                var currentCount = await _likeRepository.GetCountByQuestionIdAsync(questionId, ct);
                question.LikeCount = currentCount;
                await _questionRepository.UpdateAsync(question, ct);

                _logger.LogInformation("User {UserId} liked question {QuestionId}", userId, questionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding like for question: {QuestionId} by user: {UserId}", questionId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveLikeAsync(Guid questionId, Guid userId, CancellationToken ct = default)
        {
            try
            {
                // Проверяем существование лайка
                var existingLike = await _likeRepository.GetByQuestionAndUserAsync(questionId, userId, ct);
                if (existingLike == null)
                {
                    _logger.LogInformation("No like found to remove for question: {QuestionId} by user: {UserId}", questionId, userId);
                    return false;
                }

                // Удаляем лайк
                await _likeRepository.DeleteByQuestionAndUserAsync(questionId, userId, ct);

                // Обновляем счетчик лайков у вопроса
                var question = await _questionRepository.GetByIdAsync(questionId, ct);
                if (question != null)
                {
                    var currentCount = await _likeRepository.GetCountByQuestionIdAsync(questionId, ct);
                    question.LikeCount = Math.Max(0, currentCount);
                    await _questionRepository.UpdateAsync(question, ct);
                }

                _logger.LogInformation("User {UserId} removed like from question {QuestionId}", userId, questionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing like for question: {QuestionId} by user: {UserId}", questionId, userId);
                return false;
            }
        }

        public async Task<int> GetLikeCountAsync(Guid questionId, CancellationToken ct = default)
        {
            try
            {
                return await _likeRepository.GetCountByQuestionIdAsync(questionId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting like count for question: {QuestionId}", questionId);
                return 0;
            }
        }

        public async Task<int> GetUserLikeCountAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                return await _likeRepository.GetCountByUserIdAsync(userId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting like count for user: {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> HasUserLikedAsync(Guid questionId, Guid userId, CancellationToken ct = default)
        {
            try
            {
                return await _likeRepository.ExistsAsync(questionId, userId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} liked question: {QuestionId}", userId, questionId);
                return false;
            }
        }

        public async Task<IEnumerable<Guid>> GetUserLikedQuestionIdsAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var likes = await _likeRepository.GetByUserIdAsync(userId, ct);
                return likes.Select(like => like.QuestionId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting liked question IDs for user: {UserId}", userId);
                return new List<Guid>();
            }
        }
    }
}