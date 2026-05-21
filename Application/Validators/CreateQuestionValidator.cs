using FluentValidation;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.IServices;

namespace IceBreakerApp.Application.Validators;

public class CreateQuestionValidator : AbstractValidator<CreateQuestionDTO>
{
    private readonly IUserService _userService;
    private readonly ITopicService _topicService;

    public CreateQuestionValidator(IUserService userService, ITopicService topicService)
    {
        _userService = userService;
        _topicService = topicService;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .Must(UserExists).WithMessage("User does not exist"); 

        RuleFor(x => x.TopicId)
            .NotEmpty().WithMessage("Topic ID is required")
            .Must(TopicExists).WithMessage("Topic does not exist"); 

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(10).WithMessage("Content must be at least 10 characters")
            .MaximumLength(5000).WithMessage("Content must not exceed 5000 characters");
    }

    private bool UserExists(Guid userId) 
    {
        return _userService.GetByIdAsync(userId, CancellationToken.None).Result != null;
    }

    private bool TopicExists(Guid topicId) 
    {
        return _topicService.GetByIdAsync(topicId, CancellationToken.None).Result != null;
    }
}