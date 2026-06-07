namespace Spendy.Data.Models
{
    using System;
    using System.Text.Json.Serialization;
    using LiteDB;

    public class Account
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid AuthId { get; set; }

        public string AccountId { get; set; }

        public string AccountType { get; set; }

        public string DisplayName { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal Overdraft { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime LastTransactionUpdate { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public Provider Provider { get; set; }
    }
}
