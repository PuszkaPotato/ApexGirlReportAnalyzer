using ApexGirlReportAnalyzer.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApexGirlReportAnalyzer.API.Authentication;

/// <summary>
/// Handles API key authentication by validating the X-API-Key header against stored keys in the database.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppDbContext _context;

    /// <inheritdoc />
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext context)
        : base(options, logger, encoder)
    {
        _context = context;
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Validate the API key
        var apiKeyEntity = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == apiKey
                                    && k.IsActive
                                    && k.RevokedAt == null);

        if (apiKeyEntity == null)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // Check if the API key has expired
        if (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("API Key has expired");
        }

        // Create claims and identity
        var claims = new[] { new Claim(ClaimTypes.Name, apiKeyEntity.Name) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Update last used timestamp
        apiKeyEntity.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return AuthenticateResult.Success(ticket);
    }
}
