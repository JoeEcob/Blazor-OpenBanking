namespace TrueLayer.API.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class TLAccount
    {
        [JsonPropertyName("update_timestamp")]
        public DateTime UpdateTimeStamp { get; set; }

        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("account_type")]
        public string AccountType { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("account_number")]
        public object AccountNumber { get; set; } // TODO

        [JsonPropertyName("provider")]
        public object Provider { get; set; } // TODO
    }
}
