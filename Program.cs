using System.Threading;
using System.Windows.Forms;

namespace ScreenTextGrabber;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Une seule instance à la fois.
        using var mutex = new Mutex(true, "ScreenTextGrabber_SingleInstance", out bool created);
        if (!created) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
