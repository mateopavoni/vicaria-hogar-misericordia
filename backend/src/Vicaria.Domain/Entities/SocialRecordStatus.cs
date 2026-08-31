namespace Vicaria.Domain.Entities;

// ponytail: solo Active/Inactive por ahora (lo único que pide SCRUM-5); si SCRUM-76/78
// necesitan un estado intermedio (ej. en Casona) se agrega ahí, no se lo inventa acá
public enum SocialRecordStatus
{
    Active,
    Inactive
}
