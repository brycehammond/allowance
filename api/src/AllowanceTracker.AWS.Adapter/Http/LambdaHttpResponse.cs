using AllowanceTracker.Serverless.Abstractions.Http;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using System.Text.Json;

namespace AllowanceTracker.AWS.Adapter.Http;

/// <summary>
/// AWS Lambda implementation of IHttpResponse
/// Builds APIGatewayProxyResponse to provide cloud-agnostic HTTP response abstraction
/// </summary>
public class LambdaHttpResponse : IHttpResponse
{
    private readonly Dictionary<string, string> _headers;
    private HttpStatusCode _statusCode;
    private string? _body;

    public LambdaHttpResponse()
    {
        _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Content-Type", "application/json" }
        };
        _statusCode = HttpStatusCode.OK;
    }

    public HttpStatusCode StatusCode
    {
        get => _statusCode;
        set => _statusCode = value;
    }

    public IDictionary<string, string> Headers => _headers;

    public string? Body
    {
        get => _body;
        set => _body = value;
    }

    public async Task WriteAsJsonAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        _headers["Content-Type"] = "application/json";
        _body = JsonSerializer.Serialize(data);
        await Task.CompletedTask;
    }

    public async Task WriteStringAsync(string content, CancellationToken cancellationToken = default)
    {
        _body = content;
        await Task.CompletedTask;
    }

    public void AddHeader(string key, string value)
    {
        _headers[key] = value;
    }

    /// <summary>
    /// Builds the APIGatewayProxyResponse for Lambda
    /// </summary>
    public APIGatewayProxyResponse BuildLambdaResponse()
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)_statusCode,
            Headers = new Dictionary<string, string>(_headers),
            Body = _body ?? string.Empty,
            IsBase64Encoded = false
        };
    }
}
