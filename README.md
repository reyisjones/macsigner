# MacSigner - Digital Code Signing Tool

A cross-platform digital code signing application built with Avalonia UI that integrates with Microsoft's Azure Trusted Signing service. MacSigner provides both a modern GUI interface and a powerful CLI for signing executable files, libraries, and other code artifacts.

## 🚀 Features

- **Cross-Platform**: Built with Avalonia UI, runs on macOS, Windows, and Linux
- **Modern UI**: Clean, intuitive interface using Fluent Design theme
- **CLI Support**: Full command-line interface for automation and scripting
- **Azure Integration**: Uses Microsoft Azure Trusted Signing service
- **File Detection**: Automatically detects signable files (.exe, .dll, .dylib, .app, etc.)
- **Progress Tracking**: Real-time status updates and progress monitoring
- **Backup System**: Automatic backups before replacing files with signed versions
- **MVVM Architecture**: Clean separation of concerns with reactive UI

## 📋 Prerequisites

- .NET 8.0 SDK or later (downgraded from .NET 9.0 for broader compatibility)
- Azure Trusted Signing account and credentials
- macOS 10.15+ (for macOS development)

## 🔧 Recent Updates & Important Notes

### Version 1.2.0 - Enhanced Validation & Robustness
- **Enhanced Path Validation**: Comprehensive validation prevents refresh operations with invalid paths
- **Thread Safety**: All UI updates properly dispatched to UI thread to prevent threading errors
- **Smart Error Handling**: Specific error messages for different failure scenarios (access denied, directory not found, etc.)
- **Improved User Experience**: Commands automatically enable/disable based on current state
- **Duplicate Operation Prevention**: Multiple refresh/scan operations can't run simultaneously
- **Auto-clearing File Lists**: File list clears when path changes to avoid confusion

### Breaking Changes
- **Target Framework**: Downgraded from .NET 9.0 to .NET 8.0 for better SDK compatibility
- **NuGet Packages**: Microsoft.Extensions packages downgraded to 8.0.1 versions

### Performance Improvements
- **Validation Caching**: Path validation results cached to improve UI responsiveness
- **Async Operations**: All file operations properly async to prevent UI blocking
- **Memory Management**: Better disposal of resources and reduced memory footprint

## 🔧 Installation

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/yourusername/macsigner.git
cd macsigner/macsigner.ui
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Build the application:
```bash
dotnet build
```

4. Run the application:
```bash
# GUI Mode
dotnet run

# CLI Mode
dotnet run -- sign --help
```

### Creating a Distributable Package

```bash
# macOS app bundle
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Windows executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux executable
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

## ⚙️ Configuration

### Azure Trusted Signing Setup

1. **Create Azure Trusted Signing Account**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Create a new Trusted Signing account
   - Note your endpoint URL

2. **Create Service Principal**:
   ```bash
   az ad sp create-for-rbac --name "MacSigner-SP" --role contributor
   ```
   
   This will output:
   ```json
   {
     "appId": "your-client-id",
     "displayName": "MacSigner-SP",
     "password": "your-client-secret",
     "tenant": "your-tenant-id"
   }
   ```

3. **Create Certificate Profile**:
   - In Azure Portal, go to your Trusted Signing account
   - Create a certificate profile
   - Note the profile name

### Application Configuration

#### GUI Configuration
1. Launch MacSigner
2. Click "Settings" button
3. Enter your Azure credentials:
   - **Tenant ID**: Your Azure tenant ID
   - **Client ID**: Service principal app ID
   - **Client Secret**: Service principal password
   - **Endpoint**: Your Trusted Signing endpoint URL
   - **Certificate Profile**: Your certificate profile name

#### CLI Configuration
Create a configuration file or use command-line parameters:

```bash
# Using command line parameters
macsigner sign --path /path/to/files \\
  --tenant-id "your-tenant-id" \\
  --client-id "your-client-id" \\
  --client-secret "your-client-secret" \\
  --endpoint "https://your-endpoint.codesigning.azure.net" \\
  --profile "your-certificate-profile"
