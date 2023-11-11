namespace TrueLayer.API.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class TLAccessTokenMetadata
    {
        [JsonPropertyName("consent_status")]
        public string ConsentStatus { get; set; }

        [JsonPropertyName("consent_expires_at")]
        public DateTime ConsentExpiresAt { get; set; }

        [JsonPropertyName("provider")]
        public TLProvider Provider { get; set; }
    }
}
