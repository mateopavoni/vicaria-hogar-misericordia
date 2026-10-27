using FluentValidation;

namespace Vicaria.Application.SocialRecords;

public class LogRecordExportDtoValidator : AbstractValidator<LogRecordExportDto>
{
    public static readonly string[] AllowedSections = ["datos", "datos-observaciones", "todo"];
    public static readonly string[] AllowedFormats = ["pdf", "print"];

    public LogRecordExportDtoValidator()
    {
        RuleFor(x => x.Sections)
            .Must(s => AllowedSections.Contains(s))
            .WithMessage("Las secciones exportadas no son válidas.");

        RuleFor(x => x.Format)
            .Must(f => AllowedFormats.Contains(f))
            .WithMessage("El formato de exportación no es válido.");
    }
}
