using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Services;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;


public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ITopicRepository> _mockTopicRepo;
    private readonly Mock<IQuestionLikeService> _mockLikeService;
    private readonly Mock<IQuestionAnswerService> _mockAnswerService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<QuestionService>> _mockLogger;
    private readonly Mock<IFileService> _mockFileService;
    private readonly QuestionService _service;

    public QuestionServiceTests()
    {
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTopicRepo = new Mock<ITopicRepository>();
        _mockLikeService = new Mock<IQuestionLikeService>();
        _mockAnswerService = new Mock<IQuestionAnswerService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<QuestionService>>();
        _mockFileService = new Mock<IFileService>();

        _service = new QuestionService(
            _mockQuestionRepo.Object,
            _mockUserRepo.Object,
            _mockTopicRepo.Object,
            _mockLikeService.Object,
            _mockAnswerService.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockFileService.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsEnrichedDtoAndIncrementsView()
    {
        // Arrange
        var qId = Guid.NewGuid();
        var uId = Guid.NewGuid();
        var tId = Guid.NewGuid();

        var question = new Question { Id = qId, UserId = uId, TopicId = tId, ViewCount = 10 };
        var user = new User { Id = uId, Username = "tester", DisplayName = "Test User" };
        var topic = new Topic { Id = tId, Name = "General" };

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(qId, default)).ReturnsAsync(question);
        _mockUserRepo.Setup(r => r.GetByIdAsync(uId, default)).ReturnsAsync(user);
        _mockTopicRepo.Setup(r => r.GetByIdAsync(tId, default)).ReturnsAsync(topic);
        _mockLikeService.Setup(s => s.GetLikeCountAsync(qId, default)).ReturnsAsync(5);
        _mockAnswerService.Setup(s => s.GetAnswerCountAsync(qId, default)).ReturnsAsync(2);

        _mockMapper.Setup(m => m.Map<QuestionResponseDTO>(question))
            .Returns(new QuestionResponseDTO { Id = qId });

        // Act
        var result = await _service.GetByIdAsync(qId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tester", result.Username); // Проверка обогащения данных
        Assert.Equal("General", result.TopicName);
        Assert.Equal(5, result.LikeCount);

        // Проверка инкремента просмотров
        Assert.Equal(11, question.ViewCount);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(question, default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UserOrTopicNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateQuestionDTO { UserId = Guid.NewGuid(), TopicId = Guid.NewGuid() };
        _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId, default))
            .ReturnsAsync((User?)null); // User not found

        // Act & Assert
        // Предполагаем, что NotFoundException - это ваш кастомный тип, либо используем KeyNotFoundException если стандартный
        // Если у вас свой класс NotFoundException, замените KeyNotFoundException на него в Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Success_CallsAddAsync()
    {
        // Arrange
        var dto = new CreateQuestionDTO { UserId = Guid.NewGuid(), TopicId = Guid.NewGuid(), Title = "Title" };

        _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId, default)).ReturnsAsync(new User());
        _mockTopicRepo.Setup(r => r.GetByIdAsync(dto.TopicId, default)).ReturnsAsync(new Topic());

        var questionEntity = new Question();
        _mockMapper.Setup(m => m.Map<Question>(dto)).Returns(questionEntity);
        _mockMapper.Setup(m => m.Map<QuestionResponseDTO>(It.IsAny<Question>())).Returns(new QuestionResponseDTO());

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _mockQuestionRepo.Verify(r => r.AddAsync(It.Is<Question>(q =>
            q.ViewCount == 0 && q.IsActive == true
        ), default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndTimestamp()
    {
        // Arrange
        var qId = Guid.NewGuid();
        var existingQ = new Question { Id = qId, Title = "Old", Content = "Old" };
        var updateDto = new UpdateQuestionDTO { Title = "New" }; // Content is null, shouldn't change

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(qId, default)).ReturnsAsync(existingQ);

        // Act
        await _service.UpdateAsync(qId, updateDto);

        // Assert
        Assert.Equal("New", existingQ.Title);
        Assert.Equal("Old", existingQ.Content); // Не должен был измениться
        _mockQuestionRepo.Verify(r => r.UpdateAsync(existingQ, default), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_HandlesPartialFailures()
    {
        // Arrange
        var dtoList = new List<CreateQuestionDTO>
        {
            new() { Title = "Valid" },
            new() { Title = "Invalid" }
        };

        // Мокаем так, чтобы первый вызов CreateAsync прошел успешно, а второй упал
        // Поскольку CreateAsync вызывает внутренние методы, мы можем замокать репозитории
        // 1. Успешный кейс (UserId найден)
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new User());
        _mockTopicRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new Topic());

        _mockMapper.Setup(m => m.Map<Question>(It.IsAny<CreateQuestionDTO>())).Returns(new Question());
        _mockMapper.Setup(m => m.Map<QuestionResponseDTO>(It.IsAny<Question>())).Returns(new QuestionResponseDTO());

        // Имитируем ошибку на втором AddAsync (например, база упала)
        _mockQuestionRepo.SetupSequence(r => r.AddAsync(It.IsAny<Question>(), default))
            .ReturnsAsync(new Question()) // 1-й раз успешно
            .ThrowsAsync(new Exception("Database error")); // 2-й раз ошибка

        // Act
        var result = await _service.BulkCreateAsync(dtoList);

        // Assert
        Assert.Equal(1, result.SuccessItems.Count);
        Assert.Equal(1, result.Errors.Count);
        Assert.Equal("Database error", result.Errors[0].Error);
    }
}

public class TopicServiceTests
{
    private readonly Mock<ITopicRepository> _mockTopicRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<TopicService>> _mockLogger;
    private readonly Mock<IFileService> _mockFileService;
    private readonly TopicService _service;

    public TopicServiceTests()
    {
        _mockTopicRepo = new Mock<ITopicRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<TopicService>>();
        _mockFileService = new Mock<IFileService>();

        _service = new TopicService(
            _mockTopicRepo.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockFileService.Object
        );
    }

    [Fact]
    public async Task GetAllAsync_InvalidPageSize_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAllAsync(1, 101)); // > 100
    }

    [Fact]
    public async Task CreateAsync_UniqueName_CreatesTopic()
    {
        // Arrange
        var createDto = new CreateTopicDTO { Name = "New Topic" };
        _mockTopicRepo.Setup(r => r.FindByNameAsync("New Topic", default))
            .ReturnsAsync((Topic?)null);

        var topicEntity = new Topic { Id = Guid.NewGuid(), Name = "New Topic" };

        _mockMapper.Setup(m => m.Map<Topic>(createDto)).Returns(topicEntity);
        _mockMapper.Setup(m => m.Map<TopicResponseDTO>(It.IsAny<Topic>()))
            .Returns(new TopicResponseDTO());

        _mockTopicRepo.Setup(r => r.AddAsync(It.IsAny<Topic>(), default))
            .ReturnsAsync(topicEntity); // Возвращаем созданную сущность

        // Act
        await _service.CreateAsync(createDto);

        // Assert
        _mockTopicRepo.Verify(r => r.AddAsync(It.Is<Topic>(t =>
            t.Name == "New Topic" && t.IsActive == true
        ), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperation()
    {
        // Arrange
        _mockTopicRepo.Setup(r => r.FindByNameAsync("Existing", default))
            .ReturnsAsync(new Topic());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new CreateTopicDTO { Name = "Existing" }));
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockTopicRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Topic?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteAsync(Guid.NewGuid()));
    }
}

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockMapper = new Mock<IMapper>();

        _service = new UserService(
            _mockUserRepo.Object,
            _mockLogger.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task CreateAsync_UniqueUser_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateUserDTO
        {
            Email = "test@test.com",
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepo.Setup(r => r.EmailExistsAsync(createDto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepo.Setup(r => r.UsernameExistsAsync(createDto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userEntity = new User { Id = Guid.NewGuid(), Username = createDto.Username };
        var expectedDto = new UserResponseDTO { Id = userEntity.Id, Username = createDto.Username };

        // Настройка маппера: DTO -> Entity (игнорируем здесь, т.к. маппинг ручной в сервисе)
        // Настройка маппера: Entity -> ResponseDTO
        _mockMapper.Setup(m => m.Map<UserResponseDTO>(It.IsAny<User>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        _mockUserRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == createDto.Email &&
                u.Username == createDto.Username &&
                !string.IsNullOrEmpty(u.PasswordHash) // Проверяем, что пароль захеширован
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(expectedDto.Username, result.Username);
    }

    [Fact]
    public async Task CreateAsync_ExistingEmail_ThrowsException()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateAsync(new CreateUserDTO { Email = "exists@test.com" }));
    }

    [Fact]
    public async Task AuthenticateUserAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        var password = "password123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            IsActive = true,
            IsDeleted = false,
            IsEmailConfirmed = true,
            PasswordHash = passwordHash
        };

        _mockUserRepo.Setup(r => r.FindByEmailAsync("test@test.com", default))
            .ReturnsAsync(user);

        // Act
        var result = await _service.AuthenticateUserAsync("test@test.com", password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user, result);
    }

    [Fact]
    public async Task AuthenticateUserAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            IsActive = true, IsEmailConfirmed = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct_password")
        };

        _mockUserRepo.Setup(r => r.FindByUsernameAsync("user", default))
            .ReturnsAsync(user);

        // Act
        var result = await _service.AuthenticateUserAsync("user", "wrong_password");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UserExists_UpdatesFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User { Id = userId, DisplayName = "Old Name" };
        var updateDto = new UpdateUserDTO { DisplayName = "New Name" };

        _mockUserRepo.Setup(r => r.GetByIdWithTrackingAsync(userId, default))
            .ReturnsAsync(existingUser);

        // Act
        await _service.UpdateAsync(userId, updateDto);

        // Assert
        Assert.Equal("New Name", existingUser.DisplayName);
        _mockUserRepo.Verify(r => r.UpdateAsync(existingUser, default), Times.Once);
    }
}

