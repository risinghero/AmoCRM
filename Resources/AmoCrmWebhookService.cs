using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FT.AmoCRM.Resources
{
    /// <summary>Represents a registered amoCRM webhook.</summary>
    public sealed class AmoCrmWebhook
    {
        /// <summary>The webhook identifier.</summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("settings")]
        public IDictionary<string, bool> Settings { get; set; } = new Dictionary<string, bool>();

        [JsonProperty("created_at")]
        public long? CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public long? UpdatedAt { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
    }

    /// <summary>Request data for registering or updating a webhook.</summary>
    public sealed class AmoCrmWebhookRequest
    {
        /// <summary>The destination URL.</summary>
        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("settings")]
        public IDictionary<string, bool> Settings { get; set; } = new Dictionary<string, bool>();
    }

    /// <summary>Manages amoCRM webhook registrations.</summary>
    public sealed class AmoCrmWebhooksService
    {
        /// <summary>Creates a webhook service.</summary>
        private const string ResourceName = "webhooks";
        private readonly AmoCrmApiClient _apiClient;

        public AmoCrmWebhooksService(AmoCrmApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task<IReadOnlyList<AmoCrmWebhook>> ListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _apiClient.GetAllAsync<AmoCrmWebhook>(ResourceName, cancellationToken);
        }

        public Task<AmoCrmWebhook> GetAsync(long id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _apiClient.SendAsync<AmoCrmWebhook>(HttpMethod.Get, ResourceName + "/" + id, null, cancellationToken);
        }

        public Task<AmoCrmWebhook> RegisterAsync(AmoCrmWebhookRequest webhook, CancellationToken cancellationToken = default(CancellationToken))
        {
            Validate(webhook);
            return _apiClient.SendAsync<AmoCrmWebhook>(HttpMethod.Post, ResourceName, webhook, cancellationToken);
        }

        public Task<AmoCrmWebhook> UpdateAsync(long id, AmoCrmWebhookRequest webhook, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            Validate(webhook);
            return _apiClient.SendAsync<AmoCrmWebhook>(new HttpMethod("PATCH"), ResourceName + "/" + id, webhook, cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            await _apiClient.SendAsync<string>(HttpMethod.Delete, ResourceName + "/" + id, null, cancellationToken).ConfigureAwait(false);
        }

        private static void Validate(AmoCrmWebhookRequest webhook)
        {
            if (webhook == null) throw new ArgumentNullException(nameof(webhook));
            if (string.IsNullOrWhiteSpace(webhook.Destination)) throw new ArgumentException("Webhook destination is required.", nameof(webhook));
            if (!Uri.TryCreate(webhook.Destination, UriKind.Absolute, out var destination)
                || (destination.Scheme != Uri.UriSchemeHttp && destination.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Webhook destination must be an absolute HTTP or HTTPS URL.", nameof(webhook));
        }
    }
}
