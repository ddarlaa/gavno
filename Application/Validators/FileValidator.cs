using FluentValidation;
using Microsoft.AspNetCore.Http;

public class FileUploadValidator : AbstractValidator<IFormFile>
{
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    public FileUploadValidator()
    {
        RuleFor(file => file)
            .NotNull().WithMessage("Файл не может быть пустым.");

        RuleFor(file => file.Length)
            .Must(size => size > 0).WithMessage("Файл не может быть пустым.")
            .Must(size => size <= _maxFileSize).WithMessage($"Размер файла не должен превышать {_maxFileSize / 1024 / 1024} МБ.");

        RuleFor(file => file.FileName)
            .Must(NotContainPathTraversal).WithMessage("Имя файла содержит недопустимые символы (../ или ..\\).");

        RuleFor(file => file)
            .Must(BeValidFile)
            .WithMessage((_, file) => $"Недопустимый тип файла. Разрешены: JPEG, PNG, GIF, WebP, PDF, DOCX, XLSX.");
    }

    private bool NotContainPathTraversal(string fileName)
    {
        return !fileName.Contains("../") && !fileName.Contains(@"..\");
    }

    private bool BeValidFile(IFormFile file)
    {
        var (isValid, detectedType) = FileSignature.Validate(file);
        if (!isValid) return false;

        if (!AllowedContentTypes.Contains(detectedType)) return false;

        // Проверка соответствия расширения
        if (!FileSignature.IsAllowedExtension(file.FileName, detectedType)) return false;

        return true;
    }
}