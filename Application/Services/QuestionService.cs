using AutoMapper;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using IceBreakerApp.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IQuestionLikeService _questionLikeService;
        private readonly IQuestionAnswerService _questionAnswerService;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionService> _logger;

        public QuestionService(
            IQuestionRepository questionRepository,
            IUserRepository userRepository,
            ITopicRepository topicRepository,
            IQuestionLikeService questionLikeService,
            IQuestionAnswerService questionAnswerService,
            IMapper mapper,
            ILogger<QuestionService> logger)
        {
            _questionRepository = questionRepository;
            _userRepository = userRepository;
            _topicRepository = topicRepository;
            _questionLikeService = questionLikeService;
            _questionAnswerService = questionAnswerService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<QuestionResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(id, ct);
                if (question == null)
                    return null;

                // Получаем связанные данные
                var user = await _userRepository.GetByIdAsync(question.UserId, ct);
                var topic = await _topicRepository.GetByIdAsync(question.TopicId, ct);
                var likeCount = await _questionLikeService.GetLikeCountAsync(id, ct);
                var answerCount = await _questionAnswerService.GetAnswerCountAsync(id, ct);

                // Увеличиваем счетчик просмотров
                question.ViewCount++;
                await _questionRepository.UpdateAsync(question, ct);

                var response = _mapper.Map<QuestionResponseDTO>(question);
                response.Username = user?.Username ?? "Unknown";
                response.UserDisplayName = user?.DisplayName ?? $"{user?.FirstName} {user?.LastName}".Trim();
                response.TopicName = topic?.Name ?? "Unknown";
                response.LikeCount = likeCount;
                response.AnswerCount = answerCount;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question by ID: {QuestionId}", id);
                throw;
            }
        }

        public async Task<PaginatedResult<QuestionResponseDTO>> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? sortBy = null,
            string? sortOrder = null,
            string? search = null,
            Guid? topicId = null,
            CancellationToken ct = default)
        {
            try
            {
                var paginatedResult = await _questionRepository.GetPaginatedAsync(
                    pageNumber, pageSize, sortBy, sortOrder, search, topicId, ct);

                var responseList = new List<QuestionResponseDTO>();
                
                foreach (var question in paginatedResult.Items)
                {
                    // Получаем связанные данные для каждого вопроса
                    var user = await _userRepository.GetByIdAsync(question.UserId, ct);
                    var topic = await _topicRepository.GetByIdAsync(question.TopicId, ct);
                    var likeCount = await _questionLikeService.GetLikeCountAsync(question.Id, ct);
                    var answerCount = await _questionAnswerService.GetAnswerCountAsync(question.Id, ct);

                    var response = _mapper.Map<QuestionResponseDTO>(question);
                    response.Username = user?.Username ?? "Unknown";
                    response.UserDisplayName = user?.DisplayName ?? $"{user?.FirstName} {user?.LastName}".Trim();
                    response.TopicName = topic?.Name ?? "Unknown";
                    response.LikeCount = likeCount;
                    response.AnswerCount = answerCount;

                    responseList.Add(response);
                }

                return new PaginatedResult<QuestionResponseDTO>(
                    responseList, paginatedResult.TotalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all questions");
                throw;
            }
        }

        public async Task<QuestionResponseDTO> CreateAsync(CreateQuestionDTO dto, CancellationToken ct = default)
        {
            try
            {
                // Проверяем существование пользователя и топика
                var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
                if (user == null)
                    throw new NotFoundException($"User with ID {dto.UserId} not found");

                var topic = await _topicRepository.GetByIdAsync(dto.TopicId, ct);
                if (topic == null)
                    throw new NotFoundException($"Topic with ID {dto.TopicId} not found");

                // Создаем вопрос
                var question = _mapper.Map<Question>(dto);
                question.Id = Guid.NewGuid();
                question.CreatedAt = DateTime.UtcNow.ToPostgreSafeUtc();
                question.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();
                question.IsActive = true;
                question.ViewCount = 0;
                question.LikeCount = 0;
                question.AnswerCount = 0;

                await _questionRepository.AddAsync(question, ct);

                var response = _mapper.Map<QuestionResponseDTO>(question);
                response.Username = user.Username;
                response.UserDisplayName = user.DisplayName ?? $"{user.FirstName} {user.LastName}".Trim();
                response.TopicName = topic.Name;
                response.LikeCount = 0;
                response.AnswerCount = 0;

                _logger.LogInformation("Question created successfully: {QuestionId} by user: {UserId}", 
                    question.Id, dto.UserId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question");
                throw;
            }
        }

        public async Task UpdateAsync(Guid id, UpdateQuestionDTO dto, CancellationToken ct = default)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(id, ct);
                if (question == null)
                    throw new NotFoundException($"Question with ID {id} not found");

                // Проверяем топик если он указан
                if (dto.TopicId.HasValue)
                {
                    var topic = await _topicRepository.GetByIdAsync(dto.TopicId.Value, ct);
                    if (topic == null)
                        throw new NotFoundException($"Topic with ID {dto.TopicId.Value} not found");
                }

                // Обновляем поля
                if (!string.IsNullOrEmpty(dto.Title))
                    question.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Content))
                    question.Content = dto.Content;

                if (dto.TopicId.HasValue)
                    question.TopicId = dto.TopicId.Value;

                question.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();

                await _questionRepository.UpdateAsync(question, ct);

                _logger.LogInformation("Question updated successfully: {QuestionId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question: {QuestionId}", id);
                throw;
            }
        }

        public async Task PatchAsync(Guid id, JsonPatchDocument<UpdateQuestionDTO> patchDoc, CancellationToken ct = default)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(id, ct);
                if (question == null)
                    throw new NotFoundException($"Question with ID {id} not found");

                // Применяем патч к DTO
                var updateDto = _mapper.Map<UpdateQuestionDTO>(question);
                patchDoc.ApplyTo(updateDto);

                // Обновляем сущность
                if (!string.IsNullOrEmpty(updateDto.Title))
                    question.Title = updateDto.Title;

                if (!string.IsNullOrEmpty(updateDto.Content))
                    question.Content = updateDto.Content;

                if (updateDto.TopicId.HasValue)
                {
                    var topic = await _topicRepository.GetByIdAsync(updateDto.TopicId.Value, ct);
                    if (topic == null)
                        throw new NotFoundException($"Topic with ID {updateDto.TopicId.Value} not found");
                    question.TopicId = updateDto.TopicId.Value;
                }

                question.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();

                await _questionRepository.UpdateAsync(question, ct);

                _logger.LogInformation("Question patched successfully: {QuestionId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching question: {QuestionId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var question = await _questionRepository.GetByIdAsync(id, ct);
                if (question == null)
                    throw new NotFoundException($"Question with ID {id} not found");

                // Soft delete
                question.IsActive = false;
                question.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();

                await _questionRepository.UpdateAsync(question, ct);

                _logger.LogInformation("Question deleted successfully: {QuestionId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question: {QuestionId}", id);
                throw;
            }
        }

        public async Task<BulkOperationResult<QuestionResponseDTO>> BulkCreateAsync(
            List<CreateQuestionDTO> dtos, 
            CancellationToken ct = default)
        {
            var result = new BulkOperationResult<QuestionResponseDTO>();
            
            try
            {
                foreach (var dto in dtos)
                {
                    try
                    {
                        var createdQuestion = await CreateAsync(dto, ct);
                        result.SuccessItems.Add(createdQuestion);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new BulkOperationError
                        {
                            Index = dtos.IndexOf(dto),
                            Error = ex.Message
                        });
                        _logger.LogWarning(ex, "Error creating question in bulk operation");
                    }
                }

                _logger.LogInformation("Bulk create completed. Success: {SuccessCount}, Errors: {ErrorCount}", 
                    result.SuccessItems.Count, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk create operation");
                throw;
            }
        }
    }
}