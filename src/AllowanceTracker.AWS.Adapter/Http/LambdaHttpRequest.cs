using AllowanceTracker.Serverless.Abstractions.Http;
using Amazon.Lambda.APIGatewayEvents;
using System.Text;
using System.Text.Json;

namespace AllowanceTracker.AWS.Adapter.Http;

/// <summary>
/// AWS Lambda implementation of IHttpRequest
/// Wraps APIGatewayProxyRequest to provide cloud-agnostic HTTP request abstraction
/// </summary>
public class LambdaHttpRequest : IHttpRequest
{
    private readonly APIGatewayProxyRequest _request;
    private Stream? _bodyStream;

    public LambdaHttpRequest(APIGatewayProxyRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public string Method => _request.HttpMethod;

    public Uri Url
    {
        get
        {
            var scheme = _request.Headers?.ContainsKey("X-Forwarded-Proto") == true
                ? _request.Headers["X-Forwarded-Proto"]
                : "https";

            var host = _request.Headers?.ContainsKey("Host") == true
                ? _request.Headers["Host"]
                : "localhost";

            var path = _request.Path ?? "/";

            // Build query string from QueryStringParameters
            var queryString = string.Empty;
            if (_request.QueryStringParameters != null && _request.QueryStringParameters.Any())
            {
                queryString = string.Join("&",
                    _request.QueryStringParameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            }

            var uriBuilder = new UriBuilder(scheme, host)
            {
                Path = path,
                Query = queryString
            };

            return uriBuilder.Uri;
        }
    }

    public IDictionary<string, string> Headers
    {
        get
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_request.Headers != null)
            {
                foreach (var header in _request.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }
            return headers;
        }
    }

    public IDictionary<string, string> Query
    {
        get
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_request.QueryStringParameters != null)
            {
                foreach (var param in _request.QueryStringParameters)
                {
                    query[param.Key] = param.Value;
                }
            }
            return query;
        }
    }

    public IDictionary<string, string> RouteValues
    {
        get
        {
            var route = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_request.PathParameters != null)
            {
                foreach (var param in _request.PathParameters)
                {
                    route[param.Key] = param.Value;
                }
            }
            return route;
        }
    }

    public Stream Body
    {
        get
        {
            if (_bodyStream == null)
            {
                var bodyString = _request.Body ?? string.Empty;

                // Check if body is base64 encoded
                if (_request.IsBase64Encoded)
                {
                    var bytes = Convert.FromBase64String(bodyString);
                    _bodyStream = new MemoryStream(bytes);
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(bodyString);
                    _bodyStream = new MemoryStream(bytes);
                }
            }

            _bodyStream.Position = 0;
            return _bodyStream;
        }
    }

    public async Task<T?> ReadFromJsonAsync<T>(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_request.Body))
        {
            return default;
        }

        Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(Body, cancellationToken: cancellationToken);
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
}
