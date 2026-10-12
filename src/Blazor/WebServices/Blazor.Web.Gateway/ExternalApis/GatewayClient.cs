using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Blazor.Web.Gateway.ExternalApis;

internal class GatewayClient : IGatewayClient
{
    internal const string HttpClientName = "BlazorApi";
    private readonly IHttpClientFactory _httpClientFactory;

    public GatewayClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory; // injecting the factory
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HttpResponseMessage> GetAsync(string baseUrl, string route)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        var uri = BuildUri(baseUrl, route);
        return await httpClient.GetAsync(uri);
    }

    public async Task<HttpResponseMessage> PostAsync(string baseUrl, string route, object? payload = null)
    {
        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        var uri = BuildUri(baseUrl, route);
        var content = new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions), Encoding.UTF8, "application/json");
        return await httpClient.PostAsync(uri, content);
    }

    public Task<HttpResponseMessage> PatchAsync(string baseUrl, string route, object? payload = null)
    {
        throw new NotImplementedException("PatchAsync is not implemented yet.");
    }

    public Task<HttpResponseMessage> PutAsync(string baseUrl, string route, object? payload = null)
    {
        throw new NotImplementedException("PutAsync is not implemented yet.");
    }

    public Task<HttpResponseMessage> DeleteAsync(string baseUrl, string route, object? payload = null)
    {
        throw new NotImplementedException("DeleteAsync is not implemented yet.");
    }

    private static Uri BuildUri(string baseUrl, string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
        var normalizedRoute = route.StartsWith('/') ? route[1..] : route;

        return new Uri(new Uri(normalizedBaseUrl), normalizedRoute);
    }
}
