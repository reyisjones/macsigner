using System;
using System.Threading.Tasks;
using MacSigner.Models;

namespace MacSigner.Services
{
    public interface ITrustedSigningService
    {
        Task<bool> AuthenticateAsync();
        Task<string> SubmitSigningRequestAsync(SigningRequest request);
        Task<SigningStatus> GetSigningStatusAsync(string requestId);
        Task<byte[]?> DownloadSignedFileAsync(string requestId, string fileName);
        Task CancelSigningRequestAsync(string requestId);
        bool IsAuthenticated { get; }
    }
}
