using Vicaria.Application.Auth;
using Vicaria.Application.Common;
using Vicaria.Application.Observations;

namespace Vicaria.UnitTests.Common;

public class SensitiveContentRulesTests
{
    [Theory]
    [InlineData("Refiere abuso sexual en la infancia")]
    [InlineData("fue víctima de ABUSO SEXUAL")]
    [InlineData("Sufrió una violación hace años")]
    [InlineData("la violaron en la calle")]
    [InlineData("Violencia sexual por parte de la pareja")]
    public void ContainsSensitiveContent_FrasesDeAbuso_DevuelveTrue(string text)
    {
        Assert.True(SensitiveContentRules.ContainsSensitiveContent(text));
    }

    [Theory]
    [InlineData("Consumo problemático, abuso de sustancias")]
    [InlineData("Se acercó al comedor, buenas condiciones de salud")]
    [InlineData("Víctima de robo en la vía pública")]
    [InlineData("")]
    [InlineData(null)]
    public void ContainsSensitiveContent_TextoHabitual_DevuelveFalse(string? text)
    {
        Assert.False(SensitiveContentRules.ContainsSensitiveContent(text));
    }

    [Fact]
    public void CreateObservationValidator_ConContenidoDeAbuso_FallaConElMensaje()
    {
        var result = new CreateObservationDtoValidator().Validate(new CreateObservationDto("Relata abuso sexual", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == SensitiveContentRules.Message);
    }

    [Fact]
    public void RegisterValidator_ConMarcadoHtmlEnElNombre_Falla()
    {
        var result = new RegisterDtoValidator().Validate(new RegisterDto("<img src=x onerror=alert(1)>", "Perez", "a@b.com", "Test1234!"));

        Assert.False(result.IsValid);
    }
}
