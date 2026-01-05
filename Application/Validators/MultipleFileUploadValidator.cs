using FluentValidation;
using IceBreakerApp.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace IceBreakerApp.Application.Validators
{
    public class MultipleFileUploadValidator : AbstractValidator<MultipleFileUploadRequest>
    {
        private readonly int _maxFiles = 10;
        private readonly long _maxTotalFileSize = 100 * 1024 * 1024; // 100 MB

        public MultipleFileUploadValidator()
        {
            RuleFor(request => request.Files)
                .NotNull().WithMessage("Список файлов не может быть пустым.")
                .Must(files => files != null && files.Count > 0).WithMessage("Необходимо загрузить хотя бы один файл.")
                .Must(files => files.Count <= _maxFiles).WithMessage($"Нельзя загружать более {_maxFiles} файлов за раз.");

            RuleFor(request => request.Files)
                .Must(BeWithinTotalSizeLimit).WithMessage($"Общий размер файлов не должен превышать {_maxTotalFileSize / (1024 * 1024)} MB.");

            RuleForEach(request => request.Files)
                .SetValidator(new FileUploadValidator()); // Применяем валидатор для каждого файла
        }

        private bool BeWithinTotalSizeLimit(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return true;
            }
            return files.Sum(f => f.Length) <= _maxTotalFileSize;
        }
    }
}