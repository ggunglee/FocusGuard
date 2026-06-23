using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FocusGuard.Services;

public sealed record TranslationResult(string DisplayText, string PlainTranslation);

public sealed class TranslationServiceException : Exception
{
    public TranslationServiceException(string message)
        : base(message)
    {
    }

    public TranslationServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class GoogleTranslationService
{
    private readonly HttpClient _httpClient;

    public GoogleTranslationService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TranslationResult> TranslateAsync(
        string query,
        bool dictionaryMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        string requestUrl =
            "https://translate.googleapis.com/translate_a/single" +
            "?client=gtx&sl=auto&tl=ko&dt=t&dt=bd&q=" + Uri.EscapeDataString(query);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TranslationServiceException("번역 서버에 연결할 수 없습니다.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationServiceException(GetHttpErrorMessage(response.StatusCode));
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseResponse(json, dictionaryMode);
        }
    }

    public static TranslationResult ParseResponse(string json, bool dictionaryMode)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                throw new TranslationServiceException("번역 서버가 빈 결과를 반환했습니다.");
            }

            string translation = ReadTranslation(root[0]);
            if (string.IsNullOrWhiteSpace(translation))
            {
                throw new TranslationServiceException("번역 결과를 읽을 수 없습니다.");
            }

            if (!dictionaryMode)
            {
                return new TranslationResult(translation, translation);
            }

            string dictionaryText = ReadDictionary(root);
            string displayText = string.IsNullOrWhiteSpace(dictionaryText)
                ? $"🌐 [영한 번역]\n결과: {translation}"
                : $"🌐 [영한 사전]\n\n{dictionaryText}";

            return new TranslationResult(displayText, translation);
        }
        catch (TranslationServiceException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            throw new TranslationServiceException("번역 서버 응답 형식이 올바르지 않습니다.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new TranslationServiceException("번역 서버 응답을 해석할 수 없습니다.", ex);
        }
    }

    private static string ReadTranslation(JsonElement segments)
    {
        if (segments.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (JsonElement segment in segments.EnumerateArray())
        {
            if (segment.ValueKind == JsonValueKind.Array &&
                segment.GetArrayLength() > 0 &&
                segment[0].ValueKind == JsonValueKind.String)
            {
                builder.Append(segment[0].GetString());
            }
        }

        return builder.ToString();
    }

    private static string ReadDictionary(JsonElement root)
    {
        if (root.GetArrayLength() < 2 || root[1].ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (JsonElement entry in root[1].EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Array ||
                entry.GetArrayLength() < 2 ||
                entry[0].ValueKind != JsonValueKind.String ||
                entry[1].ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            string? partOfSpeech = entry[0].GetString();
            string[] meanings = entry[1]
                .EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();

            if (!string.IsNullOrWhiteSpace(partOfSpeech) && meanings.Length > 0)
            {
                lines.Add($"▪ [{partOfSpeech}]: {string.Join(", ", meanings)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetHttpErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => "번역 요청이 너무 많습니다. 잠시 후 다시 시도해주세요.",
        HttpStatusCode.Forbidden => "번역 서버가 요청을 거부했습니다.",
        _ => $"번역 서버 오류가 발생했습니다. (HTTP {(int)statusCode})"
    };
}
