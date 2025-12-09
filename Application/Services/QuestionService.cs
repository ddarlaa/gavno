using AutoMapper;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using Microsoft.AspNetCore.JsonPatch;

namespace IceBreakerApp.Application.Services;

public class QuestionService(
    IQuestionRepository questionRepository,
    IUserRepository userRepository,
    ITopicRepository topicRepository,
    IMapper mapper)
    : IQuestionService
{
    public async Task<QuestionResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var question = await questionRepository.GetByIdAsync(id, ct);
        if (question == null || !question.IsActive)
            return null;

        // Инкремент счетчика просмотров
        question.IncrementViewCount();
        await questionRepository.UpdateAsync(question, ct);

        return await MapToResponseDtoAsync(question, ct);
    }

    public async Task<PaginatedResult<QuestionResponseDTO>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? search,
        Guid? topicId,
        CancellationToken ct = default)
    {
        // Валидация параметров пагинации
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Получаем пагинированные вопросы из репозитория
        var paginatedQuestions = await questionRepository.GetPaginatedAsync(
            pageNumber, pageSize, sortBy, sortOrder, search, topicId, ct);

        // Получаем все ID пользователей и тем
        var userIds = paginatedQuestions.Items.Select(q => q.UserId).Distinct();
        var topicIds = paginatedQuestions.Items.Select(q => q.TopicId).Distinct();

        // Batch-запросы (по 2 запроса вместо 2N)
        var users = await userRepository.GetByIdsAsync(userIds, ct);
        var topics = await topicRepository.GetByIdsAsync(topicIds, ct);

        var userDict = users.ToDictionary(u => u.Id);
        var topicDict = topics.ToDictionary(t => t.Id);

        // Маппинг без дополнительных запросов
        var responseDtos = new List<QuestionResponseDTO>();
        foreach (var question in paginatedQuestions.Items)
        {
            var dto = mapper.Map<QuestionResponseDTO>(question);

            if (userDict.TryGetValue(question.UserId, out var user))
                dto.UserDisplayName = user.DisplayName;

            if (topicDict.TryGetValue(question.TopicId, out var topic))
                dto.TopicName = topic.Name;

            responseDtos.Add(dto);
        }

        return new PaginatedResult<QuestionResponseDTO>(responseDtos,
            paginatedQuestions.TotalCount,
            paginatedQuestions.PageNumber,
            paginatedQuestions.PageSize);
    }

    public async Task<QuestionResponseDTO> CreateAsync(CreateQuestionDTO dto, CancellationToken ct = default)
    {
        // Валидация существования связанных сущностей
        var user = await userRepository.GetByIdAsync(dto.UserId, ct);
        if (user == null)
            throw new NotFoundException("User", dto.UserId);

        var topic = await topicRepository.GetByIdAsync(dto.TopicId, ct);
        if (topic == null)
            throw new NotFoundException("Topic", dto.TopicId);

        // Создание доменной сущности через фабричный метод
        var question = Question.Create(dto.UserId, dto.TopicId, dto.Title, dto.Content);

        // Сохранение
        await questionRepository.AddAsync(question, ct);

        return await MapToResponseDtoAsync(question, ct);
    }

    public async Task UpdateAsync(Guid id, UpdateQuestionDTO dto, CancellationToken ct = default)
    {
        var question = await questionRepository.GetByIdAsync(id, ct);
        if (question == null || !question.IsActive)
            throw new NotFoundException("Question", id);

        // Валидация TopicId если он изменяется
        if (dto.TopicId.HasValue)
        {
            var topic = await topicRepository.GetByIdAsync(dto.TopicId.Value, ct);
            if (topic == null)
                throw new NotFoundException("Topic", dto.TopicId.Value);
        }

        // Обновление через доменный метод
        question.Update(dto.Title, dto.Content, dto.TopicId);

        await questionRepository.UpdateAsync(question, ct);
    }

    public async Task PatchAsync(
        Guid id,
        JsonPatchDocument<UpdateQuestionDTO> patchDoc,
        CancellationToken ct = default)
    {
        var question = await questionRepository.GetByIdAsync(id, ct);
        if (question == null || !question.IsActive)
            throw new NotFoundException("Question", id);

        // Маппинг в DTO для применения патча
        var updateDto = mapper.Map<UpdateQuestionDTO>(question);
        patchDoc.ApplyTo(updateDto);

        // Валидация TopicId если он был изменен
        if (updateDto.TopicId.HasValue)
        {
            var topic = await topicRepository.GetByIdAsync(updateDto.TopicId.Value, ct);
            if (topic == null)
                throw new NotFoundException("Topic", updateDto.TopicId.Value);
        }

        // Обновление через доменный метод
        question.Update(updateDto.Title, updateDto.Content, updateDto.TopicId);

        await questionRepository.UpdateAsync(question, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var question = await questionRepository.GetByIdAsync(id, ct);
        if (question == null || !question.IsActive)
            throw new NotFoundException("Question", id);

        // Soft delete через доменный метод
        question.Delete();

        await questionRepository.UpdateAsync(question, ct);
    }

    public async Task<BulkOperationResult<QuestionResponseDTO>> BulkCreateAsync(
        List<CreateQuestionDTO> dtos,
        CancellationToken ct = default)
    {
        var result = new BulkOperationResult<QuestionResponseDTO>();

        for (int i = 0; i < dtos.Count; i++)
        {
            try
            {
                var created = await CreateAsync(dtos[i], ct);
                result.SuccessItems.Add(created);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkOperationError
                {
                    Index = i,
                    Error = ex.Message
                });
            }
        }

        return result;
    }

    // Вспомогательные методы
    private async Task<QuestionResponseDTO> MapToResponseDtoAsync(
        Question question,
        CancellationToken ct)
    {
        var dto = mapper.Map<QuestionResponseDTO>(question);

        // Заполнение связанных данных
        var user = await userRepository.GetByIdAsync(question.UserId, ct);
        if (user != null)
            dto.UserDisplayName = user.DisplayName;

        var topic = await topicRepository.GetByIdAsync(question.TopicId, ct);
        if (topic != null)
            dto.TopicName = topic.Name;

        return dto;
    }
}