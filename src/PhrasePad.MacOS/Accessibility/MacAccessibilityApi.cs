using System.Runtime.InteropServices;

namespace PhrasePad.MacOS.Accessibility;

internal sealed class MacAccessibilityApi : IMacAccessibilityApi
{
    private const string ApplicationServicesFramework =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    public bool IsProcessTrusted()
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException(
                "macOS Accessibility permission can only be queried on macOS.");
        }

        return AXIsProcessTrusted();
    }

    [DllImport(ApplicationServicesFramework)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrusted();
}
