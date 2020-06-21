namespace Spendy.Data.Models
{
    using LiteDB;
    using TrueLayer.API.Models;

    public class Provider
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public AccessToken AccessToken { get; set; }
    }
}
