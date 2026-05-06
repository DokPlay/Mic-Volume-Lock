using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MicVolumeLock.Services;

public enum HotkeyAction
{
    ToggleProtection,
    VolumeUp,
    VolumeDown
}

public sealed class HotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const int IdToggle = 701;
    private const int IdUp = 702;
    private const int IdDown = 703;

    private readonly Window _window;
    private HwndSource? _source;
    private nint _handle;
    private bool _registered;

    public event Action<HotkeyAction>? HotkeyPressed;

    public HotkeyManager(Window window)
    {
        _window = window;
    }

    public bool Register()
    {
        Unregister();

        _handle = new WindowInteropHelper(_window).Handle;
        if (_handle == 0)
        {
            return false;
        }

        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);

        var ok =
            RegisterHotKey(_handle, IdToggle, ModControl | ModAlt, (uint)System.Windows.Forms.Keys.M) &&
            RegisterHotKey(_handle, IdUp, ModControl | ModAlt, (uint)System.Windows.Forms.Keys.Up) &&
            RegisterHotKey(_handle, IdDown, ModControl | ModAlt, (uint)System.Windows.Forms.Keys.Down);

        _registered = ok;
        if (!ok)
        {
            Unregister();
        }

        return ok;
    }

    public void Unregister()
    {
        if (_handle != 0)
        {
            UnregisterHotKey(_handle, IdToggle);
            UnregisterHotKey(_handle, IdUp);
            UnregisterHotKey(_handle, IdDown);
        }

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }

        _registered = false;
    }

    public void Dispose()
    {
        Unregister();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (!_registered || msg != WmHotkey)
        {
            return 0;
        }

        handled = true;
        var id = wParam.ToInt32();
        var action = id switch
        {
            IdToggle => HotkeyAction.ToggleProtection,
            IdUp => HotkeyAction.VolumeUp,
            IdDown => HotkeyAction.VolumeDown,
            _ => (HotkeyAction?)null
        };

        if (action.HasValue)
        {
            HotkeyPressed?.Invoke(action.Value);
        }

        return 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
