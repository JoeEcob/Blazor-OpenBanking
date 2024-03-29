﻿namespace Spendy.Data.Models
{
    using LiteDB;
    using System;

    public class Card
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId AuthId { get; set; }

        // ID from TrueLayer
        public string AccountId { get; set; }

        public string DisplayName { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal CreditLimit { get; set; }

        public decimal LastStatementBalance { get; set; }

        public string LastStatementDate { get; set; }

        public decimal PaymentDue { get; set; }

        public string PaymentDueDate { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime LastTransactionUpdate { get; set; }

        [BsonIgnore]
        public Provider Provider { get; set; }
    }
}