public class ChunkedFileServiceTests : IDisposable
{
    private readonly Mock<IFileMetadataRepository> _mockMetadataRepo;
    private readonly Mock<IUploadSessionRepository> _mockSessionRepo;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IFileStorageSettings> _mockStorageSettings;
    private readonly Mock<ILogger<ChunkedFileService>> _mockLogger;
    private readonly ChunkedFileService _service;

    // Временная папка для тестов, чтобы не мусорить в реальной системе
    private readonly string _testBaseDirectory;

    public ChunkedFileServiceTests()
    {
        _mockMetadataRepo = new Mock<IFileMetadataRepository>();
        _mockSessionRepo = new Mock<IUploadSessionRepository>();
        _mockFileService = new Mock<IFileService>();
        _mockStorageSettings = new Mock<IFileStorageSettings>();
        _mockLogger = new Mock<ILogger<ChunkedFileService>>();

        // Создаем изолированную папку для каждого запуска тестов
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);

        _mockStorageSettings.Setup(s => s.TempPath).Returns(_testBaseDirectory);

        _service = new ChunkedFileService(
            _mockMetadataRepo.Object,
            _mockSessionRepo.Object,
            _mockFileService.Object,
            _mockStorageSettings.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        // Очистка после тестов
        if (Directory.Exists(_testBaseDirectory))
        {
            try
            {
                Directory.Delete(_testBaseDirectory, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task UploadChunkAsync_FirstChunk_CreatesNewSessionAndSavesFile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var content = "Hello World Chunk 1";
        var request = CreateChunkRequest(uploadId, 0, 2, content);

        // Имитируем, что сессии еще нет
        _mockSessionRepo.Setup(r => r.GetByUploadIdAsync(uploadId))
            .ReturnsAsync((UploadSession?)null);

        // Act
        var result = await _service.UploadChunkAsync(request, userId);

        // Assert
        // 1. Проверяем, что создалась сессия
        _mockSessionRepo.Verify(r => r.AddAsync(It.Is<UploadSession>(s =>
            s.UploadId == uploadId &&
            s.UploadedChunks == 1 &&
            s.IsPublic == request.IsPublic && // Проверка сохранения метаданных
            s.UserId == userId
        )), Times.Once);

        _mockSessionRepo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);

        // 2. Проверяем, что файл физически создан
        var expectedChunkPath = Path.Combine(_testBaseDirectory, uploadId.ToString(), "chunk_0");
        Assert.True(File.Exists(expectedChunkPath));
        Assert.Equal(content, await File.ReadAllTextAsync(expectedChunkPath));

        // 3. Проверяем результат
        Assert.False(result.IsComplete);
        Assert.Equal(50, result.Percentage); // 1 из 2 чанков = 50%
    }

    [Fact]
    public async Task UploadChunkAsync_ExistingSession_UpdatesProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var uploadId = Guid.NewGuid();
        var content = "Chunk 2 content";
        var request = CreateChunkRequest(uploadId, 1, 3, content); // Чанк 1 из 3

        var existingSession = new UploadSession
        {
            UploadId = uploadId,
            TotalChunks = 3,
            UploadedChunks = 1,
            UploadedChunkIndexes = JsonSerializer.Serialize(new HashSet<int> { 0 }),
            UserId = userId
        };

        _mockSessionRepo.Setup(r => r.GetByUploadIdAsync(uploadId))
            .ReturnsAsync(existingSession);

        // Act
        var result = await _service.UploadChunkAsync(request, userId);

        // Assert
        // Проверяем, что чанк добавился в индекс и счетчик увеличился
        Assert.Equal(2, existingSession.UploadedChunks);
        var indexes = JsonSerializer.Deserialize<HashSet<int>>(existingSession.UploadedChunkIndexes);
        Assert.Contains(1, indexes);

        _mockSessionRepo.Verify(r => r.UpdateAsync(existingSession), Times.Once);

        // Проверяем файл
        var expectedChunkPath = Path.Combine(_testBaseDirectory, uploadId.ToString(), "chunk_1");
        Assert.True(File.Exists(expectedChunkPath));
    }

    [Fact]
    public async Task UploadChunkAsync_DuplicateChunk_DoesNotIncrementCount()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var request = CreateChunkRequest(uploadId, 0, 5, "Some content");

        // Сессия, где чанк 0 уже загружен
        var existingSession = new UploadSession
        {
            UploadId = uploadId,
            TotalChunks = 5,
            UploadedChunks = 1,
            UploadedChunkIndexes = JsonSerializer.Serialize(new HashSet<int> { 0 })
        };

        _mockSessionRepo.Setup(r => r.GetByUploadIdAsync(uploadId))
            .ReturnsAsync(existingSession);

        // Act
        var result = await _service.UploadChunkAsync(request, Guid.NewGuid());

        // Assert
        // Счетчик не должен измениться
        Assert.Equal(1, existingSession.UploadedChunks);

        // UpdateAsync НЕ должен вызываться для дубликата (согласно логике else в коде)
        _mockSessionRepo.Verify(r => r.UpdateAsync(It.IsAny<UploadSession>()), Times.Never);
    }

