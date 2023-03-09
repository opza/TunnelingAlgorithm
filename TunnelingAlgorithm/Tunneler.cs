using System.Diagnostics.SymbolStore;
using TunnelingAlgorithm.Configurations;

namespace TunnelingAlgorithm
{
    public abstract class Tunneler
    {
        protected const int BORDER_SIZE = 1;

        protected const int TUNNEL_SIZE_MIN = 1;
        protected const int CORRIDOR_MARGIN = 3;

        protected Random _rand;

        protected Config _config;
        protected World _world;
        protected Roomer _roomer;

        protected int _gen;
        protected int _seed;

        protected int _maxLife;
        protected int _currLife;
        protected int _speed;
        protected int _tunnelSize;
        protected int _changeDirectionChance;

        protected Position _startPivot;
        protected Direction _startDir;

        protected (Rect rect, Direction dir) _currCorridor;

        protected List<Tunneler> childs = new List<Tunneler>();
        protected List<SplitPoint> _splitPoints = new List<SplitPoint>();

        protected bool HasCorridor => _maxLife != _currLife;
        public int MaxLife => _maxLife;
        public bool Alive => _currLife > 0;
        public int TunnelSize => _tunnelSize;
        public int Generation => _gen;
        public int Seed => _seed;

        public World World => _world;
        public Config Config => _config;
        public SplitPoint[] SplitPoints => _splitPoints.ToArray();

        public int WidthMin => BORDER_SIZE;
        public int WidthMax => _world.Width - BORDER_SIZE - 1;
        public int HeightMin => BORDER_SIZE;
        public int HeightMax => _world.Height - BORDER_SIZE - 1;

        public int CorridorWidthMin => CORRIDOR_MARGIN + BORDER_SIZE;
        public int CorridorWidthMax => _world.Width - CORRIDOR_MARGIN - BORDER_SIZE - 1;
        public int CorridorHeightMin => CORRIDOR_MARGIN + BORDER_SIZE;
        public int CorridorHeightMax => _world.Height - CORRIDOR_MARGIN - BORDER_SIZE - 1;

        public static Tunneler[] CreateRootTunnelers(World world, Config config)
        {
            //ExtensionUtility.Seed = 513201206;

            return config.EnterDatas
                .Select(data => new MainTunneler(world, config, 0, new Position(data.PosX, data.PosY), data.TunnelSize, data.Direction, data.Direction, true))
                .ToArray();
        }

        protected Tunneler(World world, Config config, int gen, Position startPivot, int tunnelSize, Direction dir, int? seed)
        {
            if (seed.HasValue)
                _seed = seed.Value;
            else
                _seed = new Random().Next();
            _rand = new Random(_seed);

            _config = config;
            _world = world;
            _roomer = new Roomer(world);
            System.Diagnostics.Debug.WriteLine($"Roomer Seed : {_roomer.Seed}");

            _gen = gen;

            _startPivot = startPivot;

            _maxLife = config.MaxLife[gen];
            _currLife = config.MaxLife[gen];

            _tunnelSize = tunnelSize;
            _speed = config.Speed[gen];

            _startDir = dir;
            _currCorridor = (new Rect(), dir);
        }

        public abstract void BuildCorridor(bool hasTailRoom);

        public abstract Tunneler CreateChild(int? seed = null);

        public abstract Tunneler[] CreateScaleUpChilds(int? seed = null);
        public abstract Tunneler[] CreateScaleDownChilds(int? seed = null);

        public bool BuildRoom(bool aligned = false)
        {
            var candidatedSplitPoints = new List<SplitPoint>(_splitPoints);
            candidatedSplitPoints.Shuffle();

            foreach (var splitPoint in candidatedSplitPoints)
            {
                var dirs = Enum.GetValues<Direction>();
                dirs.Shuffle();

                foreach (var dir in dirs)
                {
                    if (splitPoint[dir] == ConnectState.Connected)
                        continue;

                    var doorDir = GetReverseDirection(dir);
                    var pivot = GetRoomPivot(splitPoint, doorDir);

                    var buildSuccess = _roomer.Build(pivot, _config.RoomSizeData.WidthMin, _config.RoomSizeData.WidthMax, _config.RoomSizeData.HeightMin, _config.RoomSizeData.HeightMax, doorDir, aligned);
                    if (!buildSuccess)
                        continue;

                    splitPoint[dir] = ConnectState.Connected;
                    return true;
                }
            }

            return false;
        }

        public void BuildRoomAll(bool aligned = false)
        {
            foreach (var splitPoint in _splitPoints)
            {
                var dirs = Enum.GetValues<Direction>();
                foreach (var dir in dirs)
                {
                    if (splitPoint[dir] == ConnectState.Connected)
                        continue;

                    var doorDir = GetReverseDirection(dir);
                    var pivot = GetRoomPivot(splitPoint, doorDir);

                    var buildSuccess = _roomer.Build(pivot, _config.RoomSizeData.WidthMin, _config.RoomSizeData.WidthMax, _config.RoomSizeData.HeightMin, _config.RoomSizeData.HeightMax, doorDir, aligned);
                    if (!buildSuccess)
                        continue;

                    splitPoint[dir] = ConnectState.Connected;
                }
            }
        }

