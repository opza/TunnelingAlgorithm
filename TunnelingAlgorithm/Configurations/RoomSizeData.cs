
using Newtonsoft.Json;

namespace TunnelingAlgorithm.Configurations
{
    public class RoomSizeData
    {
        [JsonProperty("widthMin")]
        public int WidthMin { get; protected set; }

        [JsonProperty("widthMax")]
        public int WidthMax { get; protected set; }

        [JsonProperty("heightMin")]
        public int HeightMin { get; protected set; }

        [JsonProperty("heightMax")]
        public int HeightMax { get; protected set; }
    }
}