    [Fact]
    public async Task UploadChunkAsync_LastChunk_FinalizesUpload()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Настраиваем среду: создаем папку и "предыдущий" чанк
        var uploadDir = Path.Combine(_testBaseDirectory, uploadId.ToString());
        Directory.CreateDirectory(uploadDir);
        await File.WriteAllTextAsync(Path.Combine(uploadDir, "chunk_0"), "Part1");

        // Приходит второй (последний) чанк
        var request = CreateChunkRequest(uploadId, 1, 2, "Part2");

        var existingSession = new UploadSession
        {
            UploadId = uploadId,
            TotalChunks = 2,
            UploadedChunks = 1,
            FileName = "test.txt",
            ContentType = "text/plain",
            UserId = userId,
            IsPublic = true, // Тестируем передачу флага
            UploadedChunkIndexes = JsonSerializer.Serialize(new HashSet<int> { 0 })
        };

        _mockSessionRepo.Setup(r => r.GetByUploadIdAsync(uploadId))
            .ReturnsAsync(existingSession);

        // Мокаем результат от FileService
        var expectedFileMetadata = new FileMetadata { Id = Guid.NewGuid(), FileName = "test.txt" };
        _mockFileService.Setup(s => s.UploadAsync(
                It.IsAny<IFormFile>(),
                userId,
                true, // IsPublic
                It.IsAny<DateTime?>()))
            .ReturnsAsync(expectedFileMetadata);

