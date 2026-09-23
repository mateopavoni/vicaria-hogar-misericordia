using FluentValidation;

namespace Vicaria.Application.LifeStories;

public class UpdateLifeStoryStageDtoValidator : AbstractValidator<UpdateLifeStoryStageDto>
{
    public UpdateLifeStoryStageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("El contenido de la etapa es obligatorio.");
    }
}