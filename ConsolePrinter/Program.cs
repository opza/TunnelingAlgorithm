using TunnelingAlgorithm;
using TunnelingAlgorithm.Configurations;

var width = 50;
var height = 50;
var path = "E:\\Code\\Other\\TunnelingAlgorithm\\Param.json";

var worldGenerator = new WorldGenerator(path);
(var tiles, var rooms) = worldGenerator.Generate(width, height);

using var ws = new StreamWriter(new BufferedStream(Console.OpenStandardOutput()));

for (int x = 0; x < width; x++)
{
	for (int y = 0; y < height; y++)
	{
		switch (tiles[x,y])
		{
			case TileType.Rock:
				ws.Write("@");
				break;
			case TileType.Corridor:
				ws.Write(".");
				break;
			case TileType.Room:
				ws.Write(".");
				break;
			case TileType.Wall:
				ws.Write("#");
				break;
			case TileType.Door:
				ws.Write("&");
				break;
			default:
				break;
		}
	}

	ws.WriteLine();
}