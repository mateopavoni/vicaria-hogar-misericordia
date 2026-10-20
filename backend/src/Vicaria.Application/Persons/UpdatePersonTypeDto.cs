using Vicaria.Domain.Entities;

namespace Vicaria.Application.Persons;

// cuerpo de PUT /api/persons/{id}/type (SCRUM-134)
public record UpdatePersonTypeDto(PersonType PersonType);