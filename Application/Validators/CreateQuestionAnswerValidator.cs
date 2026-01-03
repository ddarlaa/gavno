using FluentValidation;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.IServices;

namespace IceBreakerApp.Application.Validators;

public class CreateQuestionAnswerValidator : AbstractValidator<CreateQuestionAnswerDTO>
{
    private readonly IQuestionService _questionService;
    private readonly IUserService _userService;

    public CreateQuestionAnswerValidator(IQuestionService questionService, IUserService userService)
    {
        _questionService = questionService;
        _userService = userService;

        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required")
            .Must(QuestionExists).WithMessage("Question does not exist");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .Must(UserExists).WithMessage("User does not exist"); 

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(10).WithMessage("Content must be at least 10 characters")
            .MaximumLength(5000).WithMessage("Content must not exceed 5000 characters");
    }

    private bool QuestionExists(Guid questionId) 
    {
        return _questionService.GetByIdAsync(questionId, CancellationToken.None).Result != null;
    }

    private bool UserExists(Guid userId) 
    {
        return _userService.GetByIdAsync(userId, CancellationToken.None).Result != null;
    }
}