using System;
using System.IO;

namespace MacSigner.Models
{
    public class SignableFile
    {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileName(FilePath);
        public string FileExtension => Path.GetExtension(FilePath);
        public long FileSize { get; set; }
        public SigningStatus Status { get; set; }
        public bool IsSelected { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SignedAt { get; set; }
        public string? SigningRequestId { get; set; }

        public SignableFile(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Status = SigningStatus.NotSigned;
            IsSelected = true;
            
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                FileSize = fileInfo.Length;
            }
        }

        public string GetFormattedFileSize()
        {
            const long kb = 1024;
            const long mb = kb * 1024;
            const long gb = mb * 1024;

            return FileSize switch
            {
                >= gb => $"{FileSize / gb:F1} GB",
                >= mb => $"{FileSize / mb:F1} MB",
                >= kb => $"{FileSize / kb:F1} KB",
                _ => $"{FileSize} bytes"
            };
        }

        public string FormattedFileSize => GetFormattedFileSize();

        public bool IsSignableFileType()
        {
            var extension = FileExtension.ToLowerInvariant();
            return extension switch
            {
                ".exe" or ".dll" or ".msi" or ".cab" or ".ocx" or 
                ".dylib" or ".app" or ".framework" or ".bundle" or
                ".jar" or ".apk" or ".ipa" or ".xap" => true,
                _ => false
            };
        }
    }
}
