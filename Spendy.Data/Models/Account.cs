namespace Spendy.Data.Models
{
    using LiteDB;
    using System;

    public class Account
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId AuthId { get; set; }

        // ID from TrueLayer
        public string AccountId { get; set; }

        public string DisplayName { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal Overdraft { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime LastTransactionUpdate { get; set; }

        [BsonIgnore]
        public Provider Provider { get; set; }
    }
}
