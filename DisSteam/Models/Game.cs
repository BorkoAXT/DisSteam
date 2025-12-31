using System.Text.Json.Serialization;

namespace DisSteam.Models
{
    public class Game {
        [JsonPropertyName("appid")] 
        public int AppId { get; set; } 

        
        [JsonPropertyName("playtime_forever")] 
        public int PlayTime { get; set; } 
    }
}
