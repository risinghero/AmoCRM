using System;
using Newtonsoft.Json;

namespace FT.AmoCRM.Authentication
{
    /// <summary>OAuth access and refresh token information.</summary>
    public sealed class AmoCrmToken
    {
        /// <summary>The access token.</summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonIgnore]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [JsonIgnore]
        public bool IsExpired => string.IsNullOrWhiteSpace(AccessToken) || DateTimeOffset.UtcNow >= CreatedAt.AddSeconds(Math.Max(0, ExpiresIn - 60));
    }
}
