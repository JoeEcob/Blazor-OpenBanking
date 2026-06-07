namespace Spendy.Data.Models
{
    using System;
    using System.Text.Json.Serialization;
    using LiteDB;

    public class Card
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid AuthId { get; set; }

        public string AccountId { get; set; }

        public string DisplayName { get; set; }

        public string PartialCardNumber { get; set; }

        public string CardType { get; set; }

        public string CardNetwork { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal CreditLimit { get; set; }

        public decimal LastStatementBalance { get; set; }

        public string LastStatementDate { get; set; }

        public decimal PaymentDue { get; set; }

        public string PaymentDueDate { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime LastTransactionUpdate { get; set; }

        public string? CustomDisplayName { get; set; }

        public int? DisplayOrder { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public Provider Provider { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public string ResolvedDisplayName => CustomDisplayName ?? DisplayName;
    }
}
