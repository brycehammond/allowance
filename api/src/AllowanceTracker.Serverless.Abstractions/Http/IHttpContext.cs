using System.Net;

namespace AllowanceTracker.Serverless.Abstractions.Http;

/// <summary>
/// Cloud-agnostic HTTP context containing request and response
/// Provides factory methods for creating responses
/// </summary>
public interface IHttpContext
{
    /// <summary>
    /// The HTTP request
    /// </summary>
    IHttpRequest Request { get; }

    /// <summary>
    /// Create a new HTTP response with specified status code
    /// </summary>
    IHttpResponse CreateResponse(HttpStatusCode statusCode = HttpStatusCode.OK);

    /// <summary>
    /// Create OK (200) JSON response
    /// </summary>
    Task<IHttpResponse> CreateOkResponseAsync<T>(T data);

    /// <summary>
    /// Create Created (201) JSON response
    /// </summary>
    Task<IHttpResponse> CreateCreatedResponseAsync<T>(T data);

    /// <summary>
    /// Create Bad Request (400) response with error
    /// </summary>
    Task<IHttpResponse> CreateBadRequestResponseAsync(string code, string message);

    /// <summary>
    /// Create Unauthorized (401) response
    /// </summary>
    Task<IHttpResponse> CreateUnauthorizedResponseAsync(string message = "Unauthorized");

    /// <summary>
    /// Create Forbidden (403) response
    /// </summary>
    Task<IHttpResponse> CreateForbiddenResponseAsync(string message = "Forbidden");

    /// <summary>
    /// Create Not Found (404) response
    /// </summary>
    Task<IHttpResponse> CreateNotFoundResponseAsync(string message = "Resource not found");

    /// <summary>
    /// Create Internal Server Error (500) response
    /// </summary>
    Task<IHttpResponse> CreateServerErrorResponseAsync(string message = "An internal server error occurred");

    /// <summary>
    /// Create No Content (204) response
    /// </summary>
    IHttpResponse CreateNoContentResponse();
}
