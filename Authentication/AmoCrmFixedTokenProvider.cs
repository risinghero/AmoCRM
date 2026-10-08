using System;
using System.Threading;
using System.Threading.Tasks;

namespace FT.AmoCRM.Authentication
{
    /// <summary>Provides a pre-issued integration token without attempting to refresh it.</summary>
    public sealed class AmoCrmFixedTokenProvider : IAmoCrmAccessTokenProvider
    {
        private readonly string _accessToken;

        /// <summary>Creates a provider for a fixed amoCRM integration token.</summary>
        /// <param name="accessToken">The token issued by amoCRM.</param>
        public AmoCrmFixedTokenProvider(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Access token is required.", nameof(accessToken));
            _accessToken = accessToken;
        }

        /// <summary>Returns the configured token unchanged.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The configured access token.</returns>
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_accessToken);
        }
    }
}
