using System;
using System.Threading;
using System.Threading.Tasks;

namespace FT.AmoCRM.Authentication
{
    /// <summary>Provides valid access tokens and refreshes them when necessary.</summary>
    public sealed class AmoCrmTokenProvider : IAmoCrmAccessTokenProvider
    {
        /// <summary>Creates a token provider.</summary>
        private readonly AmoCrmOAuthClient _oauthClient;
        private readonly string _accountDomain;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private AmoCrmToken _token;

        public AmoCrmTokenProvider(string accountDomain, AmoCrmToken token, AmoCrmOAuthClient oauthClient)
        {
            _accountDomain = accountDomain ?? throw new ArgumentNullException(nameof(accountDomain));
            _token = token ?? throw new ArgumentNullException(nameof(token));
            _oauthClient = oauthClient ?? throw new ArgumentNullException(nameof(oauthClient));
        }

        /// <summary>Gets the currently stored token.</summary>
        public AmoCrmToken CurrentToken => _token;

        /// <summary>Replaces the currently stored token.</summary>
        public void SetToken(AmoCrmToken token)
        {
            _token = token ?? throw new ArgumentNullException(nameof(token));
        }

        /// <summary>Gets a valid access token, refreshing it when required.</summary>
        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_token.IsExpired) return _token.AccessToken;
            if (string.IsNullOrWhiteSpace(_token.RefreshToken))
                throw new InvalidOperationException("The access token has expired and no refresh token is available.");

            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_token.IsExpired)
                    _token = await _oauthClient.RefreshAsync(_accountDomain, _token.RefreshToken, cancellationToken).ConfigureAwait(false);
                return _token.AccessToken;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
