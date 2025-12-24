using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace AllowanceTracker.Functions.Extensions;

/// <summary>
/// Extension methods for creating HTTP responses
/// </summary>
public static class HttpResponseDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Create JSON response with specified status code
    /// </summary>
    public static async Task<HttpResponseData> CreateJsonResponseAsync<T>(
        this HttpRequestData req,
        T data,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(data, JsonOptions));
        return response;
    }

    /// <summary>
    /// Create OK (200) response with JSON data
    /// </summary>
    public static Task<HttpResponseData> CreateOkResponseAsync<T>(this HttpRequestData req, T data)
    {
        return req.CreateJsonResponseAsync(data, HttpStatusCode.OK);
    }

    /// <summary>
    /// Create Created (201) response with JSON data
    /// </summary>
    public static Task<HttpResponseData> CreateCreatedResponseAsync<T>(this HttpRequestData req, T data)
    {
        return req.CreateJsonResponseAsync(data, HttpStatusCode.Created);
    }

    /// <summary>
    /// Create Bad Request (400) response with error details
    /// </summary>
    public static Task<HttpResponseData> CreateBadRequestResponseAsync(
        this HttpRequestData req,
        string code,
        string message)
    {
        return req.CreateJsonResponseAsync(
            new { error = new { code, message } },
            HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Create Unauthorized (401) response
    /// </summary>
    public static Task<HttpResponseData> CreateUnauthorizedResponseAsync(
        this HttpRequestData req,
        string message = "Unauthorized")
    {
        return req.CreateJsonResponseAsync(
            new { error = new { code = "UNAUTHORIZED", message } },
            HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Create Forbidden (403) response
    /// </summary>
    public static Task<HttpResponseData> CreateForbiddenResponseAsync(
        this HttpRequestData req,
        string message = "Forbidden")
    {
        return req.CreateJsonResponseAsync(
            new { error = new { code = "FORBIDDEN", message } },
            HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Create Not Found (404) response
    /// </summary>
    public static Task<HttpResponseData> CreateNotFoundResponseAsync(
        this HttpRequestData req,
        string message = "Resource not found")
    {
        return req.CreateJsonResponseAsync(
            new { error = new { code = "NOT_FOUND", message } },
            HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Create Internal Server Error (500) response
    /// </summary>
    public static Task<HttpResponseData> CreateServerErrorResponseAsync(
        this HttpRequestData req,
        string message = "An internal server error occurred")
    {
        return req.CreateJsonResponseAsync(
            new { error = new { code = "INTERNAL_ERROR", message } },
            HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Create No Content (204) response
    /// </summary>
    public static HttpResponseData CreateNoContentResponse(this HttpRequestData req)
    {
        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
