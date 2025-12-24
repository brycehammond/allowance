using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace AllowanceTracker.Azure.Adapter.Http;

/// <summary>
/// Azure Functions implementation of IHttpRequest
/// Wraps HttpRequestData to provide cloud-agnostic HTTP request abstraction
/// </summary>
public class AzureFunctionsHttpRequest : IHttpRequest
{
    private readonly HttpRequestData _request;
    private Dictionary<string, string>? _routeValues;

    public AzureFunctionsHttpRequest(HttpRequestData request, Dictionary<string, string>? routeValues = null)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _routeValues = routeValues ?? new Dictionary<string, string>();
    }

    public string Method => _request.Method;

    public Uri Url => _request.Url;

    public IDictionary<string, string> Headers
    {
        get
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in _request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }
            return headers;
        }
    }

    public IDictionary<string, string> Query
    {
        get
        {
            var query = new Dictionary<string, string>();
            var queryString = _request.Url.Query;

            if (!string.IsNullOrEmpty(queryString))
            {
                var pairs = queryString.TrimStart('?').Split('&');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        query[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                    }
                }
            }

            return query;
        }
    }

    public IDictionary<string, string> RouteValues => _routeValues ?? new Dictionary<string, string>();

    public Stream Body => _request.Body;

    public async Task<T?> ReadFromJsonAsync<T>(CancellationToken cancellationToken = default)
    {
        if (_request.Body == null || _request.Body.Length == 0)
        {
            return default;
        }

        _request.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(_request.Body, cancellationToken: cancellationToken);
    }

    public string? GetQueryParameter(string key)
    {
        return Query.TryGetValue(key, out var value) ? value : null;
    }

    public string? GetRouteValue(string key)
    {
        return RouteValues.TryGetValue(key, out var value) ? value : null;
    }

    public Guid? GetRouteGuid(string key)
    {
        var value = GetRouteValue(key);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    public string? GetHeader(string key)
    {
        return Headers.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Sets route values - useful for extracting route parameters in Azure Functions
    /// </summary>
    public void SetRouteValues(Dictionary<string, string> routeValues)
    {
        _routeValues = routeValues;
    }
}