        // Act
        var result = await _service.UploadChunkAsync(request, userId);

        // Assert
        Assert.True(result.IsComplete);
        Assert.NotNull(result.File);
        Assert.Equal(expectedFileMetadata.Id, result.File.Id);

        // Проверяем, что склейка произошла корректно
        _mockFileService.Verify(s => s.UploadAsync(It.Is<IFormFile>(f =>
                ReadStream(f.OpenReadStream()) == "Part1Part2" // Проверка содержимого склеенного файла
        ), userId, true, It.IsAny<DateTime?>()), Times.Once);

        // Проверяем очистку временной папки
        Assert.False(Directory.Exists(uploadDir), "Temp directory should be deleted after finalization");
    }

    [Fact]
    public async Task UploadChunkAsync_ConcurrencyException_Retries()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var request = CreateChunkRequest(uploadId, 0, 1, "data");

        // Настраиваем мок так, чтобы первый раз вылетел exception, а второй раз вернулся null (новая сессия)
        _mockSessionRepo.SetupSequence(r => r.GetByUploadIdAsync(uploadId))
            .ThrowsAsync(new DbUpdateConcurrencyException()) // 1 попытка - ошибка
            .ReturnsAsync((UploadSession?)null); // 2 попытка - успех

        // Act
        await _service.UploadChunkAsync(request, Guid.NewGuid());

        // Assert
        // GetByUploadIdAsync должен был вызваться 2 раза
        _mockSessionRepo.Verify(r => r.GetByUploadIdAsync(uploadId), Times.Exactly(2));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Concurrency conflict")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    // Вспомогательные методы

    private ChunkUploadRequest CreateChunkRequest(Guid uploadId, int index, int total, string content,
        bool isPublic = false)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "chunk.bin");

        return new ChunkUploadRequest
        {
            UploadId = uploadId,
            ChunkIndex = index,
            TotalChunks = total,
            Chunk = formFile,
            FileName = "test.txt",
            ContentType = "text/plain",
            IsPublic = isPublic
        };
    }

    private string ReadStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public class JwtServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockRoleService = new Mock<IRoleService>();
        _mockLogger = new Mock<ILogger<JwtService>>();

        // Настраиваем валидные настройки для JWT
        _jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForTestingPurposesOnly123!", // Должен быть длинным
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        var mockOptions = new Mock<IOptions<JwtSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_jwtSettings);

        _service = new JwtService(
            mockOptions.Object,
            _mockUserRepo.Object,
            _mockRoleService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GenerateTokensAsync_CreatesTokensAndSavesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "test",
            Email = "test@test.com",
            IsEmailConfirmed = true
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync(user);
        _mockRoleService.Setup(r => r.GetUserRolesAsync(userId, default)).ReturnsAsync(new List<Role>());
        _mockUserRepo.Setup(r => r.GetUserClaimsAsync(userId, default)).ReturnsAsync(new List<UserClaim>());

        // Act
        var result = await _service.GenerateTokensAsync(userId);

        // Assert
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);

        // Проверяем, что сессия сохранена (AddSessionAsync)
        _mockUserRepo.Verify(r => r.AddSessionAsync(It.Is<UserSession>(s =>
                s.UserId == userId &&
                !string.IsNullOrEmpty(s.RefreshTokenHash) // Проверяем, что хеш есть
        ), default), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var rawRefreshToken = "some_random_refresh_token_string";

        // Нам нужно вручную воспроизвести логику хеширования, чтобы подготовить мок
        // так как метод ComputeRefreshTokenHash приватный, но мы знаем, что он использует HMACSHA256 с ключом настроек
        string expectedHash;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawRefreshToken));
            expectedHash = Convert.ToBase64String(hashBytes);
        }

        // Имитируем сессию в базе, которая хранит этот хеш
        var session = new UserSession
        {
            RefreshTokenHash = expectedHash,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            UserId = Guid.NewGuid()
        };

        // Когда сервис вызовет GetActiveSessionByHashAsync с вычисленным внутри хешем, 
        // он должен совпасть с expectedHash
        _mockUserRepo.Setup(r => r.GetActiveSessionByHashAsync(expectedHash, default))
            .ReturnsAsync(session);

        // Act
        var isValid = await _service.ValidateRefreshTokenAsync(rawRefreshToken);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task GetUserIdFromTokenAsync_ValidToken_ReturnsId()
    {
        // Сначала генерируем валидный токен через сам сервис (интеграционный подход внутри юнита)
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "test", Email = "a@b.c" };

        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync(user);
        _mockRoleService.Setup(r => r.GetUserRolesAsync(userId, default)).ReturnsAsync(new List<Role>());
        _mockUserRepo.Setup(r => r.GetUserClaimsAsync(userId, default)).ReturnsAsync(new List<UserClaim>());

        var tokens = await _service.GenerateTokensAsync(userId);

        // Act
        var extractedId = await _service.GetUserIdFromTokenAsync(tokens.AccessToken);

        // Assert
        Assert.Equal(userId, extractedId);
    }
}

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockRoleService = new Mock<IRoleService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _service = new AuthService(
            _mockUserRepo.Object,
            _mockRoleService.Object,
            _mockJwtService.Object,
            _mockEmailService.Object,
            _mockConfig.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_NewUser_Success()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Email = "new@test.com",
            Username = "newuser",
            Password = "Password123"
        };

        _mockUserRepo.Setup(r => r.EmailExistsAsync(request.Email, default)).ReturnsAsync(false);
        _mockUserRepo.Setup(r => r.UsernameExistsAsync(request.Username, default)).ReturnsAsync(false);

        _mockEmailService.Setup(s =>
                s.SendWelcomeEmailAsync(request.Email, request.Username, It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        _mockUserRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == request.Email), default), Times.Once);
        _mockRoleService.Verify(r => r.AssignRoleToUserAsync(It.IsAny<Guid>(), "User", default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var password = "password";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Id = Guid.NewGuid(), PasswordHash = hashedPassword, IsActive = true };

        var loginDto = new LoginDTO { EmailOrUsername = "test@test.com", Password = password };

        _mockUserRepo.Setup(r => r.FindByEmailAsync(loginDto.EmailOrUsername, default))
            .ReturnsAsync(user);

        var expectedTokens = new LoginResponseDTO { AccessToken = "abc", RefreshToken = "def" };
        _mockJwtService.Setup(s => s.GenerateTokensAsync(user.Id, default))
            .ReturnsAsync(expectedTokens);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.Equal("abc", result.AccessToken);
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Once); // Update LastLogin
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var dto = new RefreshTokenDTO { RefreshToken = "valid_refresh" };
        var userId = Guid.NewGuid();

        _mockJwtService.Setup(s => s.ValidateRefreshTokenAsync(dto.RefreshToken, default))
            .ReturnsAsync(true);

        _mockJwtService.Setup(s => s.GetUserIdFromRefreshTokenAsync(dto.RefreshToken, default))
            .ReturnsAsync(userId.ToString());

        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(new User { Id = userId, IsActive = true });

        _mockJwtService.Setup(s => s.GenerateTokensAsync(userId, default))
            .ReturnsAsync(new LoginResponseDTO { AccessToken = "new_access" });

        // Act
        var result = await _service.RefreshTokenAsync(dto);

        // Assert
        Assert.Equal("new_access", result.AccessToken);
        _mockJwtService.Verify(s => s.RevokeRefreshTokenAsync(dto.RefreshToken, default), Times.Once);
    }
}

