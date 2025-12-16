using FluentValidation;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IRepositories;

namespace IceBreakerApp.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDTO>
    {
        private readonly IUserRepository _userRepository;

        public RegisterRequestValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

            // Username validation
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers and underscores")
                .MustAsync(BeUniqueUsernameAsync).WithMessage("Username already exists");

            // Email validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
                .MustAsync(BeUniqueEmailAsync).WithMessage("Email already exists");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            // ConfirmPassword validation
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            // Optional fields validation
            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            RuleFor(x => x.DisplayName)
                .MaximumLength(255).WithMessage("Display name must not exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.DisplayName));

            RuleFor(x => x.Bio)
                .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // Date of birth validation
            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
                .GreaterThan(DateTime.Today.AddYears(-120)).WithMessage("Invalid date of birth")
                .When(x => x.DateOfBirth.HasValue);
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