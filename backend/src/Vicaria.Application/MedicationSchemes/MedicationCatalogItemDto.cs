namespace Vicaria.Application.MedicationSchemes;

// medicamento del catálogo con su stock de farmacia, para el formulario de esquemas
public record MedicationCatalogItemDto(Guid Id, string Name, int PharmacyStock);
