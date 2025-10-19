using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace AllowanceTracker.Azure.Adapter.Http;

/// <summary>
/// Azure Functions implementation of IHttpResponse
/// Wraps HttpResponseData to provide cloud-agnostic HTTP response abstraction
/// </summary>
public class AzureFunctionsHttpResponse : IHttpResponse
{
    private readonly HttpResponseData _response;

    public AzureFunctionsHttpResponse(HttpResponseData response)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public HttpStatusCode StatusCode
    {
        get => _response.StatusCode;
        set => _response.StatusCode = value;
    }

    public IDictionary<string, string> Headers
    {
        get
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in _response.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }
            return headers;
        }
    }

    public string? Body { get; set; }

    public async Task WriteAsJsonAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        _response.Headers.Add("Content-Type", "application/json");
        await _response.WriteAsJsonAsync(data, cancellationToken);
    }

    public async Task WriteStringAsync(string content, CancellationToken cancellationToken = default)
    {
        Body = content;
        await _response.WriteStringAsync(content, cancellationToken);
    }

    public void AddHeader(string key, string value)
    {
        _response.Headers.Add(key, value);
    }

    /// <summary>
    /// Gets the underlying Azure Functions HttpResponseData
    /// </summary>
    public HttpResponseData GetUnderlyingResponse() => _response;
}
