using PhrasePad.Core.Input;
using PhrasePad.MacOS.Accessibility;
using Xunit;

namespace PhrasePad.MacOS.Tests;

public sealed class MacAccessibilityPermissionServiceTests
{
    [Fact]
    public void DefaultConstructor_WiresProductionDependencies()
    {
        var service = new MacAccessibilityPermissionService();

        Assert.NotNull(service);
    }

    [Fact]
    public void GetStatus_WhenPermissionIsMissing_ReturnsSettingsGuidance()
    {
        var service = new MacAccessibilityPermissionService(
            new StubAccessibilityApi(isTrusted: false),
            new RecordingSettingsLauncher());

        var status = service.GetStatus();

        Assert.Equal(InputPermissionState.Denied, status.State);
        Assert.Contains("System Settings", status.Message, StringComparison.Ordinal);
        Assert.Equal(MacAccessibilityPermissionService.AccessibilitySettingsUri, status.SettingsUri);
    }

    [Fact]
    public void GetStatus_WhenPermissionIsGranted_ReturnsGrantedWithoutGuidance()
    {
        var service = new MacAccessibilityPermissionService(
            new StubAccessibilityApi(isTrusted: true),
            new RecordingSettingsLauncher());

        var status = service.GetStatus();

        Assert.Equal(InputPermissionState.Granted, status.State);
        Assert.Null(status.SettingsUri);
    }

    [Fact]
    public void OpenSettings_UsesAccessibilitySettingsUri()
    {
        var launcher = new RecordingSettingsLauncher();
        var service = new MacAccessibilityPermissionService(
            new StubAccessibilityApi(isTrusted: false),
            launcher);

        service.OpenSettings();

        Assert.Equal(MacAccessibilityPermissionService.AccessibilitySettingsUri, launcher.OpenedUri);
    }

    private sealed class StubAccessibilityApi(bool isTrusted) : IMacAccessibilityApi
    {
        public bool IsProcessTrusted() => isTrusted;
    }

    private sealed class RecordingSettingsLauncher : IMacSettingsLauncher
    {
        public Uri? OpenedUri { get; private set; }

        public void Open(Uri uri)
        {
            OpenedUri = uri;
        }
    }
}
