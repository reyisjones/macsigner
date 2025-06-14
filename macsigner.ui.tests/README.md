# MacSigner Test Project

A comprehensive test suite for the MacSigner application, implementing unit tests, integration tests, and UI tests using modern .NET testing practices.

## 🏗️ Project Structure

```
macsigner.ui.tests/
├── Commands/              # Command pattern tests
│   └── SignCommandTests.cs
├── Helpers/               # Test utilities and test doubles
│   └── TestHelpers.cs     # Mock factories and test utilities
├── Integration/           # End-to-end integration tests
│   └── EndToEndIntegrationTests.cs
├── Models/                # Data model validation tests
│   ├── AppSettingsTests.cs
│   ├── SigningRequestTests.cs
│   └── SignableFileTests.cs
├── Services/              # Service layer tests
│   ├── SettingsServiceTests.cs
│   └── FileServiceTests.cs
├── UI/                    # User interface tests
│   └── MainWindowUITests.cs
├── ViewModels/            # MVVM pattern tests
│   └── MainWindowViewModelTests.cs
└── macsigner.ui.tests.csproj
```

## 🧪 Testing Stack

### Core Testing Frameworks
- **xUnit 2.9.2** - Primary testing framework
- **Microsoft.NET.Test.Sdk 17.12.0** - Test SDK and runner
- **xunit.runner.visualstudio 2.8.2** - Visual Studio integration

### Testing Libraries
- **Moq 4.20.72** - Mocking framework for dependency isolation
- **FluentAssertions 6.12.2** - Expressive assertion library
- **AutoFixture 4.18.1** - Automated test data generation
- **AutoFixture.Xunit2 4.18.1** - xUnit integration for AutoFixture

### UI Testing
- **Avalonia.Headless 11.3.1** - Headless UI testing for Avalonia
- **Avalonia.Headless.XUnit 11.3.1** - xUnit integration for Avalonia testing

### Additional Testing Tools
- **Microsoft.Extensions.DependencyInjection 8.0.1** - DI container for testing
- **Microsoft.Extensions.Logging 8.0.1** - Logging infrastructure
- **Microsoft.Reactive.Testing 6.0.1** - Async and reactive testing utilities
- **coverlet.collector 6.0.2** - Code coverage collection

## 🎯 Test Categories

### ✅ Service Tests (9 tests)
**Location**: `Services/`

Tests the core business logic and service layer functionality:

- **SettingsServiceTests**: Application settings management
  - Settings loading and saving
  - Default settings creation
  - File system integration
  - Error handling and validation
  - JSON serialization/deserialization

- **FileServiceTests**: File system operations
  - Directory scanning
  - File validation
  - Signable file detection

### ✅ Model Tests
**Location**: `Models/`

Validates data models and their behavior:

- **AppSettingsTests**: Application configuration model
- **SigningRequestTests**: Code signing request model  
- **SignableFileTests**: File representation model

### ✅ ViewModel Tests
**Location**: `ViewModels/`

Tests MVVM pattern implementation:

- **MainWindowViewModelTests**: Main application view model
  - Property binding
  - Command execution
  - State management

### ✅ Command Tests
**Location**: `Commands/`

Tests command pattern implementation:

- **SignCommandTests**: Code signing command logic
  - Command execution
  - Parameter validation
  - Error handling

### ⚠️ UI Tests (Performance Issues)
**Location**: `UI/`

Tests user interface components:

- **MainWindowUITests**: Main application window testing
  - Note: Currently experiencing performance issues

### 🚧 Integration Tests (In Development)
**Location**: `Integration/`

End-to-end workflow testing:

- **EndToEndIntegrationTests**: Full application workflow testing
  - Currently empty - planned for future implementation

