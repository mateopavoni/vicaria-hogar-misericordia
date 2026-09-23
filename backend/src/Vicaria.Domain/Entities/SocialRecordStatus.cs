namespace Vicaria.Domain.Entities;

// solo Active/Inactive por ahora (lo único que pide SCRUM-5); si SCRUM-76/78
// necesitan un estado intermedio (ej. en Casa de Convivencia) se agrega ahí, no se lo inventa acá
public enum SocialRecordStatus
{
    Active,
    Inactive
}
