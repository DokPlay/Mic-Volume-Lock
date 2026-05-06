using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfPoint = System.Windows.Point;

namespace MicVolumeLock.Services;

public static class ThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    public static void Apply(bool dark)
    {
        if (dark)
        {
            SetGradient("AppBackground", "#08111B", "#101C2A", "#0C1722");
            SetSolid("PanelBackground", "#F0121C2B");
            SetSolid("PanelElevatedBackground", "#FF162235");
            SetSolid("PanelBorder", "#FF2A3A52");
            SetSolid("TextPrimary", "#FFF5FAFF");
            SetSolid("TextSecondary", "#FF9FB2C8");
            SetSolid("AccentBrush", "#FF35D7CC");
            SetSolid("AccentBrushHover", "#FF57EFE4");
            SetSolid("AccentTextBrush", "#FFBDFCF7");
            SetSolid("AccentSoftBrush", "#FF103E48");
            SetSolid("SecondaryButtonBackground", "#FF1A2739");
            SetSolid("InputBackground", "#FF0E1724");
            SetSolid("InputHoverBackground", "#FF142235");
            SetSolid("TabRailBackground", "#FF111A28");
            SetSolid("LogBackground", "#FF0D1622");
            SetSolid("ShadowBrush", "#66000000");
            return;
        }

        SetGradient("AppBackground", "#EDF7F8", "#F7FAFC", "#EAF0F7");
        SetSolid("PanelBackground", "#F7FFFFFF");
        SetSolid("PanelElevatedBackground", "#FFFFFFFF");
        SetSolid("PanelBorder", "#FFCCD9E6");
        SetSolid("TextPrimary", "#FF112033");
        SetSolid("TextSecondary", "#FF5D7088");
        SetSolid("AccentBrush", "#FF0E766E");
        SetSolid("AccentBrushHover", "#FF10998F");
        SetSolid("AccentTextBrush", "#FF073F3B");
        SetSolid("AccentSoftBrush", "#FFDFF6F2");
        SetSolid("SecondaryButtonBackground", "#FFEEF5F8");
        SetSolid("InputBackground", "#FFFFFFFF");
        SetSolid("InputHoverBackground", "#FFF1F8FA");
        SetSolid("TabRailBackground", "#FFDDEAF0");
        SetSolid("LogBackground", "#FFF7FAFC");
        SetSolid("ShadowBrush", "#22001E2A");
    }

    public static void ApplyWindowTheme(Window window, bool dark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            var enabled = dark ? 1 : 0;
            var result = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
            if (result != 0)
            {
                _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
            }
        }
        catch
        {
            // The title bar theme is best-effort and must never block the app.
        }
    }

    private static void SetSolid(string key, string color)
    {
        WpfApplication.Current.Resources[key] = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString(color));
    }

    private static void SetGradient(string key, string start, string middle, string end)
    {
        WpfApplication.Current.Resources[key] = new LinearGradientBrush
        {
            StartPoint = new WpfPoint(0, 0),
            EndPoint = new WpfPoint(1, 1),
            GradientStops = new GradientStopCollection
            {
                new((WpfColor)WpfColorConverter.ConvertFromString(start), 0),
                new((WpfColor)WpfColorConverter.ConvertFromString(middle), 0.48),
                new((WpfColor)WpfColorConverter.ConvertFromString(end), 1)
            }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
