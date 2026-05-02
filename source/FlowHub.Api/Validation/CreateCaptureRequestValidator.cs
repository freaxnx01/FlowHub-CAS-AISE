using FluentValidation;
using FlowHub.Api.Requests;

namespace FlowHub.Api.Validation;

public sealed class CreateCaptureRequestValidator : AbstractValidator<CreateCaptureRequest>
{
    public CreateCaptureRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content must not be empty.")
            .MaximumLength(8192).WithMessage("Content exceeds the 8192-character limit.");

        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Source must be a known ChannelKind value.");
    }
}
