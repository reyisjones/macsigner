using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MacSigner.Models;
using MacSigner.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace MacSigner.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly ITrustedSigningService _trustedSigningService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<MainWindowViewModel> _logger;

        private string _selectedPath = string.Empty;
        private bool _isScanning = false;
        private bool _isSigning = false;
        private string _statusMessage = "Ready";
        private double _progressPercentage = 0;
        private AppSettings _settings;

        public MainWindowViewModel(
            IFileService fileService,
            ITrustedSigningService trustedSigningService,
            ISettingsService settingsService,
            ILogger<MainWindowViewModel> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _trustedSigningService = trustedSigningService ?? throw new ArgumentNullException(nameof(trustedSigningService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SignableFiles = new ObservableCollection<SignableFile>();
            SigningRequests = new ObservableCollection<SigningRequest>();
            _settings = new AppSettings();

            // Initialize commands with proper schedulers
            BrowseCommand = ReactiveCommand.CreateFromTask(BrowseForDirectory);
            PastePathCommand = ReactiveCommand.CreateFromTask(PastePathAndOpenInFinder);
            ScanCommand = ReactiveCommand.CreateFromTask(ScanDirectory, 
                this.WhenAnyValue(x => x.CanScan).ObserveOn(RxApp.MainThreadScheduler));
            SignCommand = ReactiveCommand.CreateFromTask(SignSelectedFiles, 
                this.WhenAnyValue(x => x.CanSign).ObserveOn(RxApp.MainThreadScheduler));
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshFiles);

            // Load settings synchronously during initialization to avoid threading issues
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _settingsService.LoadSettingsAsync();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _settings = settings;
                        if (!string.IsNullOrEmpty(settings.LastSelectedPath))
                        {
                            SelectedPath = settings.LastSelectedPath;
                        }
                        StatusMessage = settings.IsConfigured() ? "Ready" : "Please configure Azure settings";
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load settings");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = "Failed to load settings";
                    });
                }
            });
        }

        public ObservableCollection<SignableFile> SignableFiles { get; }
        public ObservableCollection<SigningRequest> SigningRequests { get; }

        public string SelectedPath
        {
            get => _selectedPath;
            set 
            { 
                if (SetProperty(ref _selectedPath, value))
                {
                    // Notify dependent properties that may have changed
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(CanSign));
                    
                    // Clear files when path changes to avoid confusion
                    if (SignableFiles.Any())
                    {
                        SignableFiles.Clear();
                        OnPropertyChanged(nameof(TotalFiles));
                        OnPropertyChanged(nameof(SelectedFiles));
                        StatusMessage = "Path changed. Click Refresh to scan for files.";
                    }
                }
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set 
            { 
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(CanSign));
                }
            }
        }

        public bool IsSigning
        {
            get => _isSigning;
            set 
            { 
                if (SetProperty(ref _isSigning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(CanSign));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public bool CanScan => !IsScanning && !IsSigning && IsValidDirectory(SelectedPath);
        public bool CanSign => !IsSigning && !IsScanning && SignableFiles.Any(f => f.IsSelected) && _settings.IsConfigured();

        public int TotalFiles => SignableFiles.Count;
        public int SelectedFiles => SignableFiles.Count(f => f.IsSelected);

        public ICommand BrowseCommand { get; }
        public ICommand PastePathCommand { get; }
        public ICommand ScanCommand { get; }
        public ICommand SignCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task BrowseForDirectory()
        {
            try
            {
                // TODO: Implement directory picker dialog
                // For now, this is a placeholder
                StatusMessage = "Browse functionality to be implemented with platform-specific dialog";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to browse for directory");
                StatusMessage = "Failed to open directory browser";
            }
        }

        private async Task ScanDirectory()
        {
            // Validate path before scanning
            var validationResult = ValidateSelectedPath();
            if (!validationResult.IsValid)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = validationResult.ErrorMessage;
                });
                return;
            }

            // Ensure UI thread updates for scanning state
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsScanning = true;
                StatusMessage = "Scanning directory...";
                ProgressPercentage = 0;
            });

            try
            {
                // Clear existing files on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SignableFiles.Clear();
                });

                _logger.LogInformation($"Starting directory scan: {SelectedPath}");
                var files = await _fileService.ScanDirectoryAsync(SelectedPath, includeSubdirectories: true);
                _logger.LogInformation($"Directory scan completed. Found {files.Count} signable files");

                // Add files to collection on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var file in files)
                    {
                        SignableFiles.Add(file);
                    }

                    StatusMessage = files.Count > 0 
                        ? $"Found {files.Count} signable file{(files.Count == 1 ? "" : "s")}"
                        : "No signable files found in directory";
                    
                    // Notify property changes
                    OnPropertyChanged(nameof(TotalFiles));
                    OnPropertyChanged(nameof(SelectedFiles));
                    OnPropertyChanged(nameof(CanSign));
                });

                // Save last selected path
                try
                {
                    _settings.LastSelectedPath = SelectedPath;
                    await _settingsService.SaveSettingsAsync(_settings);
                    _logger.LogDebug($"Saved last selected path: {SelectedPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save last selected path to settings");
                    // Don't fail the entire operation for this
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, $"Directory not found: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Directory not found or has been deleted";
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"Access denied to directory: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Access denied to directory. Please check permissions.";
                });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"IO error scanning directory: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "IO error occurred while scanning directory";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during directory scan");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Failed to scan directory due to unexpected error";
                });
            }
            finally
            {
                // Ensure scanning state is reset on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsScanning = false;
                    ProgressPercentage = 0;
                });
            }
        }

        private async Task SignSelectedFiles()
        {
            var selectedFiles = SignableFiles.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                StatusMessage = "No files selected for signing";
                return;
            }

            if (!_settings.IsConfigured())
            {
                StatusMessage = "Please configure Azure settings first";
                return;
            }

            IsSigning = true;
            StatusMessage = "Authenticating with Azure...";
            ProgressPercentage = 0;

            try
            {
                // Authenticate
                var authenticated = await _trustedSigningService.AuthenticateAsync();
                if (!authenticated)
                {
                    StatusMessage = "Failed to authenticate with Azure";
                    return;
                }

                // Create signing request
                var signingRequest = new SigningRequest();
                signingRequest.Files.AddRange(selectedFiles);
                SigningRequests.Add(signingRequest);

                StatusMessage = $"Submitting {selectedFiles.Count} files for signing...";

                // Submit request
                var requestId = await _trustedSigningService.SubmitSigningRequestAsync(signingRequest);
                
                // Update file statuses
                foreach (var file in selectedFiles)
                {
                    file.Status = SigningStatus.Queued;
                    file.SigningRequestId = requestId;
                }

                StatusMessage = $"Signing request submitted. Request ID: {requestId}";
                
                // Start monitoring progress
                _ = Task.Run(() => MonitorSigningProgress(requestId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sign files");
                StatusMessage = "Failed to sign files";
            }
            finally
            {
                IsSigning = false;
            }
        }

        private async Task MonitorSigningProgress(string requestId)
        {
            try
            {
                var request = SigningRequests.FirstOrDefault(r => r.RequestId == requestId);
                if (request == null) return;

                while (request.Status == SigningStatus.Queued || request.Status == SigningStatus.InProgress)
                {
                    await Task.Delay(5000); // Poll every 5 seconds

                    var status = await _trustedSigningService.GetSigningStatusAsync(requestId);
                    request.Status = status;

                    ProgressPercentage = request.GetProgress();
                    StatusMessage = $"Signing in progress... {ProgressPercentage:F1}%";

                    if (status == SigningStatus.Completed)
                    {
                        StatusMessage = "Signing completed successfully";
                        foreach (var file in request.Files)
                        {
                            file.Status = SigningStatus.Completed;
                            file.SignedAt = DateTime.UtcNow;
                        }
                        break;
                    }
                    else if (status == SigningStatus.Failed)
                    {
                        StatusMessage = "Signing failed";
                        foreach (var file in request.Files)
                        {
                            file.Status = SigningStatus.Failed;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to monitor signing progress for request: {requestId}");
            }
        }

        private async Task RefreshFiles()
        {
            // Enhanced validation with detailed feedback
            var validationResult = ValidateSelectedPath();
            if (!validationResult.IsValid)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = validationResult.ErrorMessage;
                });
                _logger.LogInformation($"Refresh validation failed: {validationResult.ErrorMessage}");
                return;
            }

            // Check if already scanning to prevent duplicate operations
            if (IsScanning)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Directory scan already in progress";
                });
                _logger.LogInformation("Refresh attempted while already scanning");
                return;
            }

            try
            {
                _logger.LogInformation($"Starting refresh for directory: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Refreshing file list...";
                });
                
                await ScanDirectory();
                
                _logger.LogInformation($"Refresh completed successfully. Found {SignableFiles.Count} files");
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, $"Directory not found during refresh: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Directory no longer exists. Please select a valid directory.";
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"Access denied to directory: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Access denied to directory. Please check permissions.";
                });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"IO error during refresh: {SelectedPath}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "IO error occurred while accessing directory.";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during refresh");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "An unexpected error occurred during refresh.";
                });
            }
        }

        private void OpenSettings()
        {
            // TODO: Open settings window
            StatusMessage = "Settings window to be implemented";
        }

        private ValidationResult ValidateSelectedPath()
        {
            // Check for null or empty path
            if (string.IsNullOrEmpty(SelectedPath))
            {
                return new ValidationResult(false, "Please select a directory first");
            }

            // Check for whitespace-only path
            if (string.IsNullOrWhiteSpace(SelectedPath))
            {
                return new ValidationResult(false, "Directory path cannot be empty or contain only whitespace");
            }

            // Trim the path to handle potential leading/trailing spaces
            var trimmedPath = SelectedPath.Trim();
            if (string.IsNullOrEmpty(trimmedPath))
            {
                return new ValidationResult(false, "Directory path cannot be empty after trimming whitespace");
            }

            // Check for invalid path characters
            try
            {
                var invalidChars = Path.GetInvalidPathChars();
                if (trimmedPath.IndexOfAny(invalidChars) >= 0)
                {
                    return new ValidationResult(false, "Directory path contains invalid characters");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error checking path characters for: {trimmedPath}");
                return new ValidationResult(false, "Invalid directory path format");
            }

            // Check if directory exists
            if (!Directory.Exists(trimmedPath))
            {
                return new ValidationResult(false, "Selected directory does not exist or is not accessible");
            }

            // Check if we can access the directory
            try
            {
                var testAccess = Directory.GetDirectories(trimmedPath);
                // If we can enumerate directories, we have access
            }
            catch (UnauthorizedAccessException)
            {
                return new ValidationResult(false, "Access denied to selected directory");
            }
            catch (DirectoryNotFoundException)
            {
                return new ValidationResult(false, "Directory not found or has been deleted");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error accessing directory: {trimmedPath}");
                return new ValidationResult(false, "Cannot access selected directory");
            }

            return new ValidationResult(true, "Directory is valid");
        }

        private bool IsValidDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var trimmedPath = path.Trim();
                if (string.IsNullOrEmpty(trimmedPath))
                    return false;

                // Quick validation without detailed error messages
                var invalidChars = Path.GetInvalidPathChars();
                if (trimmedPath.IndexOfAny(invalidChars) >= 0)
                    return false;

                return Directory.Exists(trimmedPath);
            }
            catch
            {
                return false;
            }
        }

        private class ValidationResult
        {
            public bool IsValid { get; }
            public string ErrorMessage { get; }

            public ValidationResult(bool isValid, string errorMessage)
            {
                IsValid = isValid;
                ErrorMessage = errorMessage;
            }
        }

        private async Task PastePathAndOpenInFinder()
        {
            try
            {
                // Get text from clipboard
                var clipboardText = await GetClipboardTextAsync();
                
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    StatusMessage = "No text found in clipboard";
                    return;
                }

                // Trim and clean the path
                var filePath = clipboardText.Trim().Trim('"');
                
                // Validate that the path exists
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    StatusMessage = $"Path does not exist: {filePath}";
                    return;
                }

                // If it's a file, get the directory
                string directoryPath;
                if (File.Exists(filePath))
                {
                    directoryPath = Path.GetDirectoryName(filePath) ?? filePath;
                }
                else
                {
                    directoryPath = filePath;
                }

                // Update the selected path
                SelectedPath = directoryPath;
                
                // Open Finder/Explorer to the location
                await OpenInFileManagerAsync(filePath);
                
                StatusMessage = $"Opened path in Finder: {filePath}";
                _logger.LogInformation($"Successfully opened path in Finder: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to paste path and open in Finder");
                StatusMessage = "Failed to paste path or open in Finder";
            }
        }

        private async Task<string> GetClipboardTextAsync()
        {
            try
            {
                // Simple approach: Try to get clipboard from main window
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow?.Clipboard != null)
                    {
                        return await mainWindow.Clipboard.GetTextAsync() ?? string.Empty;
                    }
                }
                
                // Fallback: try to use system clipboard via System.Windows.Forms (if available)
                // For now, return empty string if Avalonia clipboard is not accessible
                _logger.LogWarning("Could not access clipboard through Avalonia");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get clipboard text");
                return string.Empty;
            }
        }

        private async Task OpenInFileManagerAsync(string path)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        // Open in Finder on macOS
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "open",
                                Arguments = File.Exists(path) ? $"-R \"{path}\"" : $"\"{path}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        // Open in Explorer on Windows
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = File.Exists(path) ? $"/select,\"{path}\"" : $"\"{path}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        // Open in file manager on Linux
                        var directoryPath = File.Exists(path) ? Path.GetDirectoryName(path) : path;
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "xdg-open",
                                Arguments = $"\"{directoryPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open path in file manager");
                throw;
            }
        }
    }
}
