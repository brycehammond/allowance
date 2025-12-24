using System.Net;

namespace AllowanceTracker.Serverless.Abstractions.Http;

/// <summary>
/// Cloud-agnostic HTTP response abstraction
/// Works with Azure Functions HttpResponseData, AWS Lambda APIGatewayProxyResponse, and GCP HttpResponse
/// </summary>
public interface IHttpResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Response headers
    /// </summary>
    IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Response body
    /// </summary>
    string? Body { get; set; }

    /// <summary>
    /// Set JSON response body
    /// </summary>
    Task WriteAsJsonAsync<T>(T data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write string to response body
    /// </summary>
    Task WriteStringAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add header to response
    /// </summary>
    void AddHeader(string key, string value);
}
