namespace Vicaria.Application.Backups;

public record BackupFileStreamDto(Stream Stream, string ContentType, string FileName);

public enum GetBackupFileError
{
    BackupNotFound,
    BackupNotAvailable,
    FileNotFoundInStorage
}

public class GetBackupFileResult
{
    public bool Success { get; init; }
    public BackupFileStreamDto? FileStream { get; init; }
    public GetBackupFileError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetBackupFileResult Ok(BackupFileStreamDto fileStream) =>
        new() { Success = true, FileStream = fileStream };

    public static GetBackupFileResult BackupNotFound() =>
        new() { Success = false, Error = GetBackupFileError.BackupNotFound, ErrorMessage = "El respaldo indicado no existe." };

    public static GetBackupFileResult BackupNotAvailable() =>
        new() { Success = false, Error = GetBackupFileError.BackupNotAvailable, ErrorMessage = "El respaldo no está disponible para descargar porque su generación falló." };

    public static GetBackupFileResult FileNotFoundInStorage() =>
        new() { Success = false, Error = GetBackupFileError.FileNotFoundInStorage, ErrorMessage = "El archivo físico del respaldo no fue encontrado en el almacenamiento." };
}
