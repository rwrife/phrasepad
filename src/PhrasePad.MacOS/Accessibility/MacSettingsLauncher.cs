using System.Diagnostics;

namespace PhrasePad.MacOS.Accessibility;

internal sealed class MacSettingsLauncher : IMacSettingsLauncher
{
    public void Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException(
                "macOS System Settings can only be opened on macOS.");
        }

        var startInfo = new ProcessStartInfo("/usr/bin/open")
        {
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(uri.AbsoluteUri);

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start macOS System Settings.");
    }
}
