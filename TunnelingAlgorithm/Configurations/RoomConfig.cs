using Newtonsoft.Json;

namespace TunnelingAlgorithm.Configurations
{
    public class RoomConfig
    {
        [JsonProperty("RoomType")]
        public RoomType RoomType { get; protected set; }

        [JsonProperty("Count")]
        public int Count { get; protected set; }

        [JsonProperty("Width")]
        public int Width { get; protected set; }
        [JsonProperty("Height")]
        public int Height { get; protected set; }
    }
}
