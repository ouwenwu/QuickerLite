using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace QuickerLite.Services;

public sealed class WindowsOcrService
{
    public async Task<string> RecognizeAsync(string imagePath)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en"));

        if (engine is null)
        {
            throw new InvalidOperationException("当前系统没有可用的 Windows OCR 引擎。");
        }

        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var result = await engine.RecognizeAsync(bitmap);

        return string.Join(
            Environment.NewLine,
            result.Lines.Select(line => line.Text).Where(line => !string.IsNullOrWhiteSpace(line)));
    }
}
