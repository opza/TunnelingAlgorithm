namespace TunnelingAlgorithm
{
    public enum TileType
    {
        Rock,
        Corridor,
        Room,
        Wall,
        Door
    }

    internal class Tile
    {
        readonly int _x;
        readonly int _y;
        TileType _type = TileType.Rock;

        public int X => _x;
        public int Y => _y;
        public TileType Type
        {
            get => _type;
            set => _type = value;
        }

        public Tile(int x, int y)
        {
            _x = x;
            _y = y;
        }
    }
}