        protected bool BuildRoom(SplitPoint splitPoint, bool aligned = false)
        {
            var dirs = Enum.GetValues<Direction>();
            dirs.Shuffle();

            foreach (var dir in dirs)
            {
                if (splitPoint[dir] == ConnectState.Connected)
                    continue;

                var doorDir = GetReverseDirection(dir);
                var buildSuccess = BuildRoom(splitPoint, doorDir, aligned);
                if(!buildSuccess)
                    continue;

                return true;
            }

            return false;
        }

        protected bool BuildRoom(SplitPoint splitPoint, Direction doorDir, bool aligned = false)
        {
            var pivot = GetRoomPivot(splitPoint, doorDir);
            return _roomer.Build(pivot, _config.RoomSizeData.WidthMin, _config.RoomSizeData.WidthMax, _config.RoomSizeData.HeightMin, _config.RoomSizeData.HeightMax, doorDir, aligned);
        }       

        protected Position GetRoomPivot(SplitPoint splitPoint, Direction doorDir) => doorDir switch
        {
            Direction.North => new Position(splitPoint.Rect.CenterX, splitPoint.YMin - BORDER_SIZE - 1),
            Direction.East => new Position(splitPoint.XMin - BORDER_SIZE - 1, splitPoint.Rect.CenterY),
            Direction.West => new Position(splitPoint.XMax + BORDER_SIZE + 1, splitPoint.Rect.CenterY),
            Direction.South => new Position(splitPoint.Rect.CenterX, splitPoint.YMax + BORDER_SIZE + 1)
        };

        protected void BuildCorridor(Rect rect) => BuildCorridor(rect.XMin, rect.YMin, rect.XMax, rect.YMax);

