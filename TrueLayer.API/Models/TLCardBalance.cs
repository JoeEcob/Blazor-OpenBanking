namespace TrueLayer.API.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class TLCardBalance
    {
        [JsonPropertyName("available")]
        public decimal Available { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("current")]
        public decimal Current { get; set; }

        [JsonPropertyName("credit_limit")]
        public decimal CreditLimit { get; set; }

        [JsonPropertyName("last_statement_balance")]
        public decimal LastStatementBalance { get; set; }

        [JsonPropertyName("last_statement_date")]
        public string LastStatementDate { get; set; }

        [JsonPropertyName("payment_due")]
        public decimal PaymentDue { get; set; }

        [JsonPropertyName("payment_due_date")]
        public string PaymentDueDate { get; set; }

        [JsonPropertyName("update_timestamp")]
        public DateTime UpdateTimestamp { get; set; }
    }
}
