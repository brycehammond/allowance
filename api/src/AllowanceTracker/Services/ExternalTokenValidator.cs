using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AllowanceTracker.Services;

public class ExternalTokenValidator : IExternalTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalTokenValidator> _logger;

    private static readonly ConfigurationManager<OpenIdConnectConfiguration> _appleConfigManager =
        new("https://appleid.apple.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

    public ExternalTokenValidator(
        IConfiguration configuration,
        ILogger<ExternalTokenValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ExternalTokenInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var webClientId = _configuration["ExternalAuth:Google:ClientId"];
            var iosClientId = _configuration["ExternalAuth:Google:IosClientId"];

            var validAudiences = new List<string>();
            if (!string.IsNullOrEmpty(webClientId)) validAudiences.Add(webClientId);
            if (!string.IsNullOrEmpty(iosClientId)) validAudiences.Add(iosClientId);

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = validAudiences
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new ExternalTokenInfo(
                ProviderKey: payload.Subject,
                Email: payload.Email,
                FirstName: payload.GivenName,
                LastName: payload.FamilyName);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token");
            return null;
        }
    }

    public async Task<ExternalTokenInfo?> ValidateAppleTokenAsync(string idToken)
    {
        try
        {
            var bundleId = _configuration["ExternalAuth:Apple:BundleId"];
            var serviceId = _configuration["ExternalAuth:Apple:ServiceId"];

            var validAudiences = new List<string>();
            if (!string.IsNullOrEmpty(bundleId)) validAudiences.Add(bundleId);
            if (!string.IsNullOrEmpty(serviceId)) validAudiences.Add(serviceId);

            var openIdConfig = await _appleConfigManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://appleid.apple.com",
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidAudiences = validAudiences,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(idToken, validationParameters, out var validatedToken);

            var sub = principal.FindFirst("sub")?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst("email")?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Apple ID token missing required claims (sub or email)");
                return null;
            }

            // Apple doesn't include name in the ID token — it's only provided
            // in the authorization response on first sign-in, so clients must
            // send it separately via the DTO
            return new ExternalTokenInfo(
                ProviderKey: sub,
                Email: email,
                FirstName: null,
                LastName: null);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid Apple ID token");
            return null;
        }
    }
}