## 🚀 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Categories
```bash
# Service tests only (recommended for development)
dotnet test --filter "FullyQualifiedName~Services"

# Model tests only  
dotnet test --filter "FullyQualifiedName~Models"

# Specific test class
dotnet test --filter "FullyQualifiedName~SettingsServiceTests"
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run with Detailed Output
```bash
dotnet test --verbosity detailed
```

## 📊 Test Execution Performance

| Test Category | Test Count | Execution Time | Status |
|---------------|------------|----------------|---------|
| Services | 9 | ~0.7s | ✅ Fast |
| Models | ~15 | ~1.0s | ✅ Fast |
| ViewModels | ~10 | ~1.2s | ✅ Fast |
| Commands | ~8 | ~0.8s | ✅ Fast |
| UI Tests | ~10 | >60s | ⚠️ Slow |
| **Full Suite** | **~52** | **>180s** | ⚠️ **Needs Optimization** |

> **Note**: Full test suite execution is currently slow due to UI test performance issues. Use targeted test execution for development workflow.

## 🛠️ Test Utilities

### TestHelpers Class
**Location**: `Helpers/TestHelpers.cs`

Provides common testing utilities:

```csharp
// Create mock file service
var mockFileService = TestHelpers.CreateMockFileService();

// Create mock settings service
var mockSettingsService = TestHelpers.CreateMockSettingsService();

// Create mock trusted signing service
var mockSigningService = TestHelpers.CreateMockTrustedSigningService();
```

### Global Usings
The project includes global using statements for common testing namespaces:
- `Xunit`
- `Moq`  
- `FluentAssertions`
- `AutoFixture`

## 📋 Testing Patterns

### Service Testing Pattern
```csharp
public class ServiceTests : IDisposable
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly ServiceUnderTest _service;

    public ServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new ServiceUnderTest(_mockDependency.Object);
    }

    [Fact]
    public async Task Method_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        _mockDependency.Setup(x => x.Method()).ReturnsAsync(expectedValue);

        // Act
        var result = await _service.Method();

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
        _mockDependency.Verify(x => x.Method(), Times.Once);
    }

    public void Dispose()
    {
        // Cleanup resources
    }
}
```

### Model Testing Pattern
```csharp
public class ModelTests
{
    [Theory, AutoData]
    public void Property_WithValidValue_SetsCorrectly(string validValue)
    {
        // Arrange & Act
        var model = new Model { Property = validValue };

        // Assert
        model.Property.Should().Be(validValue);
    }
}
```

## 🐛 Known Issues

### Performance Issues
- **Full Test Suite**: Execution time >3 minutes (needs investigation)
- **UI Tests**: Individual UI tests run slowly
- **Integration Tests**: Not yet implemented

### Resolved Issues
- ✅ **SettingsService Null Validation**: Fixed ArgumentNullException handling
- ✅ **SettingsService Directory Creation**: Fixed directory creation for dynamic paths

## 🔧 Development Workflow

### Before Committing
1. Run service tests: `dotnet test --filter "FullyQualifiedName~Services"`
2. Run model tests: `dotnet test --filter "FullyQualifiedName~Models"`
3. Ensure no test failures

### For New Features
1. Write tests first (TDD approach)
2. Implement feature
3. Ensure all related tests pass
4. Add integration tests if needed

### For Bug Fixes
1. Write a failing test that reproduces the bug
2. Fix the bug
3. Ensure the test now passes
4. Verify no regression in other tests

## 📈 Future Improvements

### Short Term
- [ ] Optimize UI test performance
- [ ] Implement integration tests
- [ ] Add test result caching
- [ ] Create test execution categories

### Long Term  
- [ ] Implement performance benchmarking tests
- [ ] Add mutation testing
- [ ] Implement property-based testing
- [ ] Add contract testing for external dependencies

## 📞 Troubleshooting

### Common Issues

**Test Discovery Issues**:
```bash
# Clear test cache
dotnet clean
dotnet build
```

**Slow Test Execution**:
```bash
# Run specific category instead of full suite
dotnet test --filter "FullyQualifiedName~Services"
```

**Mock Setup Issues**:
```csharp
// Ensure proper mock setup
mock.Setup(x => x.Method(It.IsAny<Type>()))
    .ReturnsAsync(result);
```

---

**Last Updated**: June 14, 2025  
**Test Project Version**: 1.0  
**Target Framework**: .NET 9.0
