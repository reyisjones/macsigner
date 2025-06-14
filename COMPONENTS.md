# MacSigner Component Documentation

This document provides detailed information about each component in the MacSigner application.

## 📁 Project Structure

```
macsigner.ui/
├── Commands/               # CLI command handlers
├── Models/                # Data models and entities
├── Services/              # Business logic and external integrations
├── ViewModels/            # MVVM ViewModels for UI binding
├── Views/                 # UI Views (future expansion)
├── App.axaml/App.axaml.cs # Application entry and configuration
├── MainWindow.axaml/.cs   # Main application window
└── Program.cs             # Application entry point
```

## 🔧 Core Components

### Models

#### AppSettings.cs
**Purpose**: Application configuration management
**Key Properties**:
- `AzureTenantId`: Azure Active Directory tenant identifier
- `AzureClientId`: Service principal application ID
- `AzureClientSecret`: Service principal secret key
- `TrustedSigningEndpoint`: Azure Trusted Signing service endpoint
- `CertificateProfileName`: Certificate profile for signing
- `LastSelectedPath`: Remember last selected directory
- `AutoSelectSignableFiles`: Automatically select detected files
- `ShowHiddenFiles`: Include hidden files in scans
- `MaxConcurrentSigningRequests`: Limit concurrent operations

**Key Methods**:
- `IsConfigured()`: Validates all required settings are present

#### SignableFile.cs
**Purpose**: Represents a file that can be digitally signed
**Key Properties**:
- `FilePath`: Full path to the file
- `FileName`: File name without path
- `FileExtension`: File extension
- `FileSize`: File size in bytes
- `Status`: Current signing status
- `IsSelected`: Whether file is selected for signing
- `ErrorMessage`: Error details if signing fails
- `SignedAt`: Timestamp when signing completed
- `SigningRequestId`: Associated request identifier

**Key Methods**:
- `GetFormattedFileSize()`: Human-readable file size (KB, MB, GB)
- `IsSignableFileType()`: Checks if file extension is supported
- `FormattedFileSize`: Property for UI binding

#### SigningRequest.cs
**Purpose**: Represents a batch signing operation
**Key Properties**:
- `RequestId`: Unique identifier for the request
- `Files`: List of files in the request
- `CreatedAt`: Request creation timestamp
- `Status`: Overall request status
- `ErrorMessage`: Error details if request fails
- `TotalFiles`: Count of files in request
- `ProcessedFiles`: Count of completed files

**Key Methods**:
- `GetProgress()`: Calculate completion percentage
- `Progress`: Property for UI binding

#### SigningStatus.cs
**Purpose**: Enumeration of possible signing states
**Values**:
- `NotSigned`: File hasn't been processed
- `Queued`: File is queued for signing
- `InProgress`: File is currently being signed
- `Completed`: File was signed successfully
- `Failed`: Signing failed
- `Cancelled`: Operation was cancelled

### Services

#### ITrustedSigningService.cs / AzureTrustedSigningService.cs
**Purpose**: Integration with Azure Trusted Signing service
**Key Methods**:
- `AuthenticateAsync()`: Authenticate with Azure using service principal
- `SubmitSigningRequestAsync()`: Submit files for signing
- `GetSigningStatusAsync()`: Check status of signing request
- `DownloadSignedFileAsync()`: Download signed file content
- `CancelSigningRequestAsync()`: Cancel a pending request

**Properties**:
- `IsAuthenticated`: Whether service is currently authenticated

**Implementation Details**:
- Uses `ClientSecretCredential` for Azure authentication
- Implements token refresh logic
- Handles HTTP communication with signing service
- Provides comprehensive error handling and logging

#### IFileService.cs / FileService.cs
**Purpose**: File system operations and validation
**Key Methods**:
- `ScanDirectoryAsync()`: Recursively scan directory for signable files
- `ValidateFileAsync()`: Verify file accessibility and constraints
- `BackupFileAsync()`: Create backup before replacing file
- `ReplaceFileWithSignedVersionAsync()`: Replace original with signed version

**Implementation Details**:
- Maintains list of supported file extensions
- Respects hidden file visibility settings
- Creates timestamped backups
- Validates file sizes and accessibility
- Provides detailed logging

#### ISettingsService.cs / SettingsService.cs
**Purpose**: Configuration persistence and management
**Key Methods**:
- `LoadSettingsAsync()`: Load settings from storage
- `SaveSettingsAsync()`: Persist settings to storage
- `GetSettingsFilePath()`: Get path to settings file

**Implementation Details**:
- Stores settings in JSON format
- Uses platform-appropriate application data directory
- Creates default settings if none exist
- Handles serialization/deserialization errors gracefully

### ViewModels

#### ViewModelBase.cs
**Purpose**: Base class for all ViewModels
**Features**:
- Implements `INotifyPropertyChanged`
- Provides `OnPropertyChanged()` method
- Includes `SetProperty()` helper for property setters
- Supports `[CallerMemberName]` attribute for automatic property names

#### MainWindowViewModel.cs
**Purpose**: Main window UI logic and data binding
**Key Properties**:
- `SignableFiles`: Observable collection of detected files
- `SigningRequests`: Observable collection of signing operations
- `SelectedPath`: Currently selected directory path
- `IsScanning`: Whether directory scan is in progress
- `IsSigning`: Whether signing operation is in progress
- `StatusMessage`: Current status text for UI
- `ProgressPercentage`: Progress value for progress bars
- `CanScan`: Whether scan operation is allowed
- `CanSign`: Whether sign operation is allowed
- `TotalFiles`: Count of detected files
- `SelectedFiles`: Count of selected files

