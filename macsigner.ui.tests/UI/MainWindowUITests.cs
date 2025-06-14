using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAssertions;
using MacSigner.Models;
using MacSigner.Services;
using MacSigner.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MacSigner.Tests.UI
{
    public class MainWindowUITests
    {
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly Mock<ITrustedSigningService> _trustedSigningServiceMock;
        private readonly Mock<ISettingsService> _settingsServiceMock;
        private readonly Mock<ILogger<MainWindowViewModel>> _loggerMock;
        private readonly IFixture _fixture;

        public MainWindowUITests()
        {
            _fixture = new Fixture();
            _fileServiceMock = new Mock<IFileService>();
            _trustedSigningServiceMock = new Mock<ITrustedSigningService>();
            _settingsServiceMock = new Mock<ISettingsService>();
            _loggerMock = new Mock<ILogger<MainWindowViewModel>>();

            _settingsServiceMock.Setup(x => x.LoadSettingsAsync())
                .ReturnsAsync(_fixture.Create<AppSettings>());
        }

        [Fact]
        public async Task MainWindow_CanBeCreated()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange & Act
                var viewModel = new MainWindowViewModel(
                    _fileServiceMock.Object,
                    _trustedSigningServiceMock.Object,
                    _settingsServiceMock.Object,
                    _loggerMock.Object);

                var window = new MainWindow
                {
                    DataContext = viewModel
                };

                // Assert
                window.Should().NotBeNull();
                window.DataContext.Should().Be(viewModel);
            });
        }

        [Fact]
        public async Task MainWindow_HasCorrectTitle()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange & Act
                var window = new MainWindow();

                // Assert
                window.Title.Should().Be("MacSigner - Digital Code Signing Tool");
            });
        }

        [Fact]
        public async Task MainWindow_HasCorrectInitialSize()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange & Act
                var window = new MainWindow();

                // Assert
                window.Width.Should().Be(1200);
                window.Height.Should().Be(800);
                window.MinWidth.Should().Be(1000);
                window.MinHeight.Should().Be(600);
            });
        }

        [Fact]
        public async Task MainWindow_CanFindFilesList()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var viewModel = new MainWindowViewModel(
                    _fileServiceMock.Object,
                    _trustedSigningServiceMock.Object,
                    _settingsServiceMock.Object,
                    _loggerMock.Object);

                var window = new MainWindow
                {
                    DataContext = viewModel
                };

                // Act - Force the window to apply its template
                window.ApplyTemplate();
                
                // Find the ListBox that displays files (it's bound to SignableFiles)
                var filesList = window.FindDescendantOfType<ListBox>();

                // Assert
                filesList.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task MainWindow_FilesListBinding_WorksCorrectly()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var viewModel = new MainWindowViewModel(
                    _fileServiceMock.Object,
                    _trustedSigningServiceMock.Object,
                    _settingsServiceMock.Object,
                    _loggerMock.Object);

                var testFile = _fixture.Create<SignableFile>();
                viewModel.SignableFiles.Add(testFile);

                var window = new MainWindow
                {
                    DataContext = viewModel
                };

                // Act
                window.ApplyTemplate();
                var filesList = window.FindDescendantOfType<ListBox>();

                // Assert
                filesList.Should().NotBeNull();
                filesList!.ItemsSource.Should().Be(viewModel.SignableFiles);
            });
        }

        [Fact]
        public async Task MainWindow_CanFindTabControl()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var window = new MainWindow();

                // Act
                window.ApplyTemplate();
                var tabControl = window.FindDescendantOfType<TabControl>();

                // Assert
                tabControl.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task MainWindow_CanFindProgressBars()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var window = new MainWindow();

                // Act
                window.ApplyTemplate();
                var progressBars = window.FindDescendantsOfType<ProgressBar>().ToList();

                // Assert
                progressBars.Should().HaveCountGreaterOrEqualTo(1);
            });
        }

        [Fact]
        public async Task MainWindow_CanFindButtons()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var window = new MainWindow();

                // Act
                window.ApplyTemplate();
                var buttons = window.FindDescendantsOfType<Button>().ToList();

                // Assert
                buttons.Should().NotBeEmpty();
                buttons.Should().Contain(b => b.Content != null && b.Content.ToString()!.Contains("Browse"));
                buttons.Should().Contain(b => b.Content != null && b.Content.ToString()!.Contains("Refresh"));
                buttons.Should().Contain(b => b.Content != null && b.Content.ToString()!.Contains("Sign"));
            });
        }

        [Fact]
        public async Task MainWindow_CanFindTextBoxes()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var window = new MainWindow();

                // Act
                window.ApplyTemplate();
                var textBoxes = window.FindDescendantsOfType<TextBox>().ToList();

                // Assert
                textBoxes.Should().NotBeEmpty();
            });
        }

        [Fact]
        public async Task MainWindow_ViewModelBinding_WorksCorrectly()
        {
            await UiThreadHelper.RunOnUiThread(async () =>
            {
                // Arrange
                var viewModel = new MainWindowViewModel(
                    _fileServiceMock.Object,
                    _trustedSigningServiceMock.Object,
                    _settingsServiceMock.Object,
                    _loggerMock.Object);

                var window = new MainWindow
                {
                    DataContext = viewModel
                };

                // Act & Assert
                window.DataContext.Should().Be(viewModel);
                ((MainWindowViewModel)window.DataContext!).SignableFiles.Should().NotBeNull();
                ((MainWindowViewModel)window.DataContext!).SigningRequests.Should().NotBeNull();
            });
        }
    }

    // Helper class for UI thread operations
    public static class UiThreadHelper
    {
        public static async Task RunOnUiThread(Func<Task> action)
        {
            if (Application.Current == null)
            {
                AppBuilder.Configure<TestApp>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
            }

            await Dispatcher.UIThread.InvokeAsync(action);
        }

        public static async Task RunOnUiThread(Action action)
        {
            await RunOnUiThread(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }
    }

    // Test application class
    public class TestApp : Application
    {
        public override void Initialize()
        {
            // Empty initialization
        }
    }
}

// Extension methods for finding UI elements
public static class ControlExtensions
{
    public static T? FindDescendantOfType<T>(this Control control) where T : Control
    {
        var queue = new Queue<Control>();
        queue.Enqueue(control);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current is T result)
                return result;

            if (current is Panel panel)
            {
                foreach (Control child in panel.Children)
                    queue.Enqueue(child);
            }
            else if (current is ContentControl contentControl && contentControl.Content is Control contentChild)
            {
                queue.Enqueue(contentChild);
            }
            else if (current is Decorator decorator && decorator.Child is Control decoratorChild)
            {
                queue.Enqueue(decoratorChild);
            }
        }

        return null;
    }

    public static IEnumerable<T> FindDescendantsOfType<T>(this Control control) where T : Control
    {
        var queue = new Queue<Control>();
        queue.Enqueue(control);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current is T result)
                yield return result;

            if (current is Panel panel)
            {
                foreach (Control child in panel.Children)
                    queue.Enqueue(child);
            }
            else if (current is ContentControl contentControl && contentControl.Content is Control contentChild)
            {
                queue.Enqueue(contentChild);
            }
            else if (current is Decorator decorator && decorator.Child is Control decoratorChild)
            {
                queue.Enqueue(decoratorChild);
            }
        }
    }
}
