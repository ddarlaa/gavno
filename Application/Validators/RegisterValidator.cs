using FluentValidation;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;

namespace IceBreakerApp.Application.Validators;

public class RegisterValidator: AbstractValidator<RegisterDTO>
{
    private readonly IUserRepository _userRepository;
    
    public RegisterValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Неверный формат email")
            .MaximumLength(200).WithMessage("Email не должен превышать 200 символов")
            .MustAsync(async (email, cancellation) =>
                await IsEmailUniqueAsync(email, cancellation))
            .WithMessage("Email уже используется");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .Length(3, 50).WithMessage("Имя пользователя должно быть от 3 до 50 символов")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("Имя пользователя может содержать только буквы, цифры и подчёркивание")
            .MustAsync(async (username, cancellation) =>
                await IsUsernameUniqueAsync(username, cancellation))
            .WithMessage("Имя пользователя уже используется");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
            .Matches("[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву")
            .Matches("[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")
            .WithMessage("Пароль должен содержать хотя бы один специальный символ");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Пароли не совпадают");
    }

    private async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken)
    {
        return await _userRepository.IsEmailUniqueAsync(email, cancellationToken);
    }

    private async Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken)
    {
        return await _userRepository.IsUsernameUniqueAsync(username, cancellationToken);
    }
    
}
