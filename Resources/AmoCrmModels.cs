using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FT.AmoCRM.Resources
{
    /// <summary>Base fields shared by amoCRM entities.</summary>
    public abstract class AmoCrmEntity
    {
        /// <summary>The entity identifier.</summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created_at")]
        public long? CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public long? UpdatedAt { get; set; }

        [JsonProperty("custom_fields_values")]
        public IList<AmoCrmCustomFieldValue> CustomFieldsValues { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
    }

    /// <summary>Represents a deal in the amoCRM API.</summary>
    public sealed class AmoCrmLead : AmoCrmEntity
    {
        /// <summary>The deal price.</summary>
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("responsible_user_id")]
        public long? ResponsibleUserId { get; set; }

        [JsonProperty("group_id")]
        public long? GroupId { get; set; }

        [JsonProperty("status_id")]
        public long? StatusId { get; set; }

        [JsonProperty("pipeline_id")]
        public long? PipelineId { get; set; }

    }

    /// <summary>Represents a contact in the amoCRM API.</summary>
    public sealed class AmoCrmContact : AmoCrmEntity
    {
        /// <summary>The contact first name.</summary>
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("responsible_user_id")]
        public long? ResponsibleUserId { get; set; }

    }

    /// <summary>Represents a company in the amoCRM API.</summary>
    public sealed class AmoCrmCompany : AmoCrmEntity
    {
        /// <summary>The responsible user identifier.</summary>
        [JsonProperty("responsible_user_id")]
        public long? ResponsibleUserId { get; set; }

    }

    /// <summary>Represents an amoCRM user.</summary>
    public sealed class AmoCrmUser : AmoCrmEntity
    {
        /// <summary>The user's email address.</summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("lang")]
        public string Language { get; set; }

        [JsonProperty("is_admin")]
        public bool? IsAdmin { get; set; }

        [JsonProperty("is_active")]
        public bool? IsActive { get; set; }
    }

    /// <summary>Represents an amoCRM task.</summary>
    public sealed class AmoCrmTask : AmoCrmEntity
    {
        /// <summary>The task text.</summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("complete_till")]
        public long? CompleteTill { get; set; }

        [JsonProperty("entity_id")]
        public long? EntityId { get; set; }

        [JsonProperty("entity_type")]
        public string EntityType { get; set; }

        [JsonProperty("task_type_id")]
        public long? TaskTypeId { get; set; }

        [JsonProperty("responsible_user_id")]
        public long? ResponsibleUserId { get; set; }

        [JsonProperty("is_completed")]
        public bool? IsCompleted { get; set; }

        [JsonProperty("result")]
        public JToken Result { get; set; }
    }

    /// <summary>Represents a sales pipeline.</summary>
    public sealed class AmoCrmPipeline : AmoCrmEntity
    {
        /// <summary>The pipeline sort order.</summary>
        [JsonProperty("sort")]
        public int? Sort { get; set; }

        [JsonProperty("is_main")]
        public bool? IsMain { get; set; }

        [JsonProperty("statuses")]
        public IList<AmoCrmPipelineStatus> Statuses { get; set; }
    }

    /// <summary>Represents a pipeline status.</summary>
    public sealed class AmoCrmPipelineStatus : AmoCrmEntity
    {
        /// <summary>The status color.</summary>
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("sort")]
        public int? Sort { get; set; }

        [JsonProperty("pipeline_id")]
        public long? PipelineId { get; set; }
    }

    /// <summary>Represents values belonging to one custom field.</summary>
    public sealed class AmoCrmCustomFieldValue
    {
        /// <summary>The custom-field identifier.</summary>
        [JsonProperty("field_id")]
        public long FieldId { get; set; }

        [JsonProperty("field_name")]
        public string FieldName { get; set; }

        [JsonProperty("field_code")]
        public string FieldCode { get; set; }

        [JsonProperty("values")]
        public IList<AmoCrmCustomFieldValueItem> Values { get; set; } = new List<AmoCrmCustomFieldValueItem>();
    }

    /// <summary>Represents one custom-field value item.</summary>
    public sealed class AmoCrmCustomFieldValueItem
    {
        /// <summary>The scalar value.</summary>
        [JsonProperty("value")]
        public JToken Value { get; set; }

        [JsonProperty("enum_id")]
        public long? EnumId { get; set; }

        [JsonProperty("enum_code")]
        public string EnumCode { get; set; }
    }
}
