namespace TunnelingAlgorithm
{
    internal enum SplitPointType
    {
        None,
        ScaleUp,
        ScaleDown,
        Corner,
    }

    internal enum ConnectState
    {
        NonConnected,
        Connected
    }

    internal class SplitPoint
    {
        const int SPLIT_STATE_CHECKING_SIZE = 2;

        Dictionary<Direction, ConnectState> _connectStates = new Dictionary<Direction, ConnectState>();

        SplitPointType _type;
        Rect _rect;

        public ConnectState this[Direction direction]
        {
            get => _connectStates[direction];
            set => _connectStates[direction] = value;
        }

        public SplitPointType Type => _type;
        public Rect Rect => _rect;

        public int XMin => _rect.XMin;
        public int YMin => _rect.YMin;
        public int XMax => _rect.XMax;
        public int YMax => _rect.YMax;

        public int NonConnectedCount => _connectStates.Count(pair => pair.Value == ConnectState.NonConnected);
        public int ConnectedCount => _connectStates.Count(pair => pair.Value == ConnectState.Connected);


        public SplitPoint(SplitPointType type, int xMin, int yMin, int xMax, int yMax) : this(type, new Rect(xMin, yMin, xMax, yMax)) { }

        public SplitPoint(SplitPointType type, Rect rect)
        {
            _type = type;
            _rect = rect;

            foreach (var dir in Enum.GetValues<Direction>())
            {
                _connectStates.Add(dir, ConnectState.NonConnected);
            }
        }

        public void UpdateState(World world)
        {
            foreach (var dir in Enum.GetValues<Direction>())
            {
                var splitState = dir switch
                {
                    Direction.North => GetStateInNorth(world),
                    Direction.East => GetStateInEast(world),
                    Direction.West => GetStateInWest(world),
                    Direction.South => GetStateInSouth(world)
                };

                _connectStates[dir] = splitState;
            }
        }

        ConnectState GetStateInNorth(World world)
        {
            if (world.ExitTileTypeAny(XMin, YMax + 1, XMax, YMax + SPLIT_STATE_CHECKING_SIZE, TileType.Corridor, TileType.Room, TileType.Door))
                return ConnectState.Connected;

            return ConnectState.NonConnected;
        }

        ConnectState GetStateInEast(World world)
        {
            if (world.ExitTileTypeAny(XMax + 1, YMin, XMax + SPLIT_STATE_CHECKING_SIZE, YMax, TileType.Corridor, TileType.Room, TileType.Door))
                return ConnectState.Connected;

            return ConnectState.NonConnected;
        }

        ConnectState GetStateInWest(World world)
        {
            if (world.ExitTileTypeAny(XMin - SPLIT_STATE_CHECKING_SIZE, YMin, XMin - 1, YMax, TileType.Corridor, TileType.Room, TileType.Door))
                return ConnectState.Connected;

            return ConnectState.NonConnected;
        }

        ConnectState GetStateInSouth(World world)
        {
            if(world.ExitTileTypeAny(XMin, YMin - SPLIT_STATE_CHECKING_SIZE, XMax, YMin - 1, TileType.Corridor, TileType.Room, TileType.Door))
                return ConnectState.Connected;

            return ConnectState.NonConnected;
        }


    }
}
