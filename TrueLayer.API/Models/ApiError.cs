namespace TrueLayer.API.Models
{
    using System.Text.Json.Serialization;

    public class ApiError
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }

        [JsonPropertyName("error_details")]
        public string ErrorDetails { get; set; }
    }
}