**Commands**:
- `BrowseCommand`: Open directory browser
- `ScanCommand`: Scan selected directory
- `SignCommand`: Sign selected files
- `OpenSettingsCommand`: Open settings dialog
- `RefreshCommand`: Refresh file list

**Key Methods**:
- `LoadSettingsAsync()`: Initialize application settings
- `BrowseForDirectory()`: Handle directory selection
- `ScanDirectory()`: Perform file scanning
- `SignSelectedFiles()`: Execute signing operation
- `MonitorSigningProgress()`: Track signing progress
- `RefreshFiles()`: Update file list

### Commands (CLI)

#### SignCommand.cs
**Purpose**: Command-line interface for signing operations
**Options**:
- `--path`: Target directory or file (required)
- `--tenant-id`: Azure tenant ID
- `--client-id`: Azure client ID
- `--client-secret`: Azure client secret
- `--endpoint`: Trusted Signing endpoint
- `--profile`: Certificate profile name
- `--recursive`: Scan directories recursively
- `--verbose`: Enable detailed output

**Features**:
- Validates all required parameters
- Supports both file and directory targets
- Provides real-time progress updates
- Creates detailed success/failure reports
- Handles timeouts and cancellation
- Comprehensive error handling with user-friendly messages

### UI Components

#### App.axaml / App.axaml.cs
**Purpose**: Application configuration and dependency injection
**Features**:
- Configures Fluent theme
- Sets up dependency injection container
- Registers all services and ViewModels
- Handles application lifecycle
- Configures logging

#### MainWindow.axaml / MainWindow.axaml.cs
**Purpose**: Main application window UI
**Sections**:
- **Header**: Path selection, scan controls, file summary
- **Tabs**: Files list and signing requests
- **Status Bar**: Status messages and progress indicators

**Features**:
- Responsive layout with proper sizing
- Data binding to ViewModel properties
- Status indicators with color coding
- Progress bars for operations
- Tabbed interface for different views
- Clean, modern Fluent Design styling

#### Program.cs
**Purpose**: Application entry point
**Features**:
- Detects CLI vs GUI mode based on arguments
- Configures CLI command structure
- Handles version command
- Initializes Avalonia application for GUI mode

## 🔄 Data Flow

### GUI Mode Flow
1. **Startup**: `Program.cs` → `App.axaml.cs` → `MainWindow`
2. **Directory Selection**: User input → `MainWindowViewModel.BrowseCommand`
3. **File Scanning**: `ScanCommand` → `FileService.ScanDirectoryAsync`
4. **File Display**: `SignableFiles` collection → UI binding
5. **Signing**: `SignCommand` → `TrustedSigningService.SubmitSigningRequestAsync`
6. **Progress Monitoring**: Background task → UI updates
7. **Completion**: File replacement → Status updates

### CLI Mode Flow
1. **Startup**: `Program.cs` → `RunCliMode`
2. **Command Parsing**: `System.CommandLine` → `SignCommand`
3. **Configuration**: Settings service → Azure service
4. **File Processing**: File service → Signing service
5. **Progress Display**: Console output → Status updates
6. **Completion**: File replacement → Exit code

## 🔌 Dependencies

### Core Dependencies
- **Avalonia**: Cross-platform UI framework
- **Avalonia.Themes.Fluent**: Modern UI theme
- **ReactiveUI**: MVVM framework
- **System.CommandLine**: CLI framework
- **Azure.Identity**: Azure authentication
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Logging**: Logging framework
- **Microsoft.Extensions.Http**: HTTP client factory
- **Newtonsoft.Json**: JSON serialization

### Development Dependencies
- **XamlNameReferenceGenerator**: XAML code generation
- **Avalonia.Diagnostics**: Development tools (Debug only)

## 🚀 Extension Points

### Adding New File Types
1. Update `FileService._signableExtensions` HashSet
2. Add detection logic in `SignableFile.IsSignableFileType()`
3. Update documentation

### Adding New Settings
1. Add properties to `AppSettings` model
2. Update `IsConfigured()` validation if required
3. Add UI controls for new settings
4. Update CLI options if applicable

### Adding New Commands
1. Create new command class inheriting from `Command`
2. Add to `Program.RunCliMode()` method
3. Implement command logic and options

### Adding New Services
1. Define interface in `Services/` folder
2. Implement service class
3. Register in `App.axaml.cs` dependency injection
4. Inject into ViewModels or other services as needed

## 🧪 Testing Strategy

### Unit Testing Areas
- **Models**: Property validation, calculations
- **Services**: File operations, API interactions
- **ViewModels**: Command logic, property changes
- **CLI Commands**: Parameter validation, execution logic

### Integration Testing Areas
- **File Service**: Actual file system operations
- **Settings Service**: Configuration persistence
- **UI Integration**: ViewModel to View binding

### Manual Testing Areas
- **UI Responsiveness**: All controls work as expected
- **Error Handling**: Graceful degradation
- **Performance**: Large directory scanning
- **Cross-Platform**: macOS, Windows, Linux compatibility

This component documentation provides a comprehensive overview of the MacSigner architecture and implementation details for development and maintenance purposes.
