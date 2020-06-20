namespace TrueLayer.API.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    internal class AccessTokenMetadata
    {
        [JsonPropertyName("consent_status")]
        public string ConsentStatus { get; set; }

        [JsonPropertyName("consent_expires_at")]
        public DateTime ConsentExpiresAt { get; set; }

        [JsonPropertyName("provider")]
        public Dictionary<string, string> Provider { get; set; }
    }
}
