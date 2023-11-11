namespace TrueLayer.API.Models
{
    using System.Text.Json.Serialization;

    public class TLProvider
    {
        [JsonPropertyName("provider_id")]
        public string ProviderId { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; }
        
        [JsonPropertyName("scopes")]
        public string[] Scopes { get; set; }
    }
}
