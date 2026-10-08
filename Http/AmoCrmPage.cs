using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FT.AmoCRM.Http
{
    /// <summary>Represents a paginated amoCRM response.</summary>
    public sealed class AmoCrmPage<T>
    {
        /// <summary>The embedded response collections.</summary>
        [JsonProperty("_embedded")]
        public AmoCrmEmbedded<T> Embedded { get; set; }

        [JsonProperty("_links")]
        public AmoCrmLinks Links { get; set; }

        /// <summary>The resource items contained in this response.</summary>
        public IReadOnlyList<T> Items => Embedded?.Leads ?? Embedded?.Contacts ?? Embedded?.Companies
            ?? Embedded?.Users ?? Embedded?.Tasks ?? Embedded?.Pipelines
            ?? Embedded?.Items ?? Embedded?.CustomFields ?? (IReadOnlyList<T>)Array.Empty<T>();
    }

    /// <summary>Collections embedded in an amoCRM response.</summary>
    public sealed class AmoCrmEmbedded<T>
    {
        /// <summary>The standard resource items.</summary>
        [JsonProperty("items")]
        public List<T> Items { get; set; }

        /// <summary>The deals returned by the leads endpoint.</summary>
        [JsonProperty("leads")]
        public List<T> Leads { get; set; }

        /// <summary>The contacts returned by the contacts endpoint.</summary>
        [JsonProperty("contacts")]
        public List<T> Contacts { get; set; }

        /// <summary>The companies returned by the companies endpoint.</summary>
        [JsonProperty("companies")]
        public List<T> Companies { get; set; }

        /// <summary>The users returned by the users endpoint.</summary>
        [JsonProperty("users")]
        public List<T> Users { get; set; }

        /// <summary>The tasks returned by the tasks endpoint.</summary>
        [JsonProperty("tasks")]
        public List<T> Tasks { get; set; }

        /// <summary>The pipelines returned by the pipelines endpoint.</summary>
        [JsonProperty("pipelines")]
        public List<T> Pipelines { get; set; }

        /// <summary>The custom-field definitions returned by the fields endpoint.</summary>
        [JsonProperty("custom_fields")]
        public List<T> CustomFields { get; set; }
    }

    /// <summary>Pagination links.</summary>
    public sealed class AmoCrmLinks
    {
        /// <summary>The next-page link.</summary>
        [JsonProperty("next")]
        public AmoCrmLink Next { get; set; }
    }

    /// <summary>A pagination link.</summary>
    public sealed class AmoCrmLink
    {
        /// <summary>The link URI.</summary>
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }
}
