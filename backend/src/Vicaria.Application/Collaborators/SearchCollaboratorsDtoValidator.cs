using FluentValidation;

namespace Vicaria.Application.Collaborators;

public class SearchCollaboratorsDtoValidator : AbstractValidator<SearchCollaboratorsDto>
{
    public SearchCollaboratorsDtoValidator()
    {
        RuleFor(x => x.Q)
            .MaximumLength(100).WithMessage("El texto de búsqueda no puede superar los 100 caracteres.");
    }
}
