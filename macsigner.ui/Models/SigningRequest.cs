using System;
using System.Collections.Generic;
using System.Linq;

namespace MacSigner.Models
{
    public class SigningRequest
    {
        public string RequestId { get; set; }
        public List<SignableFile> Files { get; set; }
        public DateTime CreatedAt { get; set; }
        public SigningStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalFiles => Files.Count;
        public int ProcessedFiles => Files.Count(f => f.Status == SigningStatus.Completed || f.Status == SigningStatus.Failed);

        public SigningRequest()
        {
            RequestId = Guid.NewGuid().ToString();
            Files = new List<SignableFile>();
            CreatedAt = DateTime.UtcNow;
            Status = SigningStatus.NotSigned;
        }

        public double GetProgress()
        {
            if (TotalFiles == 0) return 0;
            return (double)ProcessedFiles / TotalFiles * 100;
        }

        public double Progress => GetProgress();
    }
}
