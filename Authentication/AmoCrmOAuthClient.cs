using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FT.AmoCRM.Authentication
{
    /// <summary>Implements amoCRM OAuth authorization-code and refresh flows.</summary>
    public sealed class AmoCrmOAuthClient
    {
        /// <summary>Creates an OAuth client.</summary>
        private readonly HttpClient _httpClient;
        private readonly AmoCrmOAuthOptions _options;

        public AmoCrmOAuthClient(HttpClient httpClient, AmoCrmOAuthOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Builds the amoCRM authorization URL.</summary>
        public Uri GetAuthorizationUri(string accountDomain, string state = null)
        {
            var query = "client_id=" + Uri.EscapeDataString(_options.ClientId)
                + "&redirect_uri=" + Uri.EscapeDataString(_options.RedirectUri.ToString())
                + "&response_type=code";
            if (!string.IsNullOrWhiteSpace(state)) query += "&state=" + Uri.EscapeDataString(state);
            return new Uri("https://" + NormalizeDomain(accountDomain) + "/oauth?" + query);
        }

        /// <summary>Exchanges an authorization code for tokens.</summary>
        public Task<AmoCrmToken> ExchangeCodeAsync(string accountDomain, string authorizationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(authorizationCode)) throw new ArgumentException("Authorization code is required.", nameof(authorizationCode));
            return SendTokenRequestAsync(accountDomain, new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = authorizationCode,
                ["redirect_uri"] = _options.RedirectUri.ToString()
            }, cancellationToken);
        }

        /// <summary>Refreshes an access token.</summary>
        public Task<AmoCrmToken> RefreshAsync(string accountDomain, string refreshToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
            return SendTokenRequestAsync(accountDomain, new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["redirect_uri"] = _options.RedirectUri.ToString()
            }, cancellationToken);
        }

        private async Task<AmoCrmToken> SendTokenRequestAsync(string accountDomain, IDictionary<string, string> values, CancellationToken cancellationToken)
        {
            var domain = NormalizeDomain(accountDomain);
            using (var content = new FormUrlEncodedContent(values))
            using (var response = await _httpClient.PostAsync(new Uri("https://" + domain + "/oauth2/access_token"), content, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new AmoCrmAuthenticationException((int)response.StatusCode, body);

                var token = JsonConvert.DeserializeObject<AmoCrmToken>(body);
                if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
                    throw new AmoCrmAuthenticationException((int)response.StatusCode, "The OAuth response did not contain an access token.");
                token.CreatedAt = DateTimeOffset.UtcNow;
                return token;
            }
        }

        private static string NormalizeDomain(string accountDomain)
        {
            if (string.IsNullOrWhiteSpace(accountDomain)) throw new ArgumentException("Account domain is required.", nameof(accountDomain));
            return accountDomain.Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/');
        }
    }

    /// <summary>Represents an OAuth request failure.</summary>
    public sealed class AmoCrmAuthenticationException : Exception
    {
        /// <summary>Creates an authentication exception.</summary>
        public AmoCrmAuthenticationException(int statusCode, string responseBody)
            : base("amoCRM authentication failed with HTTP " + statusCode + ".")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        /// <summary>The HTTP status code.</summary>
        public int StatusCode { get; }
        /// <summary>The raw OAuth response body.</summary>
        public string ResponseBody { get; }
    }
}
