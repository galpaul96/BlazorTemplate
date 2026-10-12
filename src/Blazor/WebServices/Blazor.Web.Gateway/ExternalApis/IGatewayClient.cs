using System.Net.Http;

namespace Blazor.Web.Gateway.ExternalApis;

public interface IGatewayClient
{
    Task<HttpResponseMessage> GetAsync(string baseUrl, string route);

    Task<HttpResponseMessage> PostAsync(string baseUrl, string route, object? payload = null);

    Task<HttpResponseMessage> PatchAsync(string baseUrl, string route, object? payload = null);

    Task<HttpResponseMessage> PutAsync(string baseUrl, string route, object? payload = null);

    Task<HttpResponseMessage> DeleteAsync(string baseUrl, string route, object? payload = null);
}
