using System.Text.RegularExpressions;
using FluentValidation;
using Vicaria.Application.Common;

namespace Vicaria.Application.Common;

// Control técnico del acuerdo de no cargar información de abusos en el sistema (se maneja solo
// de forma verbal). Es una red de seguridad, no un filtro perfecto: lista conservadora de frases
// inequívocas para no bloquear usos legítimos (ej. "abuso de sustancias"). Para ajustarla, editar Pattern.
public static partial class SensitiveContentRules
{
    public const string Message =
        "No se puede cargar información sobre abusos en el sistema. Ese tema se maneja solo de forma verbal con el equipo.";

    [GeneratedRegex(
        @"abus(o|os|ó|aron|ada|ado|adas|ados)\s+(sexual(es|mente)?|infantil)|(sexual(mente)?\s+)abus(ó|aron|ada|ado)|violaci(ó|o)n|violencia\s+sexual|\bviol(ó|aron)\b|\bviolad(a|o)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    // nombres y apellidos no admiten marcado HTML (< >); evita guardar payloads tipo <img onerror=...>
    public static IRuleBuilderOptions<T, string?> MustBeAPlainName<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(text => text is null || !text.AsSpan().ContainsAny('<', '>')).WithMessage("El nombre no puede contener los caracteres < o >.");

    public static bool ContainsSensitiveContent(string? text) =>
        !string.IsNullOrWhiteSpace(text) && Pattern().IsMatch(text);

    public static IRuleBuilderOptions<T, string?> MustNotContainSensitiveContent<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(text => !ContainsSensitiveContent(text)).WithMessage(Message);
}
