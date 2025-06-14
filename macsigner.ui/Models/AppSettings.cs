namespace MacSigner.Models
{
    public class AppSettings
    {
        public string? AzureTenantId { get; set; }
        public string? AzureClientId { get; set; }
        public string? AzureClientSecret { get; set; }
        public string? TrustedSigningEndpoint { get; set; }
        public string? CertificateProfileName { get; set; }
        public string? LastSelectedPath { get; set; }
        public bool AutoSelectSignableFiles { get; set; } = true;
        public bool ShowHiddenFiles { get; set; } = false;
        public int MaxConcurrentSigningRequests { get; set; } = 5;

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(AzureTenantId) &&
                   !string.IsNullOrWhiteSpace(AzureClientId) &&
                   !string.IsNullOrWhiteSpace(AzureClientSecret) &&
                   !string.IsNullOrWhiteSpace(TrustedSigningEndpoint) &&
                   !string.IsNullOrWhiteSpace(CertificateProfileName);
        }
    }
}
