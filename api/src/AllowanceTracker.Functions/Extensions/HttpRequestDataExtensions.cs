using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace AllowanceTracker.Functions.Extensions;

/// <summary>
/// Extension methods for HttpRequestData to simplify request handling
/// </summary>
public static class HttpRequestDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Read and deserialize JSON body from request
    /// </summary>
    public static async Task<T?> ReadFromJsonAsync<T>(this HttpRequestData req)
    {
        return await JsonSerializer.DeserializeAsync<T>(req.Body, JsonOptions);
    }

    /// <summary>
    /// Get query parameter value
    /// </summary>
    public static string? GetQueryParameter(this HttpRequestData req, string key)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        return query[key];
    }

    /// <summary>
    /// Get route parameter value
    /// </summary>
    public static string? GetRouteValue(this HttpRequestData req, string key)
    {
        if (req.FunctionContext.BindingContext.BindingData.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Parse Guid from route parameter
    /// </summary>
    public static Guid? GetRouteGuid(this HttpRequestData req, string key)
    {
        var value = req.GetRouteValue(key);
        if (value != null && Guid.TryParse(value, out var guid))
        {
            return guid;
        }
        return null;
    }
}
