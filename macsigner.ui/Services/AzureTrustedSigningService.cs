using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using MacSigner.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MacSigner.Services
{
    public class AzureTrustedSigningService : ITrustedSigningService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private readonly ILogger<AzureTrustedSigningService> _logger;
        private string? _accessToken;
        private DateTime _tokenExpiry;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

        public AzureTrustedSigningService(HttpClient httpClient, AppSettings settings, ILogger<AzureTrustedSigningService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                _logger.LogInformation("Authenticating with Azure Trusted Signing service...");

                if (!_settings.IsConfigured())
                {
                    _logger.LogError("Azure Trusted Signing service is not properly configured");
                    return false;
                }

                var credential = new ClientSecretCredential(
                    _settings.AzureTenantId,
                    _settings.AzureClientId,
                    _settings.AzureClientSecret);

                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://codesigning.azure.net/.default" });

                var token = await credential.GetTokenAsync(tokenRequestContext);
                _accessToken = token.Token;
                _tokenExpiry = token.ExpiresOn.UtcDateTime;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                _logger.LogInformation("Successfully authenticated with Azure Trusted Signing service");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate with Azure Trusted Signing service");
                return false;
            }
        }

        public async Task<string> SubmitSigningRequestAsync(SigningRequest request)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    var authResult = await AuthenticateAsync();
                    if (!authResult)
                    {
                        throw new InvalidOperationException("Failed to authenticate with Azure Trusted Signing service");
                    }
                }

                _logger.LogInformation($"Submitting signing request for {request.Files.Count} files");

                // Create the signing request payload
                var payload = new
                {
                    certificateProfileName = _settings.CertificateProfileName,
                    files = request.Files.Select(f => new
                    {
                        fileName = f.FileName,
                        filePath = f.FilePath,
                        fileSize = f.FileSize
                    }).ToArray(),
                    requestId = request.RequestId
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_settings.TrustedSigningEndpoint}/sign", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                _logger.LogInformation($"Successfully submitted signing request: {request.RequestId}");
                return request.RequestId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to submit signing request: {request.RequestId}");
                throw;
            }
        }

        public async Task<SigningStatus> GetSigningStatusAsync(string requestId)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    var authResult = await AuthenticateAsync();
                    if (!authResult)
                    {
                        throw new InvalidOperationException("Failed to authenticate with Azure Trusted Signing service");
                    }
                }

                var response = await _httpClient.GetAsync($"{_settings.TrustedSigningEndpoint}/status/{requestId}");
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                var status = result?.status?.ToString() ?? "unknown";
                return status.ToLowerInvariant() switch
                {
                    "pending" => SigningStatus.Queued,
                    "in-progress" => SigningStatus.InProgress,
                    "completed" => SigningStatus.Completed,
                    "failed" => SigningStatus.Failed,
                    "cancelled" => SigningStatus.Cancelled,
                    _ => SigningStatus.NotSigned
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get signing status for request: {requestId}");
                return SigningStatus.Failed;
            }
        }

        public async Task<byte[]?> DownloadSignedFileAsync(string requestId, string fileName)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    var authResult = await AuthenticateAsync();
                    if (!authResult)
                    {
                        throw new InvalidOperationException("Failed to authenticate with Azure Trusted Signing service");
                    }
                }

                var response = await _httpClient.GetAsync($"{_settings.TrustedSigningEndpoint}/download/{requestId}/{fileName}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download signed file: {fileName} for request: {requestId}");
                return null;
            }
        }

        public async Task CancelSigningRequestAsync(string requestId)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    var authResult = await AuthenticateAsync();
                    if (!authResult)
                    {
                        throw new InvalidOperationException("Failed to authenticate with Azure Trusted Signing service");
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_settings.TrustedSigningEndpoint}/cancel/{requestId}");
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Successfully cancelled signing request: {requestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to cancel signing request: {requestId}");
                throw;
            }
        }
    }
}
