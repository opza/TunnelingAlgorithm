
using System;
using TunnelingAlgorithm.Configurations;

namespace TunnelingAlgorithm
{
    public class WorldGenerator
    {       
        Random _rand = new Random();

        Config _config;
        public Config Config => _config;

        public WorldGenerator(string paramPath)
        {
            _config = Config.Read(paramPath);
        }

        public (TileType[,] tiles, RoomData[] rooms) Generate(int width, int height) => Generate(width, height, _rand.Next());

        public (TileType[,] tiles, RoomData[] rooms) Generate(int width, int height, int seed)
        {
            var world = new World(width, height);
            var tunnelers = world.Generate(_config, seed);

            var types = new TileType[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    types[x, y] = world.Tiles[x, y].Type;
                }
            }

            return (types, tunnelers[0].BuildedRooms);
        }
    }
}
