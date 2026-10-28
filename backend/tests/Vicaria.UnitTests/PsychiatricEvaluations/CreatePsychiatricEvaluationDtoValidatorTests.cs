using Vicaria.Application.PsychiatricEvaluations;

namespace Vicaria.UnitTests.PsychiatricEvaluations;

public class CreatePsychiatricEvaluationDtoValidatorTests
{
    // "hoy" es el de Córdoba (UTC-3): mañana se rechaza aunque en UTC ya sea ese día
    [Fact]
    public void Validate_FechaDeMananaEnCordoba_EsInvalida()
    {
        var cordobaToday = DateTime.UtcNow.AddHours(-3).Date;
        var validator = new CreatePsychiatricEvaluationDtoValidator();

        var tomorrow = validator.Validate(new CreatePsychiatricEvaluationDto(cordobaToday.AddDays(1), "Dr. Test", "Diagnóstico"));
        var today = validator.Validate(new CreatePsychiatricEvaluationDto(cordobaToday, "Dr. Test", "Diagnóstico"));

        Assert.False(tomorrow.IsValid);
        Assert.True(today.IsValid);
    }
}
