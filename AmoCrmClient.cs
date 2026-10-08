using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM.Authentication;
using FT.AmoCRM.Http;
using FT.AmoCRM.Resources;

namespace FT.AmoCRM
{
    /// <summary>Entry point for authenticated amoCRM API operations.</summary>
    public sealed class AmoCrmClient
    {
        private readonly string _accountDomain;

        /// <summary>Creates a client for an amoCRM account.</summary>
        public AmoCrmClient(HttpClient httpClient, string accountDomain, AmoCrmToken token, AmoCrmOAuthOptions oauthOptions, AmoCrmClientOptions clientOptions = null)
        {
            if (oauthOptions == null) throw new ArgumentNullException(nameof(oauthOptions));
            if (string.IsNullOrWhiteSpace(accountDomain)) throw new ArgumentException("Account domain is required.", nameof(accountDomain));
            _accountDomain = accountDomain;
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            OAuth = new AmoCrmOAuthClient(HttpClient, oauthOptions);
            TokenProvider = new AmoCrmTokenProvider(accountDomain, token, OAuth);
            AccessTokenProvider = TokenProvider;
            Api = new AmoCrmApiClient(HttpClient, accountDomain, AccessTokenProvider, clientOptions);
            InitializeServices();
        }

        /// <summary>Creates a client using a pre-issued integration token without OAuth refresh.</summary>
        /// <param name="httpClient">The HTTP client used for requests.</param>
        /// <param name="accountDomain">The amoCRM account domain.</param>
        /// <param name="fixedAccessToken">The integration token issued by amoCRM.</param>
        /// <param name="clientOptions">Optional API retry settings.</param>
        public AmoCrmClient(HttpClient httpClient, string accountDomain, string fixedAccessToken, AmoCrmClientOptions clientOptions = null)
        {
            if (string.IsNullOrWhiteSpace(accountDomain)) throw new ArgumentException("Account domain is required.", nameof(accountDomain));
            _accountDomain = accountDomain;
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            OAuth = null;
            TokenProvider = null;
            AccessTokenProvider = new AmoCrmFixedTokenProvider(fixedAccessToken);
            Api = new AmoCrmApiClient(HttpClient, accountDomain, AccessTokenProvider, clientOptions);
            InitializeServices();
        }

        /// <summary>The HTTP client used for requests.</summary>
        public HttpClient HttpClient { get; }
        /// <summary>The OAuth client.</summary>
        public AmoCrmOAuthClient OAuth { get; }
        /// <summary>The token provider used by API requests.</summary>
        public AmoCrmTokenProvider TokenProvider { get; }
        /// <summary>The access-token provider used by API requests in either authentication mode.</summary>
        public IAmoCrmAccessTokenProvider AccessTokenProvider { get; }
        /// <summary>The low-level authenticated API client.</summary>
        public AmoCrmApiClient Api { get; }
        /// <summary>The deals service.</summary>
        public AmoCrmDealsService Deals { get; private set; }
        /// <summary>The contacts service.</summary>
        public AmoCrmContactsService Contacts { get; private set; }
        /// <summary>The companies service.</summary>
        public AmoCrmCompaniesService Companies { get; private set; }
        /// <summary>The users service.</summary>
        public AmoCrmUsersService Users { get; private set; }
        /// <summary>The pipelines service.</summary>
        public AmoCrmPipelinesService Pipelines { get; private set; }
        /// <summary>The webhook management service.</summary>
        public AmoCrmWebhooksService Webhooks { get; private set; }
        /// <summary>The custom-field service.</summary>
        public AmoCrmFieldsService Fields { get; private set; }
        /// <summary>The tasks service.</summary>
        public AmoCrmTasksService Tasks { get; private set; }

        /// <summary>Exchanges an authorization code for OAuth tokens and stores the result in the provider.</summary>
        /// <param name="authorizationCode">The code returned by amoCRM.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The received OAuth token.</returns>
        public async Task<AmoCrmToken> AuthorizeAsync(string authorizationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (OAuth == null) throw new InvalidOperationException("Authorization-code flow is unavailable when using a fixed integration token.");
            var token = await OAuth.ExchangeCodeAsync(GetAccountDomain(), authorizationCode, cancellationToken).ConfigureAwait(false);
            TokenProvider.SetToken(token);
            return token;
        }

        private string GetAccountDomain()
        {
            return _accountDomain;
        }

        private void InitializeServices()
        {
            Deals = new AmoCrmDealsService(Api);
            Contacts = new AmoCrmContactsService(Api);
            Companies = new AmoCrmCompaniesService(Api);
            Users = new AmoCrmUsersService(Api);
            Pipelines = new AmoCrmPipelinesService(Api);
            Webhooks = new AmoCrmWebhooksService(Api);
            Fields = new AmoCrmFieldsService(Api);
            Tasks = new AmoCrmTasksService(Api);
        }
    }
}
