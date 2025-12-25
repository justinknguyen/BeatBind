using FluentValidation;

namespace BeatBind.Application.Commands
{
    public class UpdateClientCredentialsCommandValidator : AbstractValidator<UpdateClientCredentialsCommand>
    {
        public UpdateClientCredentialsCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty().WithMessage("Client ID is required.");
            RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("Client Secret is required.");
        }
    }
}
