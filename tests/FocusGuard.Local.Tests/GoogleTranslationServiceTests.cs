using System.Net;
using System.Text;
using FocusGuard.Services;
using Xunit;

namespace FocusGuard.Local.Tests;

public sealed class GoogleTranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_RequestsExpectedEndpointAndCombinesSegments()
    {
        Uri? requestedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse("[[[\"집중\",\"Focus\"],[\"하세요\",\" please\"]],null,\"en\"]");
        });
        var service = new GoogleTranslationService(new HttpClient(handler));

        TranslationResult result = await service.TranslateAsync("Focus please", dictionaryMode: false);

        Assert.Equal("집중하세요", result.PlainTranslation);
        Assert.Equal("집중하세요", result.DisplayText);
        Assert.NotNull(requestedUri);
        Assert.Equal("translate.googleapis.com", requestedUri.Host);
        Assert.Contains("client=gtx", requestedUri.Query);
        Assert.Contains("sl=auto", requestedUri.Query);
        Assert.Contains("tl=ko", requestedUri.Query);
        Assert.Contains("dt=t", requestedUri.Query);
        Assert.Contains("dt=bd", requestedUri.Query);
        Assert.Contains("q=Focus%20please", requestedUri.Query);
    }

    [Fact]
    public async Task TranslateAsync_FormatsDictionaryEntries()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            "[[[\"집중하다\",\"focus\"]],[[\"verb\",[\"집중하다\",\"초점을 맞추다\"]],[\"noun\",[\"초점\"]]],\"en\"]"));
        var service = new GoogleTranslationService(new HttpClient(handler));

        TranslationResult result = await service.TranslateAsync("focus", dictionaryMode: true);

        Assert.Equal("집중하다", result.PlainTranslation);
        Assert.Contains("🌐 [영한 사전]", result.DisplayText);
        Assert.Contains("▪ [verb]: 집중하다, 초점을 맞추다", result.DisplayText);
        Assert.Contains("▪ [noun]: 초점", result.DisplayText);
    }

    [Fact]
    public async Task TranslateAsync_UsesTranslationFallbackWithoutDictionaryEntries()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("[[[\"집중하다\",\"focus\"]],null,\"en\"]"));
        var service = new GoogleTranslationService(new HttpClient(handler));

        TranslationResult result = await service.TranslateAsync("focus", dictionaryMode: true);

        Assert.Equal("🌐 [영한 번역]\n결과: 집중하다", result.DisplayText);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "[]")]
    [InlineData(HttpStatusCode.OK, "not-json")]
    public async Task TranslateAsync_ThrowsTypedExceptionForBadResponses(HttpStatusCode status, string body)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        var service = new GoogleTranslationService(new HttpClient(handler));

        await Assert.ThrowsAsync<TranslationServiceException>(
            () => service.TranslateAsync("focus", dictionaryMode: false));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
