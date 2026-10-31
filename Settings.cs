using Microsoft.Win32;

namespace ScreenTextGrabber;

static class Settings
{
    const string KeyPath = @"Software\ScreenTextGrabber";
    const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string AppName = "ScreenTextGrabber";

    // Langue OCR choisie (vide = langues du système).
    public static string Language
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(KeyPath);
            return k?.GetValue("Language") as string ?? "";
        }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(KeyPath);
            k.SetValue("Language", value ?? "");
        }
    }

    // Au premier lancement, on active le démarrage avec Windows.
    public static void InitOnFirstRun()
    {
        using var k = Registry.CurrentUser.CreateSubKey(KeyPath);
        if (k.GetValue("Initialized") == null)
        {
            SetStartup(true);
            k.SetValue("Initialized", 1);
        }
    }

    public static bool StartupEnabled
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunPath);
            return k?.GetValue(AppName) != null;
        }
    }

    public static void SetStartup(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(RunPath);
        if (on)
            k.SetValue(AppName, "\"" + Environment.ProcessPath + "\"");
        else
            k.DeleteValue(AppName, false);
    }
}
