using System.Text.Json.Serialization;

namespace DisSteam.Models
{
    public class Badge
    {
        [JsonPropertyName("badgeid")]
        public long BadgeId { get; set; }

        [JsonPropertyName("appid")]
        public long? AppId { get; set; } 

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("completion_time")]
        public long CompletionTime { get; set; }

        [JsonPropertyName("xp")]
        public int Xp { get; set; }

        [JsonPropertyName("scarcity")]
        public int Scarcity { get; set; }

        [JsonPropertyName("border_color")]
        public int? BorderColor { get; set; } 
    }
}
