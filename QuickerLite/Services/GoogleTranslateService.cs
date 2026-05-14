using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QuickerLite.Services;

public sealed class GoogleTranslateService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var source = string.IsNullOrWhiteSpace(sourceLanguageCode) ? "auto" : sourceLanguageCode;
        var target = string.IsNullOrWhiteSpace(targetLanguageCode) ? "zh-CN" : targetLanguageCode;
        var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
            + Uri.EscapeDataString(source)
            + "&tl="
            + Uri.EscapeDataString(target)
            + "&dt=t&q="
            + Uri.EscapeDataString(text.Trim());
        using var response = await Client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Google 翻译返回格式异常。");
        }

        var translatedParts = root[0]
            .EnumerateArray()
            .Where(part => part.ValueKind == JsonValueKind.Array && part.GetArrayLength() > 0)
            .Select(part => part[0].ValueKind == JsonValueKind.String ? part[0].GetString() : "")
            .Where(part => !string.IsNullOrEmpty(part));

        var translated = string.Concat(translatedParts);
        if (string.IsNullOrWhiteSpace(translated))
        {
            throw new InvalidOperationException("没有收到翻译结果。");
        }

        return translated;
    }
}
