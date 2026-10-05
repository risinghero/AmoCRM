using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FT.AmoCRM.Http
{
    /// <summary>Builds URL query parameters and amoCRM filter expressions.</summary>
    public sealed class AmoCrmQuery
    {
        /// <summary>Adds an arbitrary query parameter.</summary>
        private readonly IDictionary<string, IList<string>> _parameters = new Dictionary<string, IList<string>>(StringComparer.Ordinal);

        public AmoCrmQuery Add(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name is required.", nameof(name));
            if (value == null) return this;
            IList<string> values;
            if (!_parameters.TryGetValue(name, out values))
            {
                values = new List<string>();
                _parameters.Add(name, values);
            }
            values.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
            return this;
        }

        public AmoCrmQuery Filter(string field, object value)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Filter field is required.", nameof(field));
            return Add("filter[" + field + "][]", value);
        }

        public AmoCrmQuery FilterFrom(string field, object value)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Filter field is required.", nameof(field));
            return Add("filter[" + field + "][from]", value);
        }

        public AmoCrmQuery FilterTo(string field, object value)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Filter field is required.", nameof(field));
            return Add("filter[" + field + "][to]", value);
        }

        public override string ToString()
        {
            return string.Join("&", _parameters.SelectMany(parameter => parameter.Value.Select(value => Uri.EscapeDataString(parameter.Key) + "=" + Uri.EscapeDataString(value))));
        }

        internal string AppendTo(string path)
        {
            var query = ToString();
            if (string.IsNullOrEmpty(query)) return path;
            return path + (path.IndexOf("?", StringComparison.Ordinal) >= 0 ? "&" : "?") + query;
        }
    }
}
