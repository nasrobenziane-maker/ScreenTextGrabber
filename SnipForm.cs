using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenTextGrabber;

// Écran de sélection : image figée de l'écran, assombrie, avec un cadre tracé à la souris.
sealed class SnipForm : Form
{
    readonly Bitmap shot;
    readonly Rectangle screenBounds;
    Point start, current;
    bool dragging;

    // Zone choisie, en pixels de la capture (vide si annulé).
    public Rectangle Selection { get; private set; } = Rectangle.Empty;

    public SnipForm(Bitmap screenshot, Rectangle bounds)
    {
        shot = screenshot;
        screenBounds = bounds;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Placement exact en pixels physiques, sans mise à l'échelle automatique.
        SetWindowPos(Handle, HWND_TOPMOST, screenBounds.X, screenBounds.Y,
                screenBounds.Width, screenBounds.Height, SWP_SHOWWINDOW);
        Activate();
        SetForegroundWindow(Handle);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
            Cancel();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right) { Cancel(); return; }
        if (e.Button != MouseButtons.Left) return;
        dragging = true;
        start = current = e.Location;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging) return;
        current = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!dragging || e.Button != MouseButtons.Left) return;
        dragging = false;
        Selection = ToShot(Norm(start, e.Location));
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        var full = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

        g.DrawImage(shot, full);
        using (var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            g.FillRectangle(dim, full);

        if (dragging)
        {
            Rectangle r = Norm(start, current);
            if (r.Width > 0 && r.Height > 0)
            {
                g.DrawImage(shot, r, ToShot(r), GraphicsUnit.Pixel);
                using var pen = new Pen(Color.DeepSkyBlue, 2);
                g.DrawRectangle(pen, r);
            }
        }

        using var font = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.DirectionRightToLeft
        };
        g.DrawString("اسحب بالماوس لتحديد النص   •   Esc للإلغاء", font, Brushes.White,
                new RectangleF(0, 14, ClientSize.Width, 30), sf);
    }

    void Cancel()
    {
        dragging = false;
        Selection = Rectangle.Empty;
        Close();
    }

    static Rectangle Norm(Point a, Point b)
    {
        return Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
                Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    // Convertit un rectangle de la fenêtre en rectangle de la capture.
    Rectangle ToShot(Rectangle r)
    {
        double sx = (double)shot.Width / Math.Max(1, ClientSize.Width);
        double sy = (double)shot.Height / Math.Max(1, ClientSize.Height);
        var s = Rectangle.FromLTRB((int)(r.Left * sx), (int)(r.Top * sy),
                (int)(r.Right * sx), (int)(r.Bottom * sy));
        return Rectangle.Intersect(s, new Rectangle(0, 0, shot.Width, shot.Height));
    }

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
}
