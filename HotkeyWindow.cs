using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenTextGrabber;

// Fenêtre invisible qui reçoit l'événement du raccourci clavier global.
sealed class HotkeyWindow : NativeWindow, IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const int HOTKEY_ID = 1;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;

    public event Action Pressed;

    public HotkeyWindow()
    {
        // HWND_MESSAGE (-3) : fenêtre sans interface, uniquement pour les messages.
        CreateHandle(new CreateParams { Parent = new IntPtr(-3) });
    }

    public bool Register(uint modifiers, uint virtualKey)
    {
        return RegisterHotKey(Handle, HOTKEY_ID, modifiers, virtualKey);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            Pressed?.Invoke();
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            DestroyHandle();
        }
    }

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
