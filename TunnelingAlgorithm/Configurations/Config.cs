using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TunnelingAlgorithm.Configurations
{
    public class Config
    {
        [JsonProperty("UtilitySeed")]
        public int UtilitySeed { get; protected set; }

        [JsonProperty("CorrdiorMargin")]
        public int CorridorMargin { get; protected set; }

        [JsonProperty("GenerationCount")]
        public int GenerationCount { get; protected set; }

        [JsonProperty("Enters")]
        public EnterData[] EnterDatas { get; protected set; }

        [JsonProperty("JoinTunnelerSize")]
        public int JoinTunnelerSize { get; protected set; }

        [JsonProperty("Prob_BornTunneler")]
        public int[] ProbBornTunneler { get; protected set;  }

        [JsonProperty("Prob_ChangeJoinTunneler")]
        public int[] ProbChangeJoinTunneler { get; protected set; }

        [JsonProperty("Count_BornTunnelerMin")]
        public int[] CountBornTunnelerMin { get; protected set; }

        [JsonProperty("CorridorMinDist")]
        public int[] CorridorMinDist { get; protected set; }

        [JsonProperty("Prob_ChangeDirection")]
        public int[] ProbChangeDirection { get; protected set; }

        [JsonProperty("Speed")]
        public int[] Speed { get; protected set; }

        [JsonProperty("MaxLife")]
        public int[] MaxLife { get; protected set; }

        [JsonProperty("Prob_BuildHallFromScaleUp")]
        public int[] ProbBuildHallFromScaleUp { get; protected set; }

        [JsonProperty("Prob_BuildHallFromScaleDown")]
        public int[] ProbBuildHallFromScaleDown { get; protected set; }

        [JsonProperty("Prob_BuildHallFromCorner")]
        public int[] ProbBuildHallFromCorner { get; protected set; }

        [JsonProperty("Prob_BuildRoom")]
        public int[] ProbBuildRoom { get; protected set; }

        [JsonProperty("RoomSize")]
        public RoomSizeData RoomSizeData { get; protected set; }

        public static Config Read(string path)
        {
            if (!File.Exists(path))
                throw new Exception();

            var paramJson = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<Config>(paramJson);

            return config;
        }


        
    }
}
