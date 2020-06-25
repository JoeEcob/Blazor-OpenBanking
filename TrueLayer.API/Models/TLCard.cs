namespace TrueLayer.API.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class TLCard
    {
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("card_network")]
        public string CardNetwork { get; set; }

        [JsonPropertyName("cart_type")]
        public string CardType { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("partial_card_number")]
        public string PartialCardNumber { get; set; }

        [JsonPropertyName("name_on_card")]
        public string NameOnCard { get; set; }

        [JsonPropertyName("valid_from")]
        public string ValidFrom { get; set; }

        [JsonPropertyName("valid_to")]
        public string ValidTo { get; set; }

        [JsonPropertyName("update_timestamp")]
        public DateTime UpdateTimestamp { get; set; }

        [JsonPropertyName("provider")]
        public TLProvider Provider { get; set; }

        [JsonIgnore]
        public TLCardBalance Balance { get; set; }
    }
}
