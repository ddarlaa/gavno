using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IceBreakerApp.Application.Validators
{
    public class FileUploadValidator : AbstractValidator<IFormFile>
    {
        private readonly long _maxFileSize = 50 * 1024 * 1024; // 50 MB
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        private readonly string[] _allowedContentTypes = {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        public FileUploadValidator()
        {
            RuleFor(file => file.Length)
                .NotNull().WithMessage("Файл не может быть пустым.")
                .LessThanOrEqualTo(_maxFileSize).WithMessage($"Размер файла не должен превышать {_maxFileSize / (1024 * 1024)} MB.");

            RuleFor(file => file.FileName)
                .NotNull().WithMessage("Имя файла не может быть пустым.")
                .Must(BeAValidFileName).WithMessage("Имя файла содержит недопустимые символы или является небезопасным.");

            RuleFor(file => file)
                .Must(BeAnAllowedFileType).WithMessage("Недопустимый тип файла или расширение.")
                .Must(BeAValidMagicByteSignature).WithMessage("Недопустимая сигнатура файла (magic bytes).");
        }

        private bool BeAValidFileName(string fileName)
        {
            // Проверка на path traversal
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return false;
            }

            // Проверка на запрещенные символы (Windows-специфичные)
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.Any(c => invalidChars.Contains(c)))
            {
                return false;
            }

            // Дополнительная проверка на длину имени
            if (fileName.Length > 255) // Максимальная длина имени файла
            {
                return false;
            }

            return true;
        }

        private bool BeAnAllowedFileType(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            // Проверка по расширению
            if (_allowedImageExtensions.Contains(extension) || _allowedDocumentExtensions.Contains(extension))
            {
                // Проверка по Content-Type
                return _allowedContentTypes.Contains(contentType);
            }
            return false;
        }

        private bool BeAValidMagicByteSignature(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            var (isValid, detectedContentType) = FileSignature.Validate(file);

            if (!isValid)
            {
                return false;
            }

            // Дополнительная проверка: если magic bytes определили тип,
            // убедимся, что он соответствует заявленному Content-Type и разрешенным типам.
            return _allowedContentTypes.Contains(detectedContentType.ToLowerInvariant()) &&
                   FileSignature.IsAllowedExtension(file.FileName, detectedContentType);
        }
    }
}
