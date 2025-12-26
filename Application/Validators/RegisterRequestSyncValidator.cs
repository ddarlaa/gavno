using FluentValidation;
using IceBreakerApp.Application.DTOs;

namespace IceBreakerApp.Application.Validators
{
    public class RegisterRequestSyncValidator : AbstractValidator<RegisterRequestDTO>
    {
        public RegisterRequestSyncValidator()
        {
            // Email basic validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(200).WithMessage("Email must not exceed 200 characters");
            
            // Username basic validation
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers and underscores");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            // ConfirmPassword validation
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            // Required fields validation
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            // Optional fields validation
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // Date of birth validation
            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
                .Must(BeAtLeast18YearsOld).WithMessage("Must be at least 18 years old")
                .When(x => x.DateOfBirth.HasValue);
        }

        private bool BeAtLeast18YearsOld(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return true;
            
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Value.Year;
            
            if (dateOfBirth.Value.Date > today.AddYears(-age))
                age--;
                
            return age >= 18;
        }
    }
}