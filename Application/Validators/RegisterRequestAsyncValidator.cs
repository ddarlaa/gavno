using FluentValidation;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;

namespace IceBreakerApp.Application.Validators
{
    // Маркерный интерфейс для асинхронных валидаторов
    public interface IRegisterRequestAsyncValidator
    {
    }

    public class RegisterRequestAsyncValidator : AbstractValidator<RegisterRequestDTO>, IRegisterRequestAsyncValidator
    {
        private readonly IUserRepository _userRepository;

        public RegisterRequestAsyncValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

            // Асинхронные правила для использования вручную, НЕ для автоматической валидации
            RuleFor(x => x.Email)
                .MustAsync(BeUniqueEmailAsync).WithMessage("Email already exists");
            
            RuleFor(x => x.Username)
                .MustAsync(BeUniqueUsernameAsync).WithMessage("Username already exists");
        }

        private async Task<bool> BeUniqueUsernameAsync(string username, CancellationToken cancellationToken)
        {
            var exists = await _userRepository.UsernameExistsAsync(username, cancellationToken);
            return !exists;
        }

        private async Task<bool> BeUniqueEmailAsync(string email, CancellationToken cancellationToken)
        {
            var exists = await _userRepository.EmailExistsAsync(email, cancellationToken);
            return !exists;
        }
    }
}