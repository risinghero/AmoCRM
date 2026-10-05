using System;

namespace FT.AmoCRM.Authentication
{
    /// <summary>OAuth application credentials and callback configuration.</summary>
    public sealed class AmoCrmOAuthOptions
    {
        /// <summary>Creates OAuth application options.</summary>
        public AmoCrmOAuthOptions(string clientId, string clientSecret, Uri redirectUri)
        {
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID is required.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException("Client secret is required.", nameof(clientSecret));
            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
        }

        /// <summary>The OAuth client identifier.</summary>
        public string ClientId { get; }
        /// <summary>The OAuth client secret.</summary>
        public string ClientSecret { get; }
        /// <summary>The registered OAuth redirect URI.</summary>
        public Uri RedirectUri { get; }
    }
}
