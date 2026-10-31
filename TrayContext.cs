using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Windows.Globalization;
using Windows.Media.Ocr;

namespace ScreenTextGrabber;

// Le programme vit dans la zone de notification (près de l'horloge Windows).
sealed class TrayContext : ApplicationContext
{
    readonly NotifyIcon tray;
    readonly HotkeyWindow hotkey;
    readonly Control invoker;
    bool busy;

    public TrayContext()
    {
        Settings.InitOnFirstRun();

        invoker = new Control();
        _ = invoker.Handle;

        var menu = new ContextMenuStrip();

        var capture = new ToolStripMenuItem("استخراج نص (Ctrl+E)");
        capture.Click += (s, e) => invoker.BeginInvoke(new Action(StartCapture));

        var startup = new ToolStripMenuItem("التشغيل مع ويندوز")
        {
            Checked = Settings.StartupEnabled,
            CheckOnClick = true
        };
        startup.CheckedChanged += (s, e) => Settings.SetStartup(startup.Checked);

        var langMenu = new ToolStripMenuItem("لغة التعرف على النص");
        BuildLanguageMenu(langMenu);

        var exit = new ToolStripMenuItem("خروج");
        exit.Click += (s, e) => ExitThread();

        menu.Items.Add(capture);
        menu.Items.Add(startup);
        menu.Items.Add(langMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        tray = new NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "Screen Text Grabber  (Ctrl+E)",
            ContextMenuStrip = menu,
            Visible = true
        };

        hotkey = new HotkeyWindow();
        hotkey.Pressed += () => invoker.BeginInvoke(new Action(StartCapture));
        bool ok = hotkey.Register(HotkeyWindow.MOD_CONTROL | HotkeyWindow.MOD_NOREPEAT, 0x45); // E
        if (ok)
            tray.ShowBalloonTip(2000, "جاهز", "اضغط Ctrl+E ثم حدّد المنطقة بالماوس.", ToolTipIcon.Info);
        else
            tray.ShowBalloonTip(5000, "تعذّر تسجيل الاختصار",
                    "الاختصار Ctrl+E مستخدم من برنامج آخر.", ToolTipIcon.Warning);
    }

    async void StartCapture()
    {
        if (busy) return;
        busy = true;
        try
        {
            // 1) Capture de l'écran où se trouve la souris.
            Rectangle b = Screen.FromPoint(Cursor.Position).Bounds;
            using var shot = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(shot))
                g.CopyFromScreen(b.Location, Point.Empty, b.Size);

            // 2) Choix de la zone à la souris.
            Rectangle sel;
            using (var form = new SnipForm(shot, b))
            {
                form.ShowDialog();
                sel = form.Selection;
            }
            if (sel.Width < 4 || sel.Height < 4) return;

            // 3) Reconnaissance du texte.
            OcrEngine engine = Ocr.CreateEngine();
            if (engine == null)
            {
                tray.ShowBalloonTip(5000, "لا توجد لغة للتعرف",
                        "ثبّت لغة مع ميزة التعرف على النص من إعدادات ويندوز.", ToolTipIcon.Warning);
                return;
            }

            using Bitmap crop = shot.Clone(sel, PixelFormat.Format32bppArgb);
            string text = (await Ocr.RecognizeAsync(crop, engine)).Trim();

            if (text.Length == 0)
            {
                tray.ShowBalloonTip(3000, "لم يُعثر على نص", "جرّب تحديد منطقة أكبر أو أوضح.", ToolTipIcon.Info);
                return;
            }

            // 4) Copie dans le presse-papiers : il suffit de coller avec Ctrl+V.
            Clipboard.SetDataObject(text, true, 5, 100);
            string preview = text.Length > 80 ? text.Substring(0, 80) + "…" : text;
            tray.ShowBalloonTip(2500, "تم نسخ النص — الصقه بـ Ctrl+V", preview, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            tray.ShowBalloonTip(5000, "حدث خطأ", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            busy = false;
        }
    }

    void BuildLanguageMenu(ToolStripMenuItem parent)
    {
        parent.DropDownItems.Clear();

        var auto = new ToolStripMenuItem("تلقائي (لغات النظام)") { Tag = "" };
        parent.DropDownItems.Add(auto);

        foreach (Language lang in OcrEngine.AvailableRecognizerLanguages)
            parent.DropDownItems.Add(new ToolStripMenuItem(lang.DisplayName) { Tag = lang.LanguageTag });

        if (parent.DropDownItems.Count == 1)
            parent.DropDownItems.Add(new ToolStripMenuItem("لا توجد لغات للتعرف مثبّتة") { Enabled = false });

        void Refresh()
        {
            foreach (ToolStripItem it in parent.DropDownItems)
                if (it is ToolStripMenuItem mi && mi.Tag is string tag)
                    mi.Checked = tag == Settings.Language;
        }

        foreach (ToolStripItem it in parent.DropDownItems)
        {
            if (it is ToolStripMenuItem mi && mi.Tag is string tag)
            {
                mi.Click += (s, e) =>
                {
                    Settings.Language = tag;
                    Refresh();
                };
            }
        }
        Refresh();
    }

    static Icon MakeIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var back = new SolidBrush(Color.FromArgb(30, 136, 229));
            g.FillEllipse(back, 1, 1, 30, 30);
            using var font = new Font("Segoe UI", 17f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("T", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void ExitThreadCore()
    {
        tray.Visible = false;
        tray.Dispose();
        hotkey.Dispose();
        base.ExitThreadCore();
    }
}
