namespace Vicaria.Domain.Entities;

// las tres etapas de la historia de vida (SCRUM-171): se editan de forma
// independiente y en momentos distintos, cada una con su propio autor/fecha
public enum LifeStoryStage
{
    BeforeHogar,
    InHogar,
    AfterHogar
}