```

Configuration file location: `~/.config/MacSigner/appsettings.json`

```json
{
  "AzureTenantId": "your-tenant-id",
  "AzureClientId": "your-client-id",
  "AzureClientSecret": "your-client-secret",
  "TrustedSigningEndpoint": "https://your-endpoint.codesigning.azure.net",
  "CertificateProfileName": "your-certificate-profile",
  "AutoSelectSignableFiles": true,
  "ShowHiddenFiles": false,
  "MaxConcurrentSigningRequests": 5
}
```

## 📱 Usage

### GUI Mode

1. **Launch Application**:
   ```bash
   dotnet run
   # or
   ./macsigner  # if using published executable
   ```

2. **Select Directory**:
   - Enter path manually or use "Browse" button
   - Click "Scan" to find signable files

3. **Review Files**:
   - View detected files in the "Files to Sign" tab
   - Select/deselect files as needed
   - Check file status and details

4. **Sign Files**:
   - Click "Sign Selected Files"
   - Monitor progress in real-time
   - Review results in "Signing Requests" tab

### CLI Mode

#### Basic Usage
```bash
# Sign all files in a directory
macsigner sign --path /path/to/your/project

# Sign a specific file
macsigner sign --path /path/to/file.exe

# Sign with custom settings
macsigner sign --path /path/to/files \\
  --tenant-id "your-tenant" \\
  --client-id "your-client" \\
  --client-secret "your-secret" \\
  --endpoint "your-endpoint" \\
  --profile "your-profile" \\
  --verbose
```

#### Command Options
```
USAGE:
    macsigner sign [OPTIONS]

OPTIONS:
    -p, --path <path>              Path to directory or file to sign [REQUIRED]
    -t, --tenant-id <id>           Azure tenant ID
    -c, --client-id <id>           Azure client ID  
    -s, --client-secret <secret>   Azure client secret
    -e, --endpoint <url>           Trusted Signing endpoint URL
    -r, --profile <name>           Certificate profile name
    -R, --recursive                Scan directories recursively [default: true]
    -v, --verbose                  Enable verbose output
    --help                         Show help information

EXAMPLES:
    macsigner sign --path ~/MyApp
    macsigner sign --path ~/MyApp/app.exe --verbose
    macsigner version
