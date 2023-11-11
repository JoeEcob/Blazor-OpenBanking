namespace Spendy.Data.Models
{
    using LiteDB;

    public class Auth
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string ProviderId { get; set; }

        public string ProviderDisplayName { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
