using LiteDB;
using System;

namespace Spendy.Data.Models
{
    public class Account
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId ProviderId { get; set; }

        public string AccountId { get; set; } // ID from TrueLayer

        public string DisplayName { get; set; }

        public string LogoUri { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal Overdraft { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime LastTransactionUpdate { get; set; }

    }
}