        protected void BuildCorridor(int xMin, int yMin, int xMax, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    var tile = _world.GetTile(x, y);
                    tile.Type = TileType.Corridor;
                }
            }
        }

        protected void BuildBorder(Rect rect) => BuildBorder(rect.XMin, rect.YMin, rect.XMax, rect.YMax);

        protected void BuildBorder(int xMin, int yMin, int xMax, int yMax)
        {
            var borderXMin = xMin - BORDER_SIZE;
            var borderXMax = xMax + BORDER_SIZE;
            var borderYMin = yMin - BORDER_SIZE;
            var borderYMax = yMax + BORDER_SIZE;

            for (int x = borderXMin; x <= borderXMax; x++)
            {
                if(x == borderXMin || x == borderXMax)
                {
                    for (int y = borderYMin; y <= borderYMax; y++)
                    {
                        var tile = _world.GetTile(x, y);
                        if(tile.Type == TileType.Rock)
                        {
                            tile.Type = TileType.Wall;
                        }
                    }
                }
                else
                {
                    var bottomTile = _world.GetTile(x, borderYMin);
                    if(bottomTile.Type == TileType.Rock)
                    {
                        bottomTile.Type = TileType.Wall;
                    }

                    var topTile = _world.GetTile(x, borderYMax);
                    if(topTile.Type == TileType.Rock)
                    {
                        topTile.Type = TileType.Wall;
                    }
                }
            }
        }  

        protected Rect? GetHallRect(Rect corridorRect, Direction dir, int hallWidth, int hallHeight)
        {
            var hallMinPos = new Position();

            var leftWidth = hallWidth - _tunnelSize;
            var leftHeight = hallHeight - _tunnelSize;

            switch (dir)
            {
                case Direction.North:
                    hallMinPos = new Position(corridorRect.XMin - leftWidth / 2, corridorRect.YMax + 1);
                    break;
                case Direction.East:
                    hallMinPos = new Position(corridorRect.XMax + 1, corridorRect.YMin - leftHeight / 2);
                    break;
                case Direction.West:
                    hallMinPos = new Position(corridorRect.XMin - leftWidth, corridorRect.YMin - leftHeight / 2);
                    break;
                case Direction.South:
                    hallMinPos = new Position(corridorRect.XMin - leftWidth / 2, corridorRect.YMin - leftHeight);
                    break;
            }

            if (hallMinPos.X < WidthMin || hallMinPos.X >= WidthMax || hallMinPos.Y < HeightMin || hallMinPos.Y >= HeightMax)
                return null;

            var hallMaxPos = new Position(hallMinPos.X + hallWidth - 1, hallMinPos.Y + hallHeight - 1);
            if (hallMaxPos.X >= WidthMax || hallMaxPos.Y >= HeightMax)
                return null;

            if (_world.ExitTileTypeAny(hallMinPos.X - 1, hallMinPos.Y - 1, hallMaxPos.X + 1, hallMaxPos.Y + 1, TileType.Room, TileType.Door))
                return null;
            
            return new Rect(hallMinPos, hallMaxPos);       
        }

        protected SplitPoint CreateCorridorSplitPoint(SplitPointType type, Rect currRect, Direction currDir) => currDir switch
        {
            Direction.North => CreateCorridorSplitPointInNorth(type, currRect),
            Direction.East => CreateCorridorSplitPointInEast(type, currRect),
            Direction.West => CreateCorridorSplitPointInWest(type, currRect),
            Direction.South => CreateCorridorSplitPointInSouth(type, currRect)
        };

        protected bool HittingCorridor(Rect corridorRect, Direction dir) => dir switch
        {
            Direction.North => _world.ExitTileTypeAll(corridorRect.XMin, corridorRect.YMax + 1, corridorRect.XMax, corridorRect.YMax + 1, TileType.Corridor),
            Direction.East => _world.ExitTileTypeAll(corridorRect.XMax + 1, corridorRect.YMin, corridorRect.XMax + 1, corridorRect.YMax, TileType.Corridor),
            Direction.West => _world.ExitTileTypeAll(corridorRect.XMin - 1, corridorRect.YMin, corridorRect.XMin - 1, corridorRect.YMax, TileType.Corridor),
            Direction.South => _world.ExitTileTypeAll(corridorRect.XMin, corridorRect.YMin - 1, corridorRect.XMax, corridorRect.YMin - 1, TileType.Corridor)
        };

        protected bool HittingRoom(Rect corridorRect, Direction dir) => dir switch
        {
            Direction.North => _world.ExitTileTypeAny(corridorRect.XMin, corridorRect.YMax + Roomer.BORDER_SIZE + 1, corridorRect.XMax, corridorRect.YMax + Roomer.BORDER_SIZE + 1, TileType.Room),
            Direction.East => _world.ExitTileTypeAny(corridorRect.XMax + Roomer.BORDER_SIZE + 1, corridorRect.YMin, corridorRect.XMax + Roomer.BORDER_SIZE + 1, corridorRect.YMax, TileType.Room),
            Direction.West => _world.ExitTileTypeAny(corridorRect.XMin - Roomer.BORDER_SIZE - 1, corridorRect.YMin, corridorRect.XMin - Roomer.BORDER_SIZE - 1, corridorRect.YMax, TileType.Room),
            Direction.South => _world.ExitTileTypeAny(corridorRect.XMin, corridorRect.YMin - Roomer.BORDER_SIZE - 1, corridorRect.XMax, corridorRect.YMin - Roomer.BORDER_SIZE - 1, TileType.Room)
        };

        SplitPoint CreateCorridorSplitPointInNorth(SplitPointType type, Rect currRect)
        {
            var xMin = currRect.XMin;
            var xMax = currRect.XMax;

            var yMax = currRect.YMax;
            var yMin = yMax - _tunnelSize + 1;

            return new SplitPoint(type, xMin, yMin, xMax, yMax);
        }

        SplitPoint CreateCorridorSplitPointInEast(SplitPointType type, Rect currRect)
        {
            var yMin = currRect.YMin;
            var yMax = currRect.YMax;

            var xMax = currRect.XMax;
            var xMin = xMax - _tunnelSize + 1;

            return new SplitPoint(type, xMin, yMin, xMax, yMax);
        }

        SplitPoint CreateCorridorSplitPointInWest(SplitPointType type, Rect currRect)
        {
            var yMin = currRect.YMin;
            var yMax = currRect.YMax;

            var xMin = currRect.XMin;
            var xMax = xMin + _tunnelSize - 1;

            return new SplitPoint(type, xMin, yMin, xMax, yMax);
        }

        SplitPoint CreateCorridorSplitPointInSouth(SplitPointType type, Rect currRect)
        {
            var xMin = currRect.XMin;
            var xMax = currRect.XMax;

            var yMin = currRect.YMin;
            var yMax = yMin + _tunnelSize - 1;

            return new SplitPoint(type, xMin, yMin, xMax, yMax);
        }

        protected Rect? GetCorridor(Position pivotPos, Direction dir)
        {
            Position minPos = new();

            switch (dir)
            {
                case Direction.North:
                    minPos = GetMinPositionWhereDirectionIsNorth(pivotPos);
                    break;
                case Direction.East:
                    minPos = GetMinPositionWhereDirectionIsEast(pivotPos);
                    break;
                case Direction.West:
                    minPos = GetMinPositionWhereDirectionIsWest(pivotPos);
                    break;
                case Direction.South:
                    minPos = GetMinPositionWhereDirectionIsSouth(pivotPos);
                    break;
            }          

            var maxPos = GetMaxPosition(minPos, dir);
            return GetUsefulRect(new Rect(minPos, maxPos), dir);
        }

        protected ((Rect rect, Direction dir)? nextCorridor, (Rect hallRect, SplitPointType type)? hall) GetNextCorridor(Rect currRect, Direction currDir)
        {
            //var rect = GetNextUsefulRect(currRect, currDir, currDir);
            //if (rect.HasValue)
            //    return (rect.Value, currDir);

            //return GetNextCorridorInOtherDirectionWithoutHall(currRect, currDir);

            var isScaleUp = _rand.Next(0, 100) < _config.ProbBuildHallFromScaleUp[_gen];
            var isScaleDown = _rand.Next(0, 100) < _config.ProbBuildHallFromScaleDown[_gen] && _tunnelSize > 1;

            Rect? scaleUpHallRect = null;
            Rect? scaleDownHallRect = null;

            if (isScaleUp)
                scaleUpHallRect = GetHallRect(currRect, currDir, _tunnelSize + 4, _tunnelSize + 4);
            else if (isScaleDown)
                scaleDownHallRect = GetHallRect(currRect, currDir, _tunnelSize + 2, _tunnelSize + 2);

            Rect? nextRect = null;

            if (isScaleUp && scaleUpHallRect.HasValue)
                nextRect = GetNextCorridorWithHall(scaleUpHallRect.Value, currDir);
            else if (isScaleDown && scaleDownHallRect.HasValue)
                nextRect = GetNextCorridorWithHall(scaleDownHallRect.Value, currDir);
            else
                nextRect = GetNextUsefulRect(currRect, currDir, currDir);

            if (nextRect.HasValue)
            {
                if (isScaleUp && scaleUpHallRect.HasValue)
                    return ((nextRect.Value, currDir), (scaleUpHallRect.Value, SplitPointType.ScaleUp));
                else if (isScaleDown && scaleDownHallRect.HasValue)
                    return ((nextRect.Value, currDir), (scaleDownHallRect.Value, SplitPointType.ScaleDown));

                return ((nextRect.Value, currDir), null);
            }

            return (GetNextCorridorInOtherDirectionWithoutHall(currRect, currDir), null);

        }

        protected ((Rect rect, Direction dir)? nextCorridor, (Rect hallRect, SplitPointType type)? hall) GetNextCorridorInOtherDirection(Rect currRect, Direction currDir)
        {
            Rect? hallRect = null;
            (Rect nextRect, Direction nextDir)? nextCorridor = null;

            var hasCreatedHallChance = _rand.Next(0, 100) < _config.ProbBuildHallFromCorner[_gen];
            if (hasCreatedHallChance)
                hallRect = GetHallRect(currRect, currDir, _tunnelSize + 2, _tunnelSize + 2);

            if (hallRect.HasValue)
                nextCorridor = GetNextCorridorInOtherDirectionWithHall(hallRect.Value, currDir);
            else
                nextCorridor = GetNextCorridorInOtherDirectionWithoutHall(currRect, currDir);

            if (hallRect.HasValue)
                return (nextCorridor, (hallRect.Value, SplitPointType.Corner));

            return (nextCorridor, null);
        }

        protected (Rect nextRect, Direction nextDir)? GetNextCorridorInOtherDirectionWithHall(Rect hallRect, Direction currDir)
        {
            var candidatedDirs = GetCandidatedDirections(currDir);
            candidatedDirs.Shuffle();

            foreach (var nextDir in candidatedDirs)
            {
                var rect = GetNextCorridorWithHall(hallRect, nextDir);
                if (rect.HasValue)            
                    return (rect.Value, nextDir);     
            }

            return null;

        }

        protected (Rect nextRect, Direction nextDir)? GetNextCorridorInOtherDirectionWithoutHall(Rect currRect, Direction currDir)
        {
            var candidatedDirs = GetCandidatedDirections(currDir);

            foreach (var nextDir in candidatedDirs)
            {
                var rect = GetNextUsefulRect(currRect, currDir, nextDir);
                if (rect.HasValue)              
                    return (rect.Value, nextDir);             
            }

            return null;
        }

        protected Rect? GetNextCorridorWithHall(Rect hallRect, Direction dir)
        {
            var pivotPos = new Position();

            switch (dir)
            {
                case Direction.North:
                    pivotPos = new Position(hallRect.CenterX - (_tunnelSize / 2), hallRect.YMax + 1);
                    break;
                case Direction.East:
                    pivotPos = new Position(hallRect.XMax + 1, hallRect.CenterY - (_tunnelSize / 2));
                    break;
                case Direction.West:
                    pivotPos = new Position(hallRect.XMin - 1, hallRect.CenterY - (_tunnelSize / 2));
                    break;
                case Direction.South:
                    pivotPos = new Position(hallRect.CenterX - (_tunnelSize / 2), hallRect.YMin - 1);
                    break;
            }

            if (pivotPos.X < WidthMin || pivotPos.Y < HeightMin)
                return null;

            return GetCorridor(pivotPos, dir);
        }

        protected virtual Direction[] GetCandidatedDirections(Direction currDir)
        {
            var candidatedDirs = Enum.GetValues<Direction>().ToList();
            candidatedDirs.Remove(GetReverseDirection(_startDir));
            candidatedDirs.Remove(GetReverseDirection(currDir));
            candidatedDirs.Shuffle();

            return candidatedDirs.ToArray();
        }

        Rect? GetUsefulRect(Rect rect, Direction dir) => dir switch
        {
            Direction.North => ReadjustCorridorInNorth(rect),
            Direction.East => ReadjustCorridorInEast(rect),
            Direction.West => ReadjustCorridorInWest(rect),
            Direction.South => ReadjustCorridorInSouth(rect)
        };

        Rect? GetNextUsefulRect(Rect currRect, Direction currDir, Direction nextDir) => nextDir switch
        {
            Direction.North => GetNextCorridorRectWhereNextDirectionIsNorth(currRect, currDir),
            Direction.East => GetNextCorridorRectWhereNextDirectionIsEast(currRect, currDir),
            Direction.West => GetNextCorridorRectWhereNextDirectionIsWest(currRect, currDir),
            Direction.South => GetNextCorridorRectWhereNextDirectionIsSouth(currRect, currDir),
            _ => null
        };
        
        Position GetMinPositionWhereDirectionIsNorth(Position pivotPos)
        {
            var xMin = pivotPos.X;
            var yMin = pivotPos.Y;

            var xMax = xMin + _tunnelSize - 1;
            if (xMax > CorridorWidthMax)
                xMin -= xMax - CorridorWidthMax;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            return new Position(xMin, yMin);
        }

        Position GetMinPositionWhereDirectionIsEast(Position pivotPos)
        {
            var xMin = pivotPos.X;
            var yMin = pivotPos.Y;

            var yMax = yMin + _tunnelSize - 1;
            if (yMax > CorridorHeightMax)
                yMin -= yMax - CorridorHeightMax;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            return new Position(xMin, yMin);
        }

        Position GetMinPositionWhereDirectionIsWest(Position pivotPos)
        {
            var xMin = pivotPos.X;
            var yMin = pivotPos.Y;

            xMin = xMin - _speed + 1;

            var yMax = yMin + _tunnelSize - 1;
            if (yMax > CorridorHeightMax)
                yMin -= yMax - CorridorHeightMax;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            return new Position(xMin, yMin);
        }

        Position GetMinPositionWhereDirectionIsSouth(Position pivotPos)
        {
            var xMin = pivotPos.X;
            var yMin = pivotPos.Y;

            var xMax = xMin + _tunnelSize - 1;
            if (xMax > CorridorWidthMax)
                xMin -= xMax - CorridorWidthMax;

            yMin = yMin - _speed + 1;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            return new Position(xMin, yMin);
        }     

        Position GetMaxPosition(Position minPos, Direction dir)
        {
            var width = 0;
            var height = 0;

            switch (dir)
            {
                case Direction.North:
                case Direction.South:
                    width = _tunnelSize;
                    height = _speed;
                    break;
                case Direction.East:
                case Direction.West:
                    width = _speed;
                    height = _tunnelSize;
                    break;
            }

            var xMax = minPos.X + width - 1;
            var yMax = minPos.Y + height - 1;

            if (xMax > CorridorWidthMax)
                xMax = CorridorWidthMax;

            if (yMax > CorridorHeightMax)
                yMax = CorridorHeightMax;

            return new Position(xMax, yMax);
        }

        Rect? GetNextCorridorRectWhereNextDirectionIsNorth(Rect rect, Direction currDir)
        {
            var minPos = GetNextMinPositionWhereNextDirectionIsNorth(rect, currDir);
            if (!minPos.HasValue)
                return null;

            var (xMin, yMin) = (minPos.Value.X ,minPos.Value.Y);           

            var xMax = xMin + _tunnelSize - 1;
            var yMax = yMin + _speed - 1;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            if (xMax > CorridorWidthMax)
                xMax = CorridorWidthMax;

            if (yMax > CorridorHeightMax)
                yMax = CorridorHeightMax;

            return ReadjustCorridorInNorth(new Rect(xMin, yMin, xMax, yMax));
        }

        Rect? GetNextCorridorRectWhereNextDirectionIsEast(Rect rect, Direction currDir)
        {
            var minPos = GetNextMinPositionWhereNextDirectionIsEast(rect, currDir);
            if (!minPos.HasValue)
                return null;

            var (xMin, yMin) = (minPos.Value.X, minPos.Value.Y);          

            var xMax = xMin + _speed - 1;
            var yMax = yMin + _tunnelSize - 1;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            if (xMax > CorridorWidthMax)
                xMax = CorridorWidthMax;

            if (yMax > CorridorHeightMax)
                yMax = CorridorHeightMax;

            return ReadjustCorridorInEast(new Rect(xMin, yMin, xMax, yMax));
        }

        Rect? GetNextCorridorRectWhereNextDirectionIsWest(Rect rect, Direction currDir)
        {
            var minPos = GetNextMinPositionWhereNextDirectionIsWest(rect, currDir);
            if (!minPos.HasValue)
                return null;

            var (xMin, yMin) = (minPos.Value.X, minPos.Value.Y);     

            var xMax = xMin + _speed - 1;
            var yMax = yMin + _tunnelSize - 1;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            if (xMax > CorridorWidthMax)
                xMax = CorridorWidthMax;

            if (yMax > CorridorHeightMax)
                yMax = CorridorHeightMax;

            return ReadjustCorridorInWest(new Rect(xMin, yMin, xMax, yMax));
        }

        Rect? GetNextCorridorRectWhereNextDirectionIsSouth(Rect rect, Direction currDir)
        {
            var minPos = GetNextMinPositionWhereNextDirectionIsSouth(rect, currDir);
            if (!minPos.HasValue)
                return null;

            var (xMin, yMin) = (minPos.Value.X, minPos.Value.Y);          

            var xMax = xMin + _tunnelSize - 1;
            var yMax = yMin + _speed - 1;

            if (xMin < CorridorWidthMin)
                xMin = CorridorWidthMin;

            if (yMin < CorridorHeightMin)
                yMin = CorridorHeightMin;

            if (xMax > CorridorWidthMax)
                xMax = CorridorWidthMax;

            if (yMax > CorridorHeightMax)
                yMax = CorridorHeightMax;

            return ReadjustCorridorInSouth(new Rect(xMin, yMin, xMax, yMax));
        }

        Position? GetNextMinPositionWhereNextDirectionIsNorth(Rect rect, Direction currDir)
        {
            var nextXMin = rect.XMin;
            var nextYMin = rect.YMin;

            switch (currDir)
            {
                case Direction.North:
                    nextYMin = rect.YMax + 1;
                    break;
                case Direction.East:
                    nextXMin = rect.XMax - _tunnelSize + 1;
                    nextYMin = rect.YMax + 1;
                    break;
                case Direction.West:
                    nextYMin = rect.YMax + 1;
                    break;
                default:
                    return null;
            }

            return new Position(nextXMin, nextYMin);
        }

        Position? GetNextMinPositionWhereNextDirectionIsEast(Rect rect, Direction currDir)
        {
            var nextXMin = rect.XMin;
            var nextYMin = rect.YMin;

            switch (currDir)
            {
                case Direction.North:
                    nextXMin = rect.XMax + 1;
                    nextYMin = rect.YMax - _tunnelSize + 1;
                    break;
                case Direction.East:
                    nextXMin = rect.XMax + 1;
                    break;
                case Direction.South:
                    nextXMin = rect.XMax + 1;
                    break;
                default:
                    return null;
            }

            return new Position(nextXMin, nextYMin);
        }

        Position? GetNextMinPositionWhereNextDirectionIsWest(Rect rect, Direction currDir)
        {
            var nextXMin = rect.XMin;
            var nextYMin = rect.YMin;

            switch (currDir)
            {
                case Direction.North:
                    nextXMin -= _speed;
                    nextYMin = rect.YMax - _tunnelSize + 1;
                    break;
                case Direction.West:
                case Direction.South:
                    nextXMin -= _speed;
                    break;
                default:
                    return null;
            }

            return new Position(nextXMin, nextYMin);
        }

        Position? GetNextMinPositionWhereNextDirectionIsSouth(Rect rect, Direction currDir)
        {
            var nextXMin = rect.XMin;
            var nextYMin = rect.YMin;

            switch (currDir)
            {
                case Direction.East:
                    nextXMin = rect.XMax - _tunnelSize + 1;
                    nextYMin -= _speed;
                    break;
                case Direction.West:
                case Direction.South:
                    nextYMin -= _speed;
                    break;
                default:
                    return null;
            }

            return new Position(nextXMin, nextYMin);
        }
        
        // TODO : 안좁아지도록 수정
        protected virtual Rect? ReadjustCorridorInNorth(Rect rect)
        {
            //var newXMin = rect.XMin;
            //var newXMax = rect.XMax;

            //var neighborYMax = rect.YMax;
            //for (int y = rect.YMax; y >= rect.YMin; y--)
            //{
            //    if (_world.ExitTileTypeAll(rect.XMin, y, rect.XMax, y, TileType.Corridor))
            //        neighborYMax = y - 1;
            //}

            //for (int x = 0; x < rect.Width; x++)
            //{
            //    var leftTileX = rect.XMin + x;
            //    var rightTileX = rect.XMax - x;

            //    if (_world.ExitTileTypeAny(leftTileX, rect.YMin, leftTileX, neighborYMax, TileType.Room) || _world.ExitTileTypeAny(rightTileX, rect.YMin, rightTileX, neighborYMax, TileType.Room))
            //    {
            //        newXMin = leftTileX + Roomer.BORDER_SIZE + 1;
            //        newXMax = rightTileX - Roomer.BORDER_SIZE - 1;
            //    }
            //    else if ((_world.ExitTileTypeAny(leftTileX, rect.YMin, leftTileX, neighborYMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(leftTileX - Roomer.BORDER_SIZE, rect.YMin, leftTileX - Roomer.BORDER_SIZE, neighborYMax, TileType.Room)) ||
            //             (_world.ExitTileTypeAny(rightTileX, rect.YMin, rightTileX, neighborYMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rightTileX + Roomer.BORDER_SIZE, rect.YMin, rightTileX + Roomer.BORDER_SIZE, neighborYMax, TileType.Room)))
            //    {
            //        newXMin = leftTileX + 1;
            //        newXMax = rightTileX - 1;
            //    }
            //}

            //var newWidth = newXMax - newXMin + 1;
            //if (newWidth < TUNNEL_SIZE_MIN)
            //    return null;

            //var newYMin = rect.YMin;
            //var newYMax = rect.YMax;

            //for (int y = rect.YMax; y >= rect.YMin; y--)
            //{
            //    if (_world.ExitTileTypeAny(newXMin, y, newXMax, y, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(newXMin, y + Roomer.BORDER_SIZE, newXMax, y + Roomer.BORDER_SIZE, TileType.Room))
            //        newYMax = y - 1;
            //    else if (_world.ExitTileTypeAny(newXMin, y, newXMax, y, TileType.Room))
            //        newYMax = y - Roomer.BORDER_SIZE - 1;
            //}

            //if (newYMax < newYMin)
            //    return null;

            //return new Rect(newXMin, newYMin, newXMax, newYMax);
            //

            var newYMin = rect.YMin;
            var newYMax = newYMin - 1;

            for (int y = rect.YMin; y <= rect.YMax; y++)
            {
                if (_world.ExitTileTypeAll(rect.XMin, y, rect.XMax, y, TileType.Corridor))
                    break;
                else if (_world.ExitTileTypeAny(rect.XMin, y, rect.XMax, y, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rect.XMin - Roomer.BORDER_SIZE, y + Roomer.BORDER_SIZE, rect.XMax + Roomer.BORDER_SIZE, y + Roomer.BORDER_SIZE, TileType.Room))               
                    break;               
                else if (_world.ExitTileTypeAny(rect.XMin, y, rect.XMax, y, TileType.Room))            
                    break;

                newYMax = y;
            }

            if (newYMax < newYMin)
                return null;

            return new Rect(rect.XMin, newYMin, rect.XMax, newYMax);
        }
            
        protected virtual Rect? ReadjustCorridorInEast(Rect rect)
        {
            //var newYMin = rect.YMin;
            //var newYMax = rect.YMax;

            //var neighborXMax = rect.XMax;
            //for (int x = rect.XMax; x >= rect.XMin; x--)  
            //{
            //    if (_world.ExitTileTypeAll(x, rect.YMin, x, rect.YMax, TileType.Corridor))
            //        neighborXMax = x - 1;
            //}

            //for (int y = 0; y < rect.Height; y++)
            //{
            //    var topTileY = rect.YMax - y;
            //    var bottomTileY = rect.YMin + y;

            //    if (_world.ExitTileTypeAny(rect.XMin, topTileY, neighborXMax, topTileY, TileType.Room) || _world.ExitTileTypeAny(rect.XMin, bottomTileY, neighborXMax, bottomTileY, TileType.Room))
            //    {
            //        newYMax = topTileY - Roomer.BORDER_SIZE - 1;
            //        newYMin = bottomTileY + Roomer.BORDER_SIZE + 1;
            //    }
            //    else if ((_world.ExitTileTypeAny(rect.XMin, topTileY, neighborXMax, topTileY, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rect.XMin, topTileY + Roomer.BORDER_SIZE, neighborXMax, topTileY + Roomer.BORDER_SIZE, TileType.Room)) ||
            //             (_world.ExitTileTypeAny(rect.XMin, bottomTileY, neighborXMax, bottomTileY, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rect.XMin, bottomTileY - Roomer.BORDER_SIZE, neighborXMax, bottomTileY - Roomer.BORDER_SIZE, TileType.Room)))
            //    {
            //        newYMax = topTileY - 1;
            //        newYMin = bottomTileY + 1;
            //    }
            //}

            //var newHeight = newYMax - newYMin + 1;
            //if (newHeight < TUNNEL_SIZE_MIN)
            //    return null;

            //var newXMin = rect.XMin;
            //var newXMax = rect.XMax;

            //for (int x = rect.XMax; x >= rect.XMin; x--)
            //{
            //    if (_world.ExitTileTypeAny(x, newYMin, x, newYMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(x + Roomer.BORDER_SIZE, newYMin, x + Roomer.BORDER_SIZE, newYMax, TileType.Room))
            //        newXMax = x - 1;
            //    else if (_world.ExitTileTypeAny(x, newYMin, x, newYMax, TileType.Room))
            //        newXMax = x - Roomer.BORDER_SIZE - 1;
            //}

            //if (newXMax < newXMin)
            //    return null;

            //return new Rect(newXMin, newYMin, newXMax, newYMax);

            var newXMin = rect.XMin;
            var newXMax = newXMin - 1;

            for (int x = rect.XMin; x <= rect.XMax; x++)
            {
                if (_world.ExitTileTypeAll(x, rect.YMin, x, rect.YMax, TileType.Corridor))
                    break;
                else if (_world.ExitTileTypeAny(x, rect.YMin, x, rect.YMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(x + Roomer.BORDER_SIZE, rect.YMin - Roomer.BORDER_SIZE, x + Roomer.BORDER_SIZE, rect.YMax + Roomer.BORDER_SIZE, TileType.Room))
                    break;
                else if (_world.ExitTileTypeAny(x, rect.YMin, x, rect.YMax, TileType.Room))
                    break;

                newXMax = x;
            }

            if (newXMax < newXMin)
                return null;

            return new Rect(newXMin, rect.YMin, newXMax, rect.YMax);
        }

        protected virtual Rect? ReadjustCorridorInWest(Rect rect)
        {
            //var newYMin = rect.YMin;
            //var newYMax = rect.YMax;

            //var neighborXMin = rect.XMin;
            //for (int x = rect.XMin; x <= rect.XMax; x++)
            //{
            //    if (_world.ExitTileTypeAll(x, rect.YMin, x, rect.YMax, TileType.Corridor))
            //        neighborXMin = x + 1;
            //}

            //for (int y = 0; y < rect.Height; y++)
            //{
            //    var topTileY = rect.YMax - y;
            //    var bottomTileY = rect.YMin + y;

            //    if (_world.ExitTileTypeAny(neighborXMin, topTileY, rect.XMax, topTileY, TileType.Room) || _world.ExitTileTypeAny(neighborXMin, bottomTileY, rect.XMax, bottomTileY, TileType.Room))
            //    {
            //        newYMin = bottomTileY + Roomer.BORDER_SIZE + 1;
            //        newYMax = topTileY - Roomer.BORDER_SIZE - 1;
            //    }
            //    else if ((_world.ExitTileTypeAny(neighborXMin, topTileY, rect.XMax, topTileY, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(neighborXMin, topTileY + Roomer.BORDER_SIZE, rect.XMax, topTileY + Roomer.BORDER_SIZE, TileType.Room)) ||
            //             (_world.ExitTileTypeAny(neighborXMin, bottomTileY, rect.XMax, bottomTileY, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(neighborXMin, bottomTileY - Roomer.BORDER_SIZE, rect.XMax, bottomTileY - Roomer.BORDER_SIZE, TileType.Room)))
            //    {
            //        newYMax = topTileY - 1;
            //        newYMin = bottomTileY + 1;
            //    }
            //}

            //var newHeight = newYMax - newYMin + 1;
            //if (newHeight < TUNNEL_SIZE_MIN)
            //    return null;

            //var newXMin = rect.XMin;
            //var newXMax = rect.XMax;

            //for (int x = rect.XMin; x <= rect.XMax; x++)
            //{
            //    if (_world.ExitTileTypeAny(x, newYMin, x, newYMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(x - Roomer.BORDER_SIZE, newYMin, x - Roomer.BORDER_SIZE, newYMax, TileType.Room))
            //        newXMin = x + 1;
            //    else if (_world.ExitTileTypeAny(x, newYMin, x, newYMax, TileType.Room))
            //        newXMin = x + Roomer.BORDER_SIZE + 1;
            //}

            //if (newXMax < newXMin)
            //    return null;

            //return new Rect(newXMin, newYMin, newXMax, newYMax);

            var newXMax = rect.XMax;
            var newXMin = newXMax + 1;

            for (int x = rect.XMax; x >= rect.XMin; x--)
            {
                if (_world.ExitTileTypeAll(x, rect.YMin, x, rect.YMax, TileType.Corridor))
                    break;
                else if (_world.ExitTileTypeAny(x, rect.YMin, x, rect.YMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(x - Roomer.BORDER_SIZE, rect.YMin - Roomer.BORDER_SIZE, x - Roomer.BORDER_SIZE, rect.YMax + Roomer.BORDER_SIZE, TileType.Room))
                    break;
                else if (_world.ExitTileTypeAny(x, rect.YMin, x, rect.YMax, TileType.Room))
                    break;

                newXMin = x;
            }

            if (newXMin > newXMax)
                return null;

            return new Rect(newXMin, rect.YMin, newXMax, rect.YMax);
        }

        protected virtual Rect? ReadjustCorridorInSouth(Rect rect)
        {
            //var newXMin = rect.XMin;
            //var newXMax = rect.XMax;

            //var neighborYMin = rect.YMin;
            //for (int y = rect.YMin; y <= rect.YMax; y++)
            //{
            //    if (_world.ExitTileTypeAll(rect.XMin, y, rect.XMax, y, TileType.Corridor))
            //        neighborYMin = y + 1;
            //}

            //for (int x = 0; x < rect.Width; x++)
            //{
            //    var leftTileX = rect.XMin + x;
            //    var rightTileX = rect.XMax - x;

            //    if (_world.ExitTileTypeAny(leftTileX, neighborYMin, leftTileX, rect.YMax, TileType.Room) || _world.ExitTileTypeAny(rightTileX, neighborYMin, rightTileX, rect.YMax, TileType.Room))
            //    {
            //        newXMax = rightTileX - Roomer.BORDER_SIZE - 1;
            //        newXMin = leftTileX + Roomer.BORDER_SIZE + 1;
            //    }
            //    else if ((_world.ExitTileTypeAny(leftTileX, neighborYMin, leftTileX, rect.YMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(leftTileX - Roomer.BORDER_SIZE, neighborYMin, leftTileX - Roomer.BORDER_SIZE, rect.YMax, TileType.Room)) ||
            //             (_world.ExitTileTypeAny(rightTileX, neighborYMin, rightTileX, rect.YMax, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rightTileX + Roomer.BORDER_SIZE, neighborYMin, rightTileX + Roomer.BORDER_SIZE, rect.YMax, TileType.Room)))
            //    {
            //        newXMax = rightTileX - 1;
            //        newXMin = leftTileX + 1;
            //    }
            //}

            //var newWidth = newXMax - newXMin + 1;
            //if (newWidth < TUNNEL_SIZE_MIN)
            //    return null;

            //var newYMin = rect.YMin;
            //var newYMax = rect.YMax;

            //for (int y = rect.YMin; y <= rect.YMax; y++)
            //{
            //    if (_world.ExitTileTypeAny(newXMin, y, newXMax, y, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(newXMin, y - Roomer.BORDER_SIZE, newXMax, y - Roomer.BORDER_SIZE, TileType.Room))
            //        newYMin = y + 1;
            //    else if (_world.ExitTileTypeAny(newXMin, y, newXMax, y, TileType.Room))
            //        newYMin = y + Roomer.BORDER_SIZE + 1;
            //}

            //if (newYMax < newYMin)
            //    return null;

            //return new Rect(newXMin, newYMin, newXMax, newYMax);

            var newYMax = rect.YMax;
            var newYMin = newYMax + 1;

            for (int y = rect.YMax; y >= rect.YMin; y--)
            {
                if (_world.ExitTileTypeAll(rect.XMin, y, rect.XMax, y, TileType.Corridor))
                    break;
                else if (_world.ExitTileTypeAny(rect.XMin, y, rect.XMax, y, TileType.Wall, TileType.Door) && _world.ExitTileTypeAny(rect.XMin - Roomer.BORDER_SIZE, y - Roomer.BORDER_SIZE, rect.XMax + Roomer.BORDER_SIZE, y - Roomer.BORDER_SIZE, TileType.Room))
                    break;
                else if (_world.ExitTileTypeAny(rect.XMin, y, rect.XMax, y, TileType.Room))
                    break;

                newYMin = y;
            }

            if (newYMin > newYMax)
                return null;

            return new Rect(rect.XMin, newYMin, rect.XMax, newYMax);
        }  

        protected Direction GetReverseDirection(Direction dir) => dir switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.South => Direction.North,
            _ => throw new Exception()
        };

        protected void Kill() => _currLife = 0;
    }
}
