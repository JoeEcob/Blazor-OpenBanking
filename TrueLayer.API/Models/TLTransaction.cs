namespace TrueLayer.API.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class TLTransaction
    {
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; set; }

        [JsonPropertyName("transaction_category")]
        public string TransactionCategory { get; set; }

        [JsonPropertyName("transaction_classification")]
        public string[] TransactionClassification { get; set; }

        [JsonPropertyName("merchant_name")]
        public string MerchantName { get; set; }

        [JsonPropertyName("running_balance")]
        public object RunningBalance { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}
