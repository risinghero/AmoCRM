using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FT.AmoCRM.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FT.AmoCRM.Resources
{
    /// <summary>Entity types that expose custom fields.</summary>
    public enum AmoCrmFieldEntity
    {
        Deals,
        Contacts,
        Companies,
        Users
    }

    /// <summary>Describes an amoCRM custom-field definition.</summary>
    public sealed class AmoCrmField
    {
        /// <summary>The field identifier.</summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("sort")]
        public int? Sort { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("entity_type")]
        public string EntityType { get; set; }

        [JsonProperty("is_predefined")]
        public bool? IsPredefined { get; set; }

        [JsonProperty("is_api_only")]
        public bool? IsApiOnly { get; set; }

        [JsonProperty("enums")]
        public IList<AmoCrmFieldEnum> Enums { get; set; }

        [JsonProperty("settings")]
        public IDictionary<string, JToken> Settings { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
    }

    /// <summary>Describes an enum option of a custom field.</summary>
    public sealed class AmoCrmFieldEnum
    {
        /// <summary>The option identifier.</summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("sort")]
        public int? Sort { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>Retrieves custom-field definitions.</summary>
    public sealed class AmoCrmFieldsService
    {
        /// <summary>Creates a fields service.</summary>
        private readonly AmoCrmApiClient _apiClient;

        public AmoCrmFieldsService(AmoCrmApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task<IReadOnlyList<AmoCrmField>> ListAsync(AmoCrmFieldEntity entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _apiClient.GetAllAsync<AmoCrmField>(GetResourceName(entity) + "/custom_fields", cancellationToken);
        }

        public Task<IReadOnlyList<AmoCrmField>> ListDealsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ListAsync(AmoCrmFieldEntity.Deals, cancellationToken);
        }

        public Task<IReadOnlyList<AmoCrmField>> ListContactsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ListAsync(AmoCrmFieldEntity.Contacts, cancellationToken);
        }

        public Task<IReadOnlyList<AmoCrmField>> ListCompaniesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ListAsync(AmoCrmFieldEntity.Companies, cancellationToken);
        }

        public Task<IReadOnlyList<AmoCrmField>> ListUsersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ListAsync(AmoCrmFieldEntity.Users, cancellationToken);
        }

        private static string GetResourceName(AmoCrmFieldEntity entity)
        {
            switch (entity)
            {
                case AmoCrmFieldEntity.Deals: return "leads";
                case AmoCrmFieldEntity.Contacts: return "contacts";
                case AmoCrmFieldEntity.Companies: return "companies";
                case AmoCrmFieldEntity.Users: return "users";
                default: throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unsupported amoCRM field entity.");
            }
        }
    }
}
