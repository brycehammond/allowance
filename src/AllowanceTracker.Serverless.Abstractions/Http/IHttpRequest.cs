namespace AllowanceTracker.Serverless.Abstractions.Http;

/// <summary>
/// Cloud-agnostic HTTP request abstraction
/// Works with Azure Functions HttpRequestData, AWS Lambda APIGatewayProxyRequest, and GCP HttpRequest
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Request URL
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Request headers
    /// </summary>
    IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Query string parameters
    /// </summary>
    IDictionary<string, string> Query { get; }

    /// <summary>
    /// Route parameters (from URL path)
    /// </summary>
    IDictionary<string, string> RouteValues { get; }

    /// <summary>
    /// Request body stream
    /// </summary>
    Stream Body { get; }

    /// <summary>
    /// Read and deserialize JSON body
    /// </summary>
    Task<T?> ReadFromJsonAsync<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get query parameter value
    /// </summary>
    string? GetQueryParameter(string key);

    /// <summary>
    /// Get route parameter value
    /// </summary>
    string? GetRouteValue(string key);

    /// <summary>
    /// Get route parameter as Guid
    /// </summary>
    Guid? GetRouteGuid(string key);

    /// <summary>
    /// Get header value
    /// </summary>
    string? GetHeader(string key);
}
