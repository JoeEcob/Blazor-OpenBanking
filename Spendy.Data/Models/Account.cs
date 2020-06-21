using LiteDB;

namespace Spendy.Data.Models
{
    public class Account
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId ProviderId { get; set; }

        public string AccountId { get; set; } // ID from TrueLayer

        public string DisplayName { get; set; }

        public decimal AvailableBalance { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal Overdraft { get; set; }

    }
}
