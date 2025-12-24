using AllowanceTracker.Serverless.Abstractions.Http;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace AllowanceTracker.AWS.Adapter.Http;

/// <summary>
/// AWS Lambda implementation of IHttpContext
/// Provides cloud-agnostic HTTP context with request/response abstraction
/// </summary>
public class LambdaHttpContext : IHttpContext
{
    private readonly LambdaHttpRequest _request;

    public LambdaHttpContext(APIGatewayProxyRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _request = new LambdaHttpRequest(request);
    }

    public IHttpRequest Request => _request;

    public IHttpResponse CreateResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new LambdaHttpResponse
        {
            StatusCode = statusCode
        };
    }

    public async Task<IHttpResponse> CreateOkResponseAsync<T>(T data)
    {
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.OK };
        await response.WriteAsJsonAsync(data);
        return response;
    }

    public async Task<IHttpResponse> CreateCreatedResponseAsync<T>(T data)
    {
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.Created };
        await response.WriteAsJsonAsync(data);
        return response;
    }

    public async Task<IHttpResponse> CreateBadRequestResponseAsync(string code, string message)
    {
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.BadRequest };
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
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.Unauthorized };
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
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.Forbidden };
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
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.NotFound };
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
        var response = new LambdaHttpResponse { StatusCode = HttpStatusCode.InternalServerError };
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
        return new LambdaHttpResponse { StatusCode = HttpStatusCode.NoContent };
    }
}
