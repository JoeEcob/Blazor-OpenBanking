namespace TrueLayer.API.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class TLBalance
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("available")]
        public decimal Available { get; set; }

        [JsonPropertyName("current")]
        public decimal Current { get; set; }

        [JsonPropertyName("overdraft")]
        public decimal Overdraft { get; set; }

        [JsonPropertyName("update_timestamp")]
        public DateTime UpdateTimestamp { get; set; }
    }
}
