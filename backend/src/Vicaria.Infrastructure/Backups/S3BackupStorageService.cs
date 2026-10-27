using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Vicaria.Application.Backups;

namespace Vicaria.Infrastructure.Backups;

// Almacenamiento externo compatible con S3 (AWS S3, MinIO o similar) sobre la API
// REST de S3 con firma AWS Signature Version 4. No requiere paquetes adicionales.
public class S3BackupStorageService : IBackupStorageService
{
    private readonly HttpClient _httpClient;
    private readonly S3BackupOptions _options;

    public S3BackupStorageService(HttpClient httpClient, IOptions<S3BackupOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<StoredBackupFile> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var key = $"backups/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";

        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        using var requestContent = new ByteArrayContent(bytes);
        requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var request = BuildRequest(HttpMethod.Put, key, requestContent, bytes);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return new StoredBackupFile(key, bytes.Length);
    }

    public async Task<Stream?> DownloadAsync(string externalKey, CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(HttpMethod.Get, externalKey, null, []);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string externalKey, CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(HttpMethod.Delete, externalKey, null, []);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string key, HttpContent? content, byte[] payload)
    {
        var serviceUri = new Uri(_options.ServiceUrl.TrimEnd('/') + '/');
        Uri uri = _options.UsePathStyle
            ? new Uri(serviceUri, $"{_options.Bucket}/{key}")
            : new Uri($"{serviceUri.Scheme}://{_options.Bucket}.{serviceUri.Host}{(serviceUri.IsDefaultPort ? string.Empty : $":{serviceUri.Port}")}/{key}");

        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var payloadHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var request = new HttpRequestMessage(method, uri);
        if (content is not null)
        {
            request.Content = content;
        }

        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);

        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalHeaders = $"host:{uri.Host}\n" +
                               $"x-amz-content-sha256:{payloadHash}\n" +
                               $"x-amz-date:{amzDate}\n";

        var canonicalRequest = $"{method.Method}\n" +
                               $"{uri.AbsolutePath}\n" +
                               $"{uri.Query.TrimStart('?')}\n" +
                               $"{canonicalHeaders}\n" +
                               $"{signedHeaders}\n" +
                               $"{payloadHash}";

        var credentialScope = $"{dateStamp}/{_options.Region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n" +
                           Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))).ToLowerInvariant();

        var signingKey = GetSignatureKey(_options.SecretKey, dateStamp, _options.Region, "s3");
        var signature = Convert.ToHexString(HmacSha256(signingKey, stringToSign)).ToLowerInvariant();

        request.Headers.TryAddWithoutValidation("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_options.AccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");

        return request;
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string region, string service)
    {
        var key = Encoding.UTF8.GetBytes("AWS4" + secretKey);
        key = HmacSha256(key, dateStamp);
        key = HmacSha256(key, region);
        key = HmacSha256(key, service);
        key = HmacSha256(key, "aws4_request");
        return key;
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
