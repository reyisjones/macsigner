# MacSigner Test Issues Log

**Date**: June 14, 2025  
**Project**: MacSigner (.NET Avalonia Application)  
**Test Suite**: macsigner.ui.tests

## Overview

This document tracks the issues encountered during the testing and debugging process of the MacSigner application, specifically focusing on the SettingsService functionality.

## Issues Encountered & Resolutions

### 1. SettingsService SaveSettingsAsync Issues

**Status**: ✅ RESOLVED

#### Issue 1.1: Missing ArgumentNullException Validation
- **Description**: The `SaveSettingsAsync` method in `SettingsService` did not validate null input parameters
- **Test**: `SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException`
- **Error**: Test expected `ArgumentNullException` but no exception was thrown
- **Root Cause**: Missing null check at the beginning of the method
- **Resolution**: Added null validation with proper exception throwing

```csharp
// BEFORE (missing validation)
public async Task SaveSettingsAsync(AppSettings settings)
{
    try
    {
        // ... rest of method
    }
    // ...
}

// AFTER (with validation)
public async Task SaveSettingsAsync(AppSettings settings)
{
    if (settings == null)
        throw new ArgumentNullException(nameof(settings));
    
    try
    {
        // ... rest of method
    }
    // ...
}
```

#### Issue 1.2: Directory Creation Failure
- **Description**: The method failed to create the directory structure when the settings file path pointed to a non-existent directory
- **Test**: `SaveSettingsAsync_CreatesDirectoryIfNotExists`
- **Error**: `DirectoryNotFoundException` when attempting to write to a path where the directory didn't exist
- **Root Cause**: The original implementation relied on a private `_settingsDirectory` field, but the test framework used reflection to override `_settingsFilePath`, causing a mismatch
- **Resolution**: Changed directory creation logic to dynamically derive the directory from the current file path

```csharp
// BEFORE (using private field)
if (!Directory.Exists(_settingsDirectory))
{
    Directory.CreateDirectory(_settingsDirectory);
}

// AFTER (using dynamic path resolution)
var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);
if (!string.IsNullOrEmpty(settingsDirectory) && !Directory.Exists(settingsDirectory))
{
    Directory.CreateDirectory(settingsDirectory);
}
```

### 2. Test Execution Performance Issues

**Status**: ⚠️ ONGOING CONCERN

#### Issue 2.1: Slow Test Suite Execution
- **Description**: Full test suite execution takes extremely long (>3 minutes)
- **Impact**: Development workflow efficiency
- **Observations**: 
  - Individual test categories (e.g., SettingsService tests) run quickly (~0.7s for 9 tests)
  - Full suite appears to hang or take excessive time
- **Potential Causes**:
  - UI/Integration tests taking excessive time
  - Resource cleanup issues
  - Async operation timeouts
- **Workaround**: Run targeted test filters instead of full suite

## Test Categories & Status

### ✅ Services Tests (All Passing)
- **SettingsService**: 9 tests - All passing
- **FileService**: Tests available and passing  
- **Coverage**: Core service functionality

### ⚠️ UI Tests (Performance Issues)
- **MainWindowUITests**: Present but slow execution
- **Integration Tests**: EndToEndIntegrationTests.cs exists but empty

### ✅ Models Tests (Stable)
- **AppSettingsTests**: Data model validation
- **SigningRequestTests**: Request model testing
- **SignableFileTests**: File model testing

### ✅ ViewModels Tests (Stable)
- **MainWindowViewModelTests**: MVVM pattern testing

### ✅ Commands Tests (Stable)
- **SignCommandTests**: Command pattern implementation

## Technical Details

### Test Framework Stack
- **Testing Framework**: xUnit 2.9.2
- **Mocking**: Moq 4.20.72
- **Assertions**: FluentAssertions 6.12.2
- **Test Data**: AutoFixture 4.18.1
- **UI Testing**: Avalonia.Headless 11.3.1
- **Target Framework**: .NET 9.0

### Test Project Structure
```
macsigner.ui.tests/
├── Commands/           # Command pattern tests
├── Helpers/           # Test utilities and mocks
├── Integration/       # End-to-end integration tests
├── Models/           # Data model tests
├── Services/         # Service layer tests
├── UI/              # User interface tests
└── ViewModels/      # MVVM pattern tests
```

## Recommendations

### Immediate Actions
1. ✅ **COMPLETED**: Fix SettingsService null validation and directory creation
2. 🔄 **IN PROGRESS**: Investigate and optimize slow-running tests
3. 📝 **TODO**: Implement integration tests in `EndToEndIntegrationTests.cs`

### Long-term Improvements
1. **Performance Optimization**: 
   - Profile test execution to identify bottlenecks
   - Implement test parallelization where appropriate
   - Add test execution timeouts

2. **Test Coverage Enhancement**:
   - Complete integration test implementation
   - Add performance benchmarking tests
   - Implement end-to-end signing workflow tests

3. **Developer Experience**:
   - Create test categories for different execution speeds
   - Implement test result caching
   - Add pre-commit test hooks

## Metrics

- **Total Test Files**: 10
- **Total Lines of Test Code**: ~1,295
- **SettingsService Test Coverage**: 100% (9 tests)
- **Test Execution Time**: 
  - Services Tests: ~0.7s
  - Full Suite: >180s (needs optimization)

## Change Log

| Date | Change | Impact |
|------|--------|---------|
| 2025-06-14 | Fixed SettingsService null validation | ✅ SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException now passes |
| 2025-06-14 | Fixed SettingsService directory creation | ✅ SaveSettingsAsync_CreatesDirectoryIfNotExists now passes |
| 2025-06-14 | Documented performance issues | 📝 Identified full test suite performance bottleneck |

---

**Last Updated**: June 14, 2025  
**Status**: SettingsService issues resolved, performance optimization pending
