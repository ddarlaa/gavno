using AutoMapper;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application.Services
{
    public class QuestionAnswerService : IQuestionAnswerService
    {
        private readonly IQuestionAnswerRepository _answerRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionAnswerService> _logger;

        public QuestionAnswerService(
            IQuestionAnswerRepository answerRepository,
            IQuestionRepository questionRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<QuestionAnswerService> logger)
        {
            _answerRepository = answerRepository;
            _questionRepository = questionRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<QuestionAnswerResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var answer = await _answerRepository.GetByIdAsync(id, ct);
                if (answer == null)
                    return null;

                var user = await _userRepository.GetByIdAsync(answer.UserId, ct);

                var response = _mapper.Map<QuestionAnswerResponseDTO>(answer);
                response.Username = user?.Username ?? "Unknown";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answer by ID: {AnswerId}", id);
                throw;
            }
        }

        public async Task<PaginatedResult<QuestionAnswerResponseDTO>> GetAllAsync(
            int pageNumber,
            int pageSize,
            Guid? questionId = null,
            Guid? userId = null,
            CancellationToken ct = default)
        {
            try
            {
                var paginatedResult = await _answerRepository.GetPaginatedAsync(
                    pageNumber, pageSize, questionId, userId, ct);

                var responseList = new List<QuestionAnswerResponseDTO>();

                foreach (var answer in paginatedResult.Items)
                {
                    var user = await _userRepository.GetByIdAsync(answer.UserId, ct);

                    var response = _mapper.Map<QuestionAnswerResponseDTO>(answer);
                    response.Username = user?.Username ?? "Unknown";

                    responseList.Add(response);
                }

                return new PaginatedResult<QuestionAnswerResponseDTO>(
                    responseList, paginatedResult.TotalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all answers");
                throw;
            }
        }

        public async Task<QuestionAnswerResponseDTO> CreateAsync(CreateQuestionAnswerDTO dto, CancellationToken ct = default)
        {
            try
            {
                // Проверяем существование вопроса и пользователя
                var question = await _questionRepository.GetByIdAsync(dto.QuestionId, ct);
                if (question == null)
                    throw new NotFoundException($"Question with ID {dto.QuestionId} not found");

                var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
                if (user == null)
                    throw new NotFoundException($"User with ID {dto.UserId} not found");

                // Создаем ответ
                var answer = _mapper.Map<QuestionAnswer>(dto);
                answer.Id = Guid.NewGuid();
                answer.CreatedAt = DateTime.UtcNow;
                answer.UpdatedAt = DateTime.UtcNow;
                answer.IsActive = true;
                answer.IsAccepted = false;
                answer.ViewCount = 0;

                await _answerRepository.AddAsync(answer, ct);

                // Обновляем счетчик ответов у вопроса
                question.AnswerCount++;
                await _questionRepository.UpdateAsync(question, ct);

                var response = _mapper.Map<QuestionAnswerResponseDTO>(answer);
                response.Username = user.Username;

                _logger.LogInformation("Answer created successfully: {AnswerId} for question: {QuestionId} by user: {UserId}", 
                    answer.Id, dto.QuestionId, dto.UserId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating answer");
                throw;
            }
        }

        public async Task<List<QuestionAnswerResponseDTO>> BulkCreateAsync(IEnumerable<CreateQuestionAnswerDTO> dtos, CancellationToken ct = default)
        {
            try
            {
                var answers = new List<QuestionAnswer>();
                var responses = new List<QuestionAnswerResponseDTO>();

                foreach (var dto in dtos)
                {
                    // Проверяем существование вопроса и пользователя
                    var question = await _questionRepository.GetByIdAsync(dto.QuestionId, ct);
                    if (question == null)
                        throw new NotFoundException($"Question with ID {dto.QuestionId} not found");

                    var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
                    if (user == null)
                        throw new NotFoundException($"User with ID {dto.UserId} not found");

                    var answer = _mapper.Map<QuestionAnswer>(dto);
                    answer.Id = Guid.NewGuid();
                    answer.CreatedAt = DateTime.UtcNow;
                    answer.UpdatedAt = DateTime.UtcNow;
                    answer.IsActive = true;
                    answer.IsAccepted = false;
                    answer.ViewCount = 0;

                    answers.Add(answer);

                    var response = _mapper.Map<QuestionAnswerResponseDTO>(answer);
                    response.Username = user.Username;
                    responses.Add(response);
                }

                await _answerRepository.AddBulkAsync(answers, ct);

                // Обновляем счетчики ответов
                var questionIds = answers.Select(a => a.QuestionId).Distinct();
                foreach (var questionId in questionIds)
                {
                    var question = await _questionRepository.GetByIdAsync(questionId, ct);
                    if (question != null)
                    {
                        question.AnswerCount = answers.Count(a => a.QuestionId == questionId);
                        await _questionRepository.UpdateAsync(question, ct);
                    }
                }

                _logger.LogInformation("Bulk create completed. Created {Count} answers", answers.Count);

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk create answers");
                throw;
            }
        }

        public async Task UpdateAsync(Guid id, UpdateQuestionAnswerDTO dto, CancellationToken ct = default)
        {
            try
            {
                var answer = await _answerRepository.GetByIdAsync(id, ct);
                if (answer == null)
                    throw new NotFoundException($"Answer with ID {id} not found");

                // Обновляем поля
                if (!string.IsNullOrEmpty(dto.Content))
                    answer.Content = dto.Content;

                if (dto.IsAccepted.HasValue && dto.IsAccepted.Value)
                {
                    // Если устанавливаем как принятый, снимаем отметку с других ответов на этот вопрос
                    await _answerRepository.MarkAsAcceptedAsync(id, ct);
                    return; // MarkAsAcceptedAsync уже сохранит изменения
                }

                answer.UpdatedAt = DateTime.UtcNow;

                await _answerRepository.UpdateAsync(answer, ct);

                _logger.LogInformation("Answer updated successfully: {AnswerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating answer: {AnswerId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var answer = await _answerRepository.GetByIdAsync(id, ct);
                if (answer == null)
                    throw new NotFoundException($"Answer with ID {id} not found");

                // Soft delete
                answer.IsActive = false;
                answer.UpdatedAt = DateTime.UtcNow;

                await _answerRepository.UpdateAsync(answer, ct);

                // Обновляем счетчик ответов у вопроса
                var question = await _questionRepository.GetByIdAsync(answer.QuestionId, ct);
                if (question != null)
                {
                    question.AnswerCount = Math.Max(0, question.AnswerCount - 1);
                    await _questionRepository.UpdateAsync(question, ct);
                }

                _logger.LogInformation("Answer deleted successfully: {AnswerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting answer: {AnswerId}", id);
                throw;
            }
        }

        public async Task AcceptAsync(Guid answerId, CancellationToken ct = default)
        {
            try
            {
                var answer = await _answerRepository.GetByIdAsync(answerId, ct);
                if (answer == null)
                    throw new NotFoundException($"Answer with ID {answerId} not found");

                await _answerRepository.MarkAsAcceptedAsync(answerId, ct);

                _logger.LogInformation("Answer accepted: {AnswerId}", answerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting answer: {AnswerId}", answerId);
                throw;
            }
        }

        public async Task<QuestionAnswerResponseDTO> GetAcceptedAsync(Guid questionId, CancellationToken ct = default)
        {
            try
            {
                var answer = await _answerRepository.GetAcceptedAnswerAsync(questionId, ct);
                if (answer == null)
                    throw new NotFoundException($"No accepted answer found for question: {questionId}");

                var user = await _userRepository.GetByIdAsync(answer.UserId, ct);

                var response = _mapper.Map<QuestionAnswerResponseDTO>(answer);
                response.Username = user?.Username ?? "Unknown";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accepted answer for question: {QuestionId}", questionId);
                throw;
            }
        }

        public async Task<int> GetAnswerCountAsync(Guid questionId, CancellationToken ct = default)
        {
            try
            {
                var answers = await _answerRepository.GetByQuestionIdAsync(questionId, ct);
                return answers.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting answer count for question: {QuestionId}", questionId);
                throw;
            }
        }
    }
}