public class QuestionAnswerServiceTests
{
    private readonly Mock<IQuestionAnswerRepository> _mockAnswerRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<QuestionAnswerService>> _mockLogger;
    private readonly QuestionAnswerService _service;

    public QuestionAnswerServiceTests()
    {
        _mockAnswerRepo = new Mock<IQuestionAnswerRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<QuestionAnswerService>>();

        _service = new QuestionAnswerService(
            _mockAnswerRepo.Object,
            _mockQuestionRepo.Object,
            _mockUserRepo.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAnswerAndIncrementsCount()
    {
        // Arrange
        var dto = new CreateQuestionAnswerDTO { QuestionId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var question = new Question { Id = dto.QuestionId, AnswerCount = 0 };
        var user = new User { Id = dto.UserId, Username = "TestUser" };

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(dto.QuestionId, default)).ReturnsAsync(question);
        _mockUserRepo.Setup(r => r.GetByIdAsync(dto.UserId, default)).ReturnsAsync(user);

        var answerEntity = new QuestionAnswer { Id = Guid.NewGuid() };
        _mockMapper.Setup(m => m.Map<QuestionAnswer>(dto)).Returns(answerEntity);
        _mockMapper.Setup(m => m.Map<QuestionAnswerResponseDTO>(answerEntity))
            .Returns(new QuestionAnswerResponseDTO());

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _mockAnswerRepo.Verify(r => r.AddAsync(It.IsAny<QuestionAnswer>(), default), Times.Once);

        // Проверяем, что счетчик увеличился
        Assert.Equal(1, question.AnswerCount);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(question, default), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_UpdatesCountsCorrectly()
    {
        // Arrange
        var qId = Guid.NewGuid();
        var uId = Guid.NewGuid();
        var dtos = new List<CreateQuestionAnswerDTO>
        {
            new() { QuestionId = qId, UserId = uId },
            new() { QuestionId = qId, UserId = uId }
        };

        var question = new Question { Id = qId, AnswerCount = 0 };

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(qId, default)).ReturnsAsync(question);
        _mockUserRepo.Setup(r => r.GetByIdAsync(uId, default)).ReturnsAsync(new User());

        _mockMapper.Setup(m => m.Map<QuestionAnswer>(It.IsAny<CreateQuestionAnswerDTO>()))
            .Returns(() => new QuestionAnswer { QuestionId = qId }); // Возвращаем новые объекты
        _mockMapper.Setup(m => m.Map<QuestionAnswerResponseDTO>(It.IsAny<QuestionAnswer>()))
            .Returns(new QuestionAnswerResponseDTO());

        // Act
        await _service.BulkCreateAsync(dtos);

        // Assert
        _mockAnswerRepo.Verify(r => r.AddBulkAsync(It.Is<List<QuestionAnswer>>(l => l.Count == 2), default),
            Times.Once);

        // Проверяем, что счетчик стал равен 2
        Assert.Equal(2, question.AnswerCount);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(question, default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AcceptAsync_CallsMarkAsAccepted()
    {
        // Arrange
        var answerId = Guid.NewGuid();
        _mockAnswerRepo.Setup(r => r.GetByIdAsync(answerId, default))
            .ReturnsAsync(new QuestionAnswer());

        // Act
        await _service.AcceptAsync(answerId);

        // Assert
        _mockAnswerRepo.Verify(r => r.MarkAsAcceptedAsync(answerId, default), Times.Once);
    }
}


public class QuestionLikeServiceTests
{
    private readonly Mock<IQuestionLikeRepository> _mockLikeRepo;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<QuestionLikeService>> _mockLogger;
    private readonly QuestionLikeService _service;

    public QuestionLikeServiceTests()
    {
        _mockLikeRepo = new Mock<IQuestionLikeRepository>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<QuestionLikeService>>();

        _service = new QuestionLikeService(
            _mockLikeRepo.Object,
            _mockQuestionRepo.Object,
            _mockUserRepo.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AddLikeAsync_ValidData_AddsLikeAndUpdatesCount()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var question = new Question { Id = questionId, LikeCount = 0 };

        // 1. Вопрос и юзер существуют
        _mockQuestionRepo.Setup(r => r.GetByIdAsync(questionId, default))
            .ReturnsAsync(question);
        _mockUserRepo.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(new User { Id = userId });

        // 2. Лайка еще нет
        _mockLikeRepo.Setup(r => r.GetByQuestionAndUserAsync(questionId, userId, default))
            .ReturnsAsync((QuestionLike?)null);

        // 3. После добавления репозиторий вернет count = 1
        _mockLikeRepo.Setup(r => r.GetCountByQuestionIdAsync(questionId, default))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AddLikeAsync(questionId, userId);

        // Assert
        Assert.True(result);

        // Проверяем добавление лайка
        _mockLikeRepo.Verify(r => r.AddAsync(It.Is<QuestionLike>(l =>
            l.QuestionId == questionId && l.UserId == userId
        ), default), Times.Once);

        // Проверяем обновление счетчика в вопросе
        Assert.Equal(1, question.LikeCount);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(question, default), Times.Once);
    }

    [Fact]
    public async Task AddLikeAsync_QuestionNotFound_ReturnsFalse()
    {
        // Arrange
        _mockQuestionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Question?)null);

        // Act
        var result = await _service.AddLikeAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result);
        _mockLikeRepo.Verify(r => r.AddAsync(It.IsAny<QuestionLike>(), default), Times.Never);
    }

    [Fact]
    public async Task AddLikeAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        _mockQuestionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new Question());
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.AddLikeAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result);
        _mockLikeRepo.Verify(r => r.AddAsync(It.IsAny<QuestionLike>(), default), Times.Never);
    }

    [Fact]
    public async Task AddLikeAsync_AlreadyLiked_ReturnsFalse()
    {
        // Arrange
        var qId = Guid.NewGuid();
        var uId = Guid.NewGuid();

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(qId, default)).ReturnsAsync(new Question());
        _mockUserRepo.Setup(r => r.GetByIdAsync(uId, default)).ReturnsAsync(new User());

        // Лайк уже существует
        _mockLikeRepo.Setup(r => r.GetByQuestionAndUserAsync(qId, uId, default))
            .ReturnsAsync(new QuestionLike());

        // Act
        var result = await _service.AddLikeAsync(qId, uId);

        // Assert
        Assert.False(result);
        _mockLikeRepo.Verify(r => r.AddAsync(It.IsAny<QuestionLike>(), default), Times.Never);
    }

    [Fact]
    public async Task RemoveLikeAsync_ExistingLike_RemovesAndUpdateCount()
    {
        // Arrange
        var qId = Guid.NewGuid();
        var uId = Guid.NewGuid();
        var question = new Question { Id = qId, LikeCount = 5 };

        // Лайк найден
        _mockLikeRepo.Setup(r => r.GetByQuestionAndUserAsync(qId, uId, default))
            .ReturnsAsync(new QuestionLike());

        _mockQuestionRepo.Setup(r => r.GetByIdAsync(qId, default))
            .ReturnsAsync(question);

        // Новый счетчик после удаления будет 4
        _mockLikeRepo.Setup(r => r.GetCountByQuestionIdAsync(qId, default))
            .ReturnsAsync(4);

        // Act
        var result = await _service.RemoveLikeAsync(qId, uId);

        // Assert
        Assert.True(result);

        // Проверка удаления
        _mockLikeRepo.Verify(r => r.DeleteByQuestionAndUserAsync(qId, uId, default), Times.Once);

        // Проверка обновления вопроса
        Assert.Equal(4, question.LikeCount);
        _mockQuestionRepo.Verify(r => r.UpdateAsync(question, default), Times.Once);
    }

    [Fact]
    public async Task RemoveLikeAsync_LikeNotFound_ReturnsFalse()
    {
        // Arrange
        _mockLikeRepo.Setup(r => r.GetByQuestionAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((QuestionLike?)null);

        // Act
        var result = await _service.RemoveLikeAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result);
        _mockLikeRepo.Verify(r => r.DeleteByQuestionAndUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default),
            Times.Never);
    }

    [Fact]
    public async Task GetLikeCountAsync_Exception_ReturnsZeroAndLogsError()
    {
        // Arrange
        _mockLikeRepo.Setup(r => r.GetCountByQuestionIdAsync(It.IsAny<Guid>(), default))
            .ThrowsAsync(new Exception("DB Error"));

        // Act
        var count = await _service.GetLikeCountAsync(Guid.NewGuid());

        // Assert
        Assert.Equal(0, count);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error getting like count")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserLikedQuestionIdsAsync_ReturnsIdsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var q1 = Guid.NewGuid();
        var q2 = Guid.NewGuid();

        var likes = new List<QuestionLike>
        {
            new QuestionLike { QuestionId = q1, UserId = userId },
            new QuestionLike { QuestionId = q2, UserId = userId }
        };

        _mockLikeRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(likes);

        // Act
        var result = await _service.GetUserLikedQuestionIdsAsync(userId);

        // Assert
        Assert.Contains(q1, result);
        Assert.Contains(q2, result);
        Assert.Equal(2, result.Count());
    }
}