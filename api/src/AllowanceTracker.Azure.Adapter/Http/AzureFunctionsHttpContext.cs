using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace AllowanceTracker.Azure.Adapter.Http;

/// <summary>
/// Azure Functions implementation of IHttpContext
/// Provides cloud-agnostic HTTP context with request/response abstraction
/// </summary>
public class AzureFunctionsHttpContext : IHttpContext
{
    private readonly HttpRequestData _requestData;
    private readonly AzureFunctionsHttpRequest _request;

    public AzureFunctionsHttpContext(HttpRequestData requestData, Dictionary<string, string>? routeValues = null)
    {
        _requestData = requestData ?? throw new ArgumentNullException(nameof(requestData));
        _request = new AzureFunctionsHttpRequest(requestData, routeValues);
    }

    public IHttpRequest Request => _request;

    public IHttpResponse CreateResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = _requestData.CreateResponse(statusCode);
        return new AzureFunctionsHttpResponse(response);
    }

    public async Task<IHttpResponse> CreateOkResponseAsync<T>(T data)
    {
        var response = CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    public async Task<IHttpResponse> CreateCreatedResponseAsync<T>(T data)
    {
        var response = CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    public async Task<IHttpResponse> CreateBadRequestResponseAsync(string code, string message)
    {
        var response = CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message
            }
        });
        return response;
    }

    public async Task<IHttpResponse> CreateUnauthorizedResponseAsync(string message = "Unauthorized")
    {
        var response = CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "UNAUTHORIZED",
                message
            }
        });
        return response;
    }

    public async Task<IHttpResponse> CreateForbiddenResponseAsync(string message = "Forbidden")
    {
        var response = CreateResponse(HttpStatusCode.Forbidden);
        await response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "FORBIDDEN",
                message
            }
        });
        return response;
    }

    public async Task<IHttpResponse> CreateNotFoundResponseAsync(string message = "Resource not found")
    {
        var response = CreateResponse(HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "NOT_FOUND",
                message
            }
        });
        return response;
    }

    public async Task<IHttpResponse> CreateServerErrorResponseAsync(string message = "An internal server error occurred")
    {
        var response = CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "INTERNAL_SERVER_ERROR",
                message
            }
        });
        return response;
    }

    public IHttpResponse CreateNoContentResponse()
    {
        return CreateResponse(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Sets route values on the request - useful for extracting route parameters
    /// </summary>
    public void SetRouteValues(Dictionary<string, string> routeValues)
    {
        _request.SetRouteValues(routeValues);
    }
}
