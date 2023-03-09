using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TunnelingAlgorithm.Configurations
{
    public class EnterData
    {
        [JsonProperty(PropertyName = "seed")]
        public int? Seed { get; protected set; }

        [JsonProperty(PropertyName = "posX")]
        public int PosX { get; protected set; }

        [JsonProperty(PropertyName = "posY")]
        public int PosY { get; protected set; }

        [JsonProperty(PropertyName = "tunnelSize")]
        public int TunnelSize { get; protected set; }

        [JsonProperty(PropertyName = "dir", ItemConverterType = typeof(StringEnumConverter))]
        public Direction Direction { get; protected set; }
    }
}
