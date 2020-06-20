namespace TrueLayer.API.Models
{
    using System.Text.Json.Serialization;

    public class ApiResponse<T>
    {
        [JsonPropertyName("results")]
        public T[] Results { get; set; }

        public bool ShouldAttemptRefresh { get; set; }
    }
}
