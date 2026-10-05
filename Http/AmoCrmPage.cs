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

        public IReadOnlyList<T> Items => Embedded?.Items ?? Embedded?.CustomFields ?? (IReadOnlyList<T>)Array.Empty<T>();
    }

    /// <summary>Collections embedded in an amoCRM response.</summary>
    public sealed class AmoCrmEmbedded<T>
    {
        /// <summary>The standard resource items.</summary>
        [JsonProperty("items")]
        public List<T> Items { get; set; }

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
