namespace PhrasePad.Core.Input;

public sealed record InputPermissionStatus(
    InputPermissionState State,
    string Message,
    Uri? SettingsUri);
