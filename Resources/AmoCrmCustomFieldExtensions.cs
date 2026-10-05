using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FT.AmoCRM.Resources
{
    public static class AmoCrmCustomFieldExtensions
    {
        /// <summary>Sets a scalar custom-field value.</summary>
        /// <typeparam name="T">The CLR type of the value.</typeparam>
        /// <param name="entity">The amoCRM entity to modify.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <param name="value">The value to send to amoCRM.</param>
        public static void SetCustomField<T>(this AmoCrmEntity entity, long fieldId, T value)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (fieldId <= 0) throw new ArgumentOutOfRangeException(nameof(fieldId));

            var field = GetOrCreateField(entity, fieldId);
            field.Values = new List<AmoCrmCustomFieldValueItem>
            {
                new AmoCrmCustomFieldValueItem { Value = JToken.FromObject(ToApiValue(value)) }
            };
        }

        /// <summary>Gets the first value of a custom field or the default value when it is not present.</summary>
        /// <typeparam name="T">The expected CLR type of the value.</typeparam>
        /// <param name="entity">The entity containing the field.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        public static T GetCustomField<T>(this AmoCrmEntity entity, long fieldId)
        {
            AmoCrmCustomFieldValueItem item;
            if (!TryGetCustomFieldValue(entity, fieldId, out item) || item.Value == null || item.Value.Type == JTokenType.Null)
                return default(T);
            return item.Value.ToObject<T>();
        }

        /// <summary>Tries to read the first value of a custom field.</summary>
        /// <typeparam name="T">The expected CLR type of the value.</typeparam>
        /// <param name="entity">The entity containing the field.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <param name="value">The converted value when present.</param>
        /// <returns><see langword="true"/> when a value exists; otherwise <see langword="false"/>.</returns>
        public static bool TryGetCustomField<T>(this AmoCrmEntity entity, long fieldId, out T value)
        {
            AmoCrmCustomFieldValueItem item;
            if (!TryGetCustomFieldValue(entity, fieldId, out item) || item.Value == null || item.Value.Type == JTokenType.Null)
            {
                value = default(T);
                return false;
            }

            value = item.Value.ToObject<T>();
            return true;
        }

        /// <summary>Sets a date custom field as a Unix timestamp.</summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <param name="value">The date and time value.</param>
        public static void SetDateField(this AmoCrmEntity entity, long fieldId, DateTimeOffset value)
        {
            entity.SetCustomField(fieldId, value.ToUnixTimeSeconds());
        }

        /// <summary>Reads a Unix-timestamp custom field as a date and time.</summary>
        /// <param name="entity">The entity containing the field.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <returns>The date value, or <see langword="null"/> when the field is absent.</returns>
        public static DateTimeOffset? GetDateField(this AmoCrmEntity entity, long fieldId)
        {
            long timestamp;
            return entity.TryGetCustomField(fieldId, out timestamp)
                ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(timestamp)
                : null;
        }

        /// <summary>Sets a single-select enum custom field by identifier or code.</summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <param name="enumId">The selected enum identifier.</param>
        /// <param name="enumCode">The selected enum code.</param>
        public static void SetEnumField(this AmoCrmEntity entity, long fieldId, long? enumId = null, string enumCode = null)
        {
            if (enumId == null && string.IsNullOrWhiteSpace(enumCode))
                throw new ArgumentException("Either enumId or enumCode is required.");

            var field = GetOrCreateField(entity, fieldId);
            field.Values = new List<AmoCrmCustomFieldValueItem>
            {
                new AmoCrmCustomFieldValueItem { EnumId = enumId, EnumCode = enumCode }
            };
        }

        /// <summary>Sets a multi-select enum custom field by enum identifiers.</summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <param name="enumIds">The selected enum identifiers.</param>
        public static void SetMultiEnumField(this AmoCrmEntity entity, long fieldId, IEnumerable<long> enumIds)
        {
            if (enumIds == null) throw new ArgumentNullException(nameof(enumIds));
            var field = GetOrCreateField(entity, fieldId);
            field.Values = enumIds.Select(id => new AmoCrmCustomFieldValueItem { EnumId = id }).ToList();
        }

        /// <summary>Removes a custom field value from the entity payload.</summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="fieldId">The custom-field identifier.</param>
        /// <returns><see langword="true"/> when a value was removed.</returns>
        public static bool RemoveCustomField(this AmoCrmEntity entity, long fieldId)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.CustomFieldsValues == null) return false;
            var field = entity.CustomFieldsValues.FirstOrDefault(item => item.FieldId == fieldId);
            return field != null && entity.CustomFieldsValues.Remove(field);
        }

        private static AmoCrmCustomFieldValue GetOrCreateField(AmoCrmEntity entity, long fieldId)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (fieldId <= 0) throw new ArgumentOutOfRangeException(nameof(fieldId));
            if (entity.CustomFieldsValues == null)
                entity.CustomFieldsValues = new List<AmoCrmCustomFieldValue>();

            var field = entity.CustomFieldsValues.FirstOrDefault(item => item.FieldId == fieldId);
            if (field != null) return field;
            field = new AmoCrmCustomFieldValue { FieldId = fieldId, Values = new List<AmoCrmCustomFieldValueItem>() };
            entity.CustomFieldsValues.Add(field);
            return field;
        }

        private static bool TryGetCustomFieldValue(AmoCrmEntity entity, long fieldId, out AmoCrmCustomFieldValueItem item)
        {
            item = null;
            if (entity == null || entity.CustomFieldsValues == null) return false;
            var field = entity.CustomFieldsValues.FirstOrDefault(value => value.FieldId == fieldId);
            if (field == null || field.Values == null || field.Values.Count == 0) return false;
            item = field.Values[0];
            return item != null;
        }

        private static object ToApiValue<T>(T value)
        {
            if (value == null) return null;
            if (value is DateTimeOffset) return ((DateTimeOffset)(object)value).ToUnixTimeSeconds();
            if (value is DateTime) return new DateTimeOffset((DateTime)(object)value).ToUnixTimeSeconds();
            return value;
        }
    }
}