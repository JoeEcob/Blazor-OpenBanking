namespace Spendy.Data.Models
{
    using LiteDB;

    public class Provider
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
