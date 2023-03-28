

namespace TunnelingAlgorithm
{
    public class WorldGenerator
    {       
        public static Tile[,] Generate(int width, int height, string paramPath)
        {
            var world = new World(width, height);
            world.Generate(paramPath);

            return world.Tiles;
        }
    }
}