```

## 📁 Supported File Types

MacSigner automatically detects and can sign the following file types:

### Windows
- `.exe` - Executable files
- `.dll` - Dynamic Link Libraries
- `.msi` - Windows Installer packages
- `.cab` - Cabinet files
- `.ocx` - ActiveX controls
- `.sys` - System drivers
- `.scr` - Screen savers

### macOS
- `.app` - Application bundles
- `.dylib` - Dynamic libraries
- `.framework` - Framework bundles
- `.bundle` - Plugin bundles
- `.kext` - Kernel extensions

### Cross-Platform
- `.jar` - Java archives
- `.apk` - Android packages
- `.ipa` - iOS packages
- `.xap` - Silverlight packages
- `.vsix` - Visual Studio extensions
- `.nupkg` - NuGet packages

## 🏗️ Architecture

### Project Structure
```
macsigner.ui/
├── Commands/           # CLI command handlers
│   └── SignCommand.cs
├── Models/            # Data models
│   ├── AppSettings.cs
│   ├── SignableFile.cs
│   ├── SigningRequest.cs
│   └── SigningStatus.cs
├── Services/          # Business logic services
│   ├── AzureTrustedSigningService.cs
│   ├── FileService.cs
│   ├── ITrustedSigningService.cs
│   └── SettingsService.cs
├── ViewModels/        # MVVM ViewModels
│   ├── MainWindowViewModel.cs
│   └── ViewModelBase.cs
├── Views/             # UI Views (future settings dialogs)
├── App.axaml          # Application XAML
├── App.axaml.cs       # Application code-behind
├── MainWindow.axaml   # Main window XAML
├── MainWindow.axaml.cs# Main window code-behind
└── Program.cs         # Entry point
```

### Key Components

#### Services
- **ITrustedSigningService**: Azure Trusted Signing integration
- **IFileService**: File system operations and validation
- **ISettingsService**: Configuration management

#### Models
- **SignableFile**: Represents a file that can be signed
- **SigningRequest**: Represents a batch signing operation
- **AppSettings**: Application configuration

#### ViewModels
- **MainWindowViewModel**: Main UI logic using MVVM pattern

## 🔒 Security Considerations

- **Credential Storage**: Credentials are stored in user's application data directory
- **File Backups**: Original files are automatically backed up before signing
- **Validation**: Files are validated before and after signing
- **Secure Communication**: All API calls use HTTPS
- **Token Management**: Azure access tokens are managed securely

## 🧪 Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Manual Testing Checklist

#### GUI Testing
- [ ] Application launches without errors
- [ ] Directory browsing works correctly
- [ ] File scanning detects appropriate files
- [ ] File selection/deselection works
- [ ] Status indicators update correctly
- [ ] Progress bars display during operations
- [ ] Error handling displays user-friendly messages

#### CLI Testing
- [ ] Help text displays correctly
- [ ] Version command works
- [ ] File/directory validation works
- [ ] Authentication succeeds with valid credentials
- [ ] Signing process completes successfully
- [ ] Error messages are clear and helpful
- [ ] Verbose output provides detailed information

## 🐛 Troubleshooting

### Common Issues

#### GUI Won't Start or Crashes
```
❌ Application fails to launch or crashes on startup
```
**Solutions:**
- Ensure .NET 8.0 SDK is installed
- Check console output for detailed error messages
- Verify all NuGet packages are restored: `dotnet restore`
- Try running with: `dotnet run --verbosity diagnostic`

#### Threading Errors (Fixed in v1.2.0)
```
❌ Cross-thread operation not valid
```
**Solutions:**
- Update to latest version - this issue has been resolved
- All UI updates now properly use Dispatcher.UIThread.InvokeAsync()

#### Refresh Button Issues (Fixed in v1.2.0)
```
❌ Refresh crashes when no path is selected
```
**Solutions:**
- Update to latest version - comprehensive validation now prevents this
- Enhanced error messages guide users to valid directory selection

#### Authentication Failures
```
❌ Failed to authenticate with Azure Trusted Signing service
```
**Solutions:**
- Verify Azure credentials are correct
- Check network connectivity
- Ensure service principal has proper permissions
- Verify Trusted Signing service is available

#### File Access Errors
```
❌ Failed to process file: Access denied
```
**Solutions:**
- Run with appropriate permissions
- Check file is not in use by another process
- Verify file exists and is accessible

#### Signing Timeouts
```
⏰ Timeout waiting for signing to complete
```
**Solutions:**
- Check Azure service status
- Verify large files aren't exceeding service limits
- Check network connectivity stability

### Debug Mode
Enable verbose logging for troubleshooting:
```bash
# GUI: Check console output when run with dotnet run
# CLI: Use --verbose flag
macsigner sign --path /path/to/files --verbose
```

### Log Locations
- **macOS**: `~/Library/Logs/MacSigner/`
- **Windows**: `%APPDATA%\\MacSigner\\Logs\\`
- **Linux**: `~/.local/share/MacSigner/logs/`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup
```bash
git clone https://github.com/yourusername/macsigner.git
cd macsigner/macsigner.ui
dotnet restore
dotnet build
```

### Code Style
- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation for public APIs
- Write unit tests for new features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) for the cross-platform UI framework
- [Microsoft Azure Trusted Signing](https://azure.microsoft.com/products/trusted-signing) for the signing service
- [ReactiveUI](https://www.reactiveui.net/) for MVVM support
- [System.CommandLine](https://github.com/dotnet/command-line-api) for CLI functionality

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/macsigner/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/macsigner/discussions)
- **Email**: support@yourdomain.com

---

**MacSigner** - Making code signing simple and automated! 🚀