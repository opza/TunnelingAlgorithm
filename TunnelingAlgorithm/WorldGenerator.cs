
namespace TunnelingAlgorithm
{
    public class WorldGenerator
    {       
        public static (TileType[,], RoomData[]) Generate(int width, int height, string paramPath, int seed)
        {
            var world = new World(width, height);
            var tunnelers = world.Generate(paramPath, seed);

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
