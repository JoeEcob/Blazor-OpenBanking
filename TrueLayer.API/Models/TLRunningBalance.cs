namespace TrueLayer.API.Models
{
    using System.Text.Json.Serialization;

    public class RunningBalance
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}
