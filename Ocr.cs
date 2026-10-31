using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenTextGrabber;

static class Ocr
{
    public static OcrEngine CreateEngine()
    {
        string tag = Settings.Language;
        if (!string.IsNullOrEmpty(tag))
        {
            foreach (Language lang in OcrEngine.AvailableRecognizerLanguages)
            {
                if (lang.LanguageTag == tag)
                {
                    OcrEngine e = OcrEngine.TryCreateFromLanguage(lang);
                    if (e != null) return e;
                }
            }
        }
        return OcrEngine.TryCreateFromUserProfileLanguages();
    }

    public static async Task<string> RecognizeAsync(Bitmap source, OcrEngine engine)
    {
        using Bitmap prepared = Prepare(source);
        using var ms = new MemoryStream();
        prepared.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        using var stream = ms.AsRandomAccessStream();
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
        using SoftwareBitmap sb = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        OcrResult result = await engine.RecognizeAsync(sb);
        return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text));
    }

    // Les petites zones sont agrandies : Windows OCR lit mieux les textes plus gros.
    static Bitmap Prepare(Bitmap src)
    {
        double scale = Math.Max(src.Width, src.Height) < 1500 ? 2.0 : 1.0;
        int w = (int)(src.Width * scale);
        int h = (int)(src.Height * scale);

        int max = (int)OcrEngine.MaxImageDimension;
        if (w > max || h > max)
        {
            double k = Math.Min((double)max / w, (double)max / h);
            w = (int)(w * k);
            h = (int)(h * k);
        }

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(src, new Rectangle(0, 0, w, h));
        return bmp;
    }
}
