namespace Spendy.Data.Models
{
    using System;
    using LiteDB;

    public class Auth
    {
        [BsonId]
        public Guid Id { get; set; }

        public string ProviderId { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
