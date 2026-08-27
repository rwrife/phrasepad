using PhrasePad.Core.Input;

namespace PhrasePad.MacOS.Accessibility;

public sealed class MacAccessibilityPermissionService : IInputPermissionService
{
    public static readonly Uri AccessibilitySettingsUri = new(
        "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility");

    private readonly IMacAccessibilityApi _accessibilityApi;
    private readonly IMacSettingsLauncher _settingsLauncher;

    public MacAccessibilityPermissionService()
        : this(new MacAccessibilityApi(), new MacSettingsLauncher())
    {
    }

    public MacAccessibilityPermissionService(
        IMacAccessibilityApi accessibilityApi,
        IMacSettingsLauncher settingsLauncher)
    {
        ArgumentNullException.ThrowIfNull(accessibilityApi);
        ArgumentNullException.ThrowIfNull(settingsLauncher);

        _accessibilityApi = accessibilityApi;
        _settingsLauncher = settingsLauncher;
    }

    public InputPermissionStatus GetStatus()
    {
        if (_accessibilityApi.IsProcessTrusted())
        {
            return new InputPermissionStatus(
                InputPermissionState.Granted,
                "Accessibility permission is granted.",
                SettingsUri: null);
        }

        return new InputPermissionStatus(
            InputPermissionState.Denied,
            "PhrasePad needs Accessibility permission. Open System Settings → Privacy & Security → Accessibility and enable PhrasePad.",
            AccessibilitySettingsUri);
    }

    public void OpenSettings() => _settingsLauncher.Open(AccessibilitySettingsUri);
}
