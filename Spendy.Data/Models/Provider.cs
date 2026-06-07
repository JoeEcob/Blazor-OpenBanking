namespace Spendy.Data.Models
{
    using System;
    using LiteDB;

    public class Provider
    {
        [BsonId]
        public Guid Id { get; set; }

        public string ProviderId { get; set; }

        public string DisplayName { get; set; }

        public string Country { get; set; }

        public string LogoUrl { get; set; }
    }
}
