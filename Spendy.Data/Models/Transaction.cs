namespace Spendy.Data.Models
{
    using System;
    using LiteDB;

    public class Transaction
    {
        [BsonId]
        public Guid Id { get; set; }

        public string AccountId { get; set; }

        public string TransactionId { get; set; }

        public DateTime Timestamp { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string TransactionType { get; set; }

        public string TransactionCategory { get; set; }

        public string[] TransactionClassification { get; set; }

        public string MerchantName { get; set; }

        public RunningBalance RunningBalance { get; set; }
    }
}
