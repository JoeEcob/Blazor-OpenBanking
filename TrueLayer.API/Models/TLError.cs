namespace TrueLayer.API.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    internal class TLError
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }

        [JsonPropertyName("error_details")]
        public Dictionary<string, string> ErrorDetails { get; set; }
    }
}
