using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("oauth")]
public class OAuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IOauthAuthorizationService _oauth;
    private readonly GrafanaSettings _settings;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IAuthService authService,
        IOauthAuthorizationService oauth,
        IOptions<GrafanaSettings> settings,
        ILogger<OAuthController> logger)
    {
        _authService = authService;
        _oauth = oauth;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [AllowAnonymous]
    public IActionResult Authorize(
        [FromQuery] string? client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? state,
        [FromQuery] string? code_challenge)
    {
        if (!_settings.Enabled)
        {
            return StatusCode(503, "Grafana OAuth is disabled.");
        }

        var error = ValidateAuthorize(client_id, redirect_uri, response_type);
        if (error is not null)
        {
            return Content(LoginPage(redirect_uri, state, code_challenge, error), "text/html; charset=utf-8");
        }

        return Content(LoginPage(redirect_uri!, state, code_challenge, null), "text/html; charset=utf-8");
    }

    [HttpPost("authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthorizePost(
        [FromForm] string? email,
        [FromForm] string? password,
        [FromForm] string? redirect_uri,
        [FromForm] string? state,
        [FromForm] string? code_challenge,
        CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            return StatusCode(503, "Grafana OAuth is disabled.");
        }

        if (string.IsNullOrWhiteSpace(redirect_uri) || !_oauth.IsAllowedRedirectUri(redirect_uri))
        {
            return Content(LoginPage(redirect_uri, state, code_challenge, "Geçersiz Grafana yönlendirme adresi."), "text/html; charset=utf-8");
        }

        try
        {
            var login = await _authService.LoginAsync(new LoginRequest(email ?? "", password ?? ""), cancellationToken);
            var code = _oauth.CreateAuthorizationCode(login.UserId, redirect_uri, code_challenge);
            var location = $"{redirect_uri}{(redirect_uri.Contains('?', StringComparison.Ordinal) ? "&" : "?")}code={Uri.EscapeDataString(code)}";
            if (!string.IsNullOrEmpty(state))
            {
                location += $"&state={Uri.EscapeDataString(state)}";
            }

            return Redirect(location);
        }
        catch (UnauthorizedException exception)
        {
            return Content(LoginPage(redirect_uri, state, code_challenge, exception.Message), "text/html; charset=utf-8");
        }
        catch (ForbiddenException exception)
        {
            return Content(LoginPage(redirect_uri, state, code_challenge, exception.Message), "text/html; charset=utf-8");
        }
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromForm] OAuthTokenForm form, CancellationToken cancellationToken)
    {
        if (!ClientMatches(form.client_id, form.client_secret))
        {
            return Unauthorized(new { error = "invalid_client" });
        }

        if (!string.Equals(form.grant_type, "authorization_code", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        try
        {
            var auth = await _oauth.ExchangeCodeAsync(
                form.code ?? "",
                form.redirect_uri ?? "",
                form.code_verifier,
                cancellationToken);
            var expiresIn = Math.Max(60, (int)(auth.ExpiresAt - DateTime.UtcNow).TotalSeconds);
            return Json(new
            {
                access_token = auth.Token,
                token_type = "Bearer",
                expires_in = expiresIn
            });
        }
        catch (UnauthorizedException exception)
        {
            _logger.LogWarning("Grafana OAuth token rejected: {Message}", exception.Message);
            return BadRequest(new { error = "invalid_grant", error_description = exception.Message });
        }
    }

    [HttpGet("userinfo")]
    [Authorize]
    public async Task<IActionResult> UserInfo(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var info = await _oauth.GetUserInfoAsync(userId, cancellationToken);
        return Json(new
        {
            sub = info.Sub,
            email = info.Email,
            email_verified = true,
            name = info.Name,
            login = info.Login,
            role = info.Role,
            grafanaOrg = info.GrafanaOrg,
            orgs = info.Orgs
        });
    }

    [HttpGet("userinfo/emails")]
    [Authorize]
    public async Task<IActionResult> UserEmails(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            var info = await _oauth.GetUserInfoAsync(userId, cancellationToken);
            email = info.Email;
        }

        return Json(new[]
        {
            new
            {
                email,
                primary = true,
                verified = true
            }
        });
    }

    private string? ValidateAuthorize(string? clientId, string? redirectUri, string? responseType)
    {
        if (!string.Equals(clientId, _settings.ClientId, StringComparison.Ordinal))
        {
            return "Geçersiz OAuth istemcisi.";
        }

        if (string.IsNullOrWhiteSpace(redirectUri) || !_oauth.IsAllowedRedirectUri(redirectUri))
        {
            return "Geçersiz Grafana yönlendirme adresi.";
        }

        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
        {
            return "Yalnızca authorization_code desteklenir.";
        }

        return null;
    }

    private bool ClientMatches(string? clientId, string? clientSecret)
    {
        var header = Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(header, out var parsed)
            && string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(parsed.Parameter))
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
            var separator = decoded.IndexOf(':');
            if (separator > 0)
            {
                clientId = decoded[..separator];
                clientSecret = decoded[(separator + 1)..];
            }
        }

        return string.Equals(clientId, _settings.ClientId, StringComparison.Ordinal)
            && string.Equals(clientSecret, _settings.ClientSecret, StringComparison.Ordinal);
    }

    private static string LoginPage(string? redirectUri, string? state, string? codeChallenge, string? error)
    {
        var errorHtml = string.IsNullOrWhiteSpace(error)
            ? ""
            : $"<p class=\"error\">{System.Net.WebUtility.HtmlEncode(error)}</p>";

        return $$"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>PulseGrid · Grafana girişi</title>
              <style>
                body { font-family: Segoe UI, sans-serif; background: #f4f1ea; color: #1f1b16; margin: 0; }
                main { max-width: 24rem; margin: 12vh auto; background: #fff; padding: 2rem; border-radius: 1rem; }
                h1 { font-size: 1.25rem; margin: 0 0 .25rem; }
                p.sub { color: #6b645b; margin: 0 0 1.25rem; }
                label { display: block; font-size: .85rem; margin: .75rem 0 .3rem; }
                input { width: 100%; box-sizing: border-box; padding: .65rem .7rem; border: 1px solid #d9d3c9; border-radius: .5rem; }
                button { margin-top: 1.1rem; width: 100%; border: 0; background: #1f6feb; color: #fff; padding: .7rem; border-radius: .5rem; font-weight: 600; cursor: pointer; }
                .error { color: #b42318; background: #fde8e6; padding: .6rem .75rem; border-radius: .5rem; }
              </style>
            </head>
            <body>
              <main>
                <h1>PulseGrid</h1>
                <p class="sub">Grafana için şirket hesabınızla giriş yapın.</p>
                {{errorHtml}}
                <form method="post" action="/oauth/authorize">
                  <input type="hidden" name="redirect_uri" value="{{System.Net.WebUtility.HtmlEncode(redirectUri ?? "")}}" />
                  <input type="hidden" name="state" value="{{System.Net.WebUtility.HtmlEncode(state ?? "")}}" />
                  <input type="hidden" name="code_challenge" value="{{System.Net.WebUtility.HtmlEncode(codeChallenge ?? "")}}" />
                  <label>E-posta</label>
                  <input name="email" type="email" autocomplete="username" required />
                  <label>Şifre</label>
                  <input name="password" type="password" autocomplete="current-password" required />
                  <button type="submit">Grafana'ya devam et</button>
                </form>
              </main>
            </body>
            </html>
            """;
    }

    public sealed class OAuthTokenForm
    {
        public string? grant_type { get; set; }
        public string? code { get; set; }
        public string? redirect_uri { get; set; }
        public string? client_id { get; set; }
        public string? client_secret { get; set; }
        public string? code_verifier { get; set; }
    }
}
