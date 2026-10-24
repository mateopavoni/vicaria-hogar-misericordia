using FluentValidation;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

public record UpdatePersonStatusDto(
    SocialRecordStatus Status,
    string Reason
);

public class UpdatePersonStatusDtoValidator : AbstractValidator<UpdatePersonStatusDto>
{
    public UpdatePersonStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("El estado indicado no es válido.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("El motivo del cambio de estado es obligatorio.")
            .MaximumLength(500)
            .WithMessage("El motivo no puede superar los 500 caracteres.");
    }
}