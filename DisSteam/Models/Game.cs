using System.Text.Json.Serialization;

namespace DisSteam.Models
{
    public class Game {
        [JsonPropertyName("appid")] 
        public long AppId { get; set; } 

        
        [JsonPropertyName("playtime_forever")] 
        public long PlayTime { get; set; } 
    }
}
