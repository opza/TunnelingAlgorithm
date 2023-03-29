
namespace TunnelingAlgorithm
{
    public class WorldGenerator
    {       
        public static TileType[,] Generate(int width, int height, string paramPath)
        {
            var world = new World(width, height);
            world.Generate(paramPath);

            var types = new TileType[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    types[x, y] = world.Tiles[x, y].Type;
                }
            }

            return types;
        }
    }
}
