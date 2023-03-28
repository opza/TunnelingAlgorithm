using OpzaUtil.Linq;

namespace TunnelingAlgorithm
{
    internal class Roomer
    {
        public const int BORDER_SIZE = 1;

        Random _rand;
        World _world;

        int _seed;

        int WidthMin => BORDER_SIZE;
        int WidthMax => _world.Width - BORDER_SIZE - 1;
        int HeightMin => BORDER_SIZE;
        int HeightMax => _world.Height - BORDER_SIZE - 1;

        public int Seed => _seed;


        public Roomer(World world, int? seed = null)
        {
            if (seed.HasValue)
                _seed = seed.Value;
            else
                _seed = new Random().Next();

            _rand = new Random(_seed);
            _world = world;
        }

        public bool Build(Position pivot, int widthMin, int widthMax, int heightMin, int heightMax, Direction doorDir, bool aligned = false)
        {
            var roomWidth = _rand.Next(widthMin, widthMax + 1);
            var roomHeight = _rand.Next(heightMin, heightMax + 1);

            var roomRect = GetRoomRect(pivot, roomWidth, roomHeight, doorDir);

            Rect? adjustedRect = null;

            switch (doorDir)
            {
                case Direction.North:
                    adjustedRect = ReadjustWithNorthDoor(roomRect, widthMin, heightMin);
                    break;
                case Direction.East:
                    adjustedRect = ReadjustWithEastDoor(roomRect, widthMin, heightMin);
                    break;
                case Direction.West:
                    adjustedRect = ReadjustWithWestDoor(roomRect, widthMin, heightMin);
                    break;
                case Direction.South:
                    adjustedRect = ReadjustWithSouthDoor(roomRect, widthMin, heightMin);
                    break;
            }

            if (!adjustedRect.HasValue)
                return false;

            BuildRoom(adjustedRect.Value);
            BuildBorder(adjustedRect.Value);

            var doorTile = GetDoorTile(adjustedRect.Value, doorDir, aligned);
            BuildDoor(doorTile);

            return true;
        }

        void BuildRoom(Rect rect) => BuildRoom(rect.XMin, rect.YMin, rect.XMax, rect.YMax);

        void BuildRoom(int xMin, int yMin, int xMax, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    var tile = _world.GetTile(x, y);
                    tile.Type = TileType.Room;
                }
            }
        }

        void BuildBorder(Rect rect) => BuildBorder(rect.XMin, rect.YMin, rect.XMax, rect.YMax);

        void BuildBorder(int xMin, int yMin, int xMax, int yMax)
        {
            var borderXMin = xMin - BORDER_SIZE;
            var borderXMax = xMax + BORDER_SIZE;
            var borderYMin = yMin - BORDER_SIZE;
            var borderYMax = yMax + BORDER_SIZE;

            for (int x = borderXMin; x <= borderXMax; x++)
            {
                if (x == borderXMin || x == borderXMax)
                {
                    for (int y = borderYMin; y <= borderYMax; y++)
                    {
                        var tile = _world.GetTile(x, y);
                        if (tile.Type == TileType.Corridor || tile.Type == TileType.Room || tile.Type == TileType.Door)
                            continue;

                        tile.Type = TileType.Wall;
                    }
                }
                else
                {
                    var bottomTile = _world.GetTile(x, borderYMin);
                    if (bottomTile.Type != TileType.Corridor && bottomTile.Type != TileType.Room && bottomTile.Type != TileType.Door)
                        bottomTile.Type = TileType.Wall;

                    var topTile = _world.GetTile(x, borderYMax);
                    if (topTile.Type != TileType.Corridor && topTile.Type != TileType.Room && topTile.Type != TileType.Door)
                        topTile.Type = TileType.Wall;
                }
            }
        }

        void BuildDoor(Tile doorTile)
        {
            doorTile.Type = TileType.Door;
        }

        Rect GetRoomRect(Position pivot, int width, int height, Direction doorDir) => doorDir switch
        {
            Direction.North => GetRoomRectWithNorthDoor(pivot, width, height),
            Direction.East => GetRoomRectWithEastDoor(pivot, width, height),
            Direction.West => GetRoomRectWithWestDoor(pivot, width, height),
            Direction.South => GetRoomRectWithSouthDoor(pivot, width, height)
        };

        Rect GetRoomRectWithNorthDoor(Position pivot, int width, int height)
        {
            var widthHalf = width / 2;

            var xMin = pivot.X - widthHalf;
            var xMax = xMin + width - 1;

            var yMax = pivot.Y;
            var yMin = yMax - height + 1;

            return new Rect(xMin, yMin, xMax, yMax);
        }

        Rect GetRoomRectWithEastDoor(Position pivot, int width, int height)
        {
            var heightHalf = height / 2;

            var yMin = pivot.Y - heightHalf;
            var yMax = pivot.Y + height - 1;

            var xMax = pivot.X;
            var xMin = pivot.X - width + 1;

            return new Rect(xMin, yMin, xMax, yMax);
        }

        Rect GetRoomRectWithWestDoor(Position pivot, int width, int height)
        {
            var heightHalf = height / 2;

            var yMin = pivot.Y - heightHalf;
            var yMax = pivot.Y + height - 1;

            var xMin = pivot.X;
            var xMax = xMin + width - 1;

            return new Rect(xMin, yMin, xMax, yMax);
        }

        Rect GetRoomRectWithSouthDoor(Position pivot, int width, int height)
        {
            var widthHalf = width / 2;

            var xMin = pivot.X - widthHalf;
            var xMax = xMin + width - 1;

            var yMin = pivot.Y;
            var yMax = yMin + height - 1;

            return new Rect(xMin, yMin, xMax, yMax);
        }


        Rect? ReadjustWithNorthDoor(Rect rect, int widthMin, int heightMin)
        {
            var newXMin = rect.XMin;
            var newXMax = rect.XMax;

            for (int x = 0; x < rect.Width / 2; x++)
            {
                var neightborTileYMin = rect.YMax - heightMin + 1;

                var leftTileX = rect.XMin + x;
                if (!ValidPosition(leftTileX, neightborTileYMin) || _world.ExitTileTypeAny(leftTileX, neightborTileYMin, leftTileX, rect.YMax, TileType.Corridor, TileType.Room))
                    newXMin = leftTileX + BORDER_SIZE + 1;
                else if (_world.ExitTileTypeAny(leftTileX, neightborTileYMin, leftTileX, rect.YMax, TileType.Wall, TileType.Door))
                    newXMin = leftTileX + 1;

                var rightTileX = rect.XMax - x;
                if (!ValidPosition(rightTileX, neightborTileYMin) || _world.ExitTileTypeAny(rightTileX, neightborTileYMin, rightTileX, rect.YMax, TileType.Corridor, TileType.Room))
                    newXMax = rightTileX - BORDER_SIZE - 1;
                else if (_world.ExitTileTypeAny(rightTileX, neightborTileYMin, rightTileX, rect.YMax, TileType.Wall, TileType.Door))
                    newXMax = rightTileX - 1;
            }

            var missingXMin = newXMin - rect.XMin;
            var missingXMax = rect.XMax - newXMax;

            for (int i = 0; i < missingXMin; i++)
            {
                var neighborX1 = newXMax + 1;
                var neighborX2 = newXMax + BORDER_SIZE + 1;
                var neighborYMin = rect.YMax - heightMin + 1;

                if (!ValidPosition(neighborX1, neighborYMin) || !ValidPosition(neighborX2, neighborYMin))
                    break;

                if (_world.ExitTileTypeAny(neighborX1, neighborYMin, neighborX1, rect.YMax, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborX2, neighborYMin, neighborX2, rect.YMax, TileType.Corridor, TileType.Room))
                    break;

                newXMax++;
            }

            for (int i = 0; i < missingXMax; i++)
            {

                var neighborX1 = newXMin - 1;
                var neighborX2 = newXMin - BORDER_SIZE - 1;
                var neighborYMin = rect.YMax - heightMin + 1;

                if (!ValidPosition(neighborX1, neighborYMin) || !ValidPosition(neighborX2, neighborYMin))
                    break;

                if (_world.ExitTileTypeAny(neighborX1, neighborYMin, neighborX1, rect.YMax, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborX2, neighborYMin, neighborX2, rect.YMax, TileType.Corridor, TileType.Room))
                    break;

                newXMin--;
            }

            if (newXMin < WidthMin || newXMax > WidthMax)
                return null;

            if (newXMax - newXMin + 1 < widthMin)
                return null;

            if (newXMin > rect.CenterX || newXMax < rect.CenterX)
                return null;

            var newYMin = rect.YMin;
            var newYMax = rect.YMax;

            for (int y = rect.YMin; y <= rect.YMax; y++)
            {
                if(y < HeightMin)
                {
                    newYMin = y + 1;
                    continue;
                }

                for (int x = newXMin; x <= newXMax; x++)
                {
                    var tile = _world.GetTile(x, y);
                    if(tile.Type == TileType.Corridor || tile.Type == TileType.Room)
                    {
                        newYMin = y + BORDER_SIZE + 1;
                        break;
                    }
                    else if(tile.Type == TileType.Wall || tile.Type == TileType.Door)
                    {
                        newYMin = y + 1;
                        break;
                    }
                }
            }

            if (newYMin < HeightMin || newYMax > HeightMax)
                return null;

            if (newYMax - newYMin + 1 < heightMin)
                return null;

            if (newYMin > rect.CenterY || newYMax < rect.CenterY)
                return null;

            return new Rect(newXMin, newYMin, newXMax, newYMax);
        }

        Rect? ReadjustWithEastDoor(Rect rect, int widthMin, int heightMin)
        {
            var newYMin = rect.YMin;
            var newYMax = rect.YMax;

            for (int y = 0; y < rect.Height / 2; y++)
            {
                var neighborXMin = rect.XMax - widthMin + 1;

                var bottomY = rect.YMin + y;
                if (!ValidPosition(neighborXMin, bottomY) || _world.ExitTileTypeAny(neighborXMin, bottomY, rect.XMax, bottomY, TileType.Corridor, TileType.Room))
                    newYMin = bottomY + BORDER_SIZE + 1;
                else if (_world.ExitTileTypeAny(neighborXMin, bottomY, rect.XMax, bottomY, TileType.Wall, TileType.Door))
                    newYMin = bottomY + 1;

                var topY = rect.YMax - y;
                if (!ValidPosition(neighborXMin, topY) || _world.ExitTileTypeAny(neighborXMin, topY, rect.XMax, topY, TileType.Corridor, TileType.Room))
                    newYMax = topY - BORDER_SIZE - 1;
                else if (_world.ExitTileTypeAny(neighborXMin, topY, rect.XMax, topY, TileType.Wall, TileType.Door))
                    newYMax = topY - 1;
            }

            var missingYMin = newYMin - rect.YMin;
            var missingYMax = rect.YMax - newYMax;

            for (int i = 0; i < missingYMin; i++)
            {
                var neighborXMin = rect.XMax - widthMin + 1;
                var neighborY1 = newYMax + 1;
                var neighborY2 = newYMax + BORDER_SIZE + 1;

                if (!ValidPosition(neighborXMin, neighborY1) || !ValidPosition(neighborXMin, neighborY2))
                    break;

                if (_world.ExitTileTypeAny(neighborXMin, neighborY1, rect.XMax, neighborY1, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborXMin, neighborY2, rect.XMax, neighborY2, TileType.Corridor, TileType.Room))
                    break;

                newYMax++;
            }

            for (int i = 0; i < missingYMax; i++)
            {
                var neighborXMin = rect.XMax - widthMin + 1;
                var neighborY1 = newYMin - 1;
                var neighborY2 = newYMin - BORDER_SIZE - 1;

                if (!ValidPosition(neighborXMin, neighborY1) || !ValidPosition(neighborXMin, neighborY2))
                    break;

                if (_world.ExitTileTypeAny(neighborXMin, neighborY1, rect.XMax, neighborY1, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborXMin, neighborY2, rect.XMax, neighborY2, TileType.Corridor, TileType.Room))
                    break;

                newYMin--;
            }

            if (newYMin < HeightMin || newYMax > HeightMax)
                return null;

            if (newYMax - newYMin + 1 < heightMin)
                return null;

            if (newYMin > rect.CenterY || newYMax < rect.CenterY)
                return null;

            var newXMin = rect.XMin;
            var newXMax = rect.XMax;

            for (int x = rect.XMin; x <= rect.XMax; x++)
            {
                if(x < WidthMin)
                {
                    newXMin = x + 1;
                    continue;
                }

                for (int y = newYMin; y <= newYMax; y++)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type == TileType.Corridor || tile.Type == TileType.Room)
                    {
                        newXMin = x + BORDER_SIZE + 1;
                        break;
                    }
                    else if(tile.Type == TileType.Wall || tile.Type == TileType.Door)
                    {
                        newXMin = x + 1;
                        break;
                    }
                }
            }

            if (newXMin < WidthMin || newXMax > WidthMax)
                return null;

            if (newXMax - newXMin + 1 < widthMin)
                return null;

            if (newXMin > rect.CenterX || newXMax < rect.CenterX)
                return null;

            return new Rect(newXMin, newYMin, newXMax, newYMax);
        }

        Rect? ReadjustWithWestDoor(Rect rect, int widthMin, int heightMin)
        {
            var newYMin = rect.YMin;
            var newYMax = rect.YMax;

            for (int y = 0; y < rect.Height / 2; y++)
            {
                var neighborXMax = rect.XMin + widthMin - 1;

                var bottomY = rect.YMin + y;
                if (!ValidPosition(neighborXMax, bottomY) || _world.ExitTileTypeAny(rect.XMin, bottomY, neighborXMax, bottomY, TileType.Corridor, TileType.Room))
                    newYMin = bottomY + BORDER_SIZE + 1;
                else if (_world.ExitTileTypeAny(rect.XMin, bottomY, neighborXMax, bottomY, TileType.Wall, TileType.Door))
                    newYMin = bottomY + 1;

                var topY = rect.YMax - y;
                if (!ValidPosition(neighborXMax, topY) || _world.ExitTileTypeAny(rect.XMin, topY, neighborXMax, topY, TileType.Corridor, TileType.Room))
                    newYMax = topY - BORDER_SIZE - 1;
                else if (_world.ExitTileTypeAny(rect.XMin, topY, neighborXMax, topY, TileType.Wall, TileType.Door))
                    newYMax = topY - 1;
            }

            var missingYMin = newYMin - rect.YMin;
            var missingYMax = rect.YMax - newYMax;

            for (int i = 0; i < missingYMin; i++)
            {
                var neighborXMax = rect.XMin + widthMin - 1;
                var neighborY1 = newYMax + 1;
                var neighborY2 = newYMax + BORDER_SIZE + 1;

                if (!ValidPosition(neighborXMax, neighborY1) || !ValidPosition(neighborXMax, neighborY2))
                    break;

                if (_world.ExitTileTypeAny(rect.XMin, neighborY1, neighborXMax, neighborY1, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(rect.XMin, neighborY2, neighborXMax, neighborY2, TileType.Corridor, TileType.Room))
                    break;

                newYMax++;
            }

            for (int i = 0; i < missingYMax; i++)
            {
                var neighborXMax = rect.XMin + widthMin - 1;
                var neighborY1 = newYMin - 1;
                var neighborY2 = newYMin - BORDER_SIZE - 1;

                if (ValidPosition(neighborXMax, neighborY1) || ValidPosition(neighborXMax, neighborY2))
                    break;

                if (_world.ExitTileTypeAny(rect.XMin, neighborY1, neighborXMax, neighborY1, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(rect.XMin, neighborY2, neighborXMax, neighborY2, TileType.Corridor, TileType.Room))
                    break;

                newYMin--;
            }

            if (newYMin < HeightMin || newYMax > HeightMax)
                return null;

            if (newYMax - newYMin + 1 < heightMin)
                return null;

            if (newYMin > rect.CenterY || newYMax < rect.CenterY)
                return null;

            var newXMin = rect.XMin;
            var newXMax = rect.XMax;

            for (int x = rect.XMax; x >= rect.XMin; x--)
            {
                if(x > WidthMax)
                {
                    newXMax = x - 1;
                    continue;
                }

                for (int y = newYMin; y <= newYMax; y++)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type == TileType.Corridor || tile.Type == TileType.Room)
                    {
                        newXMax = x - BORDER_SIZE - 1;
                        break;
                    }
                    else if (tile.Type == TileType.Wall || tile.Type == TileType.Door)
                    {
                        newXMax = x - 1;
                        break;
                    }
                }
            }

            if (newXMin < WidthMin || newXMax > WidthMax)
                return null;

            if (newXMax - newXMin + 1 < widthMin)
                return null;

            if (newXMin > rect.CenterX || newXMax < rect.CenterX)
                return null;

            return new Rect(newXMin, newYMin, newXMax, newYMax);
        }

        Rect? ReadjustWithSouthDoor(Rect rect, int widthMin, int heightMin)
        {
            var newXMin = rect.XMin;
            var newXMax = rect.XMax;

            for (int x = 0; x < rect.Width / 2; x++)
            {
                var neightborTileYMax = rect.YMin + heightMin - 1;

                var leftTileX = rect.XMin + x;
                if (!ValidPosition(leftTileX, neightborTileYMax) || _world.ExitTileTypeAny(leftTileX, rect.YMin, leftTileX, neightborTileYMax, TileType.Corridor, TileType.Room))
                    newXMin = leftTileX + BORDER_SIZE + 1;
                else if (_world.ExitTileTypeAny(leftTileX, rect.YMin, leftTileX, neightborTileYMax, TileType.Wall, TileType.Door))
                    newXMin = leftTileX + 1;

                var rightTileX = rect.XMax - x;
                if (!ValidPosition(rightTileX, neightborTileYMax) || _world.ExitTileTypeAny(rightTileX, rect.YMin, rightTileX, neightborTileYMax, TileType.Corridor, TileType.Room))
                    newXMax = rightTileX - BORDER_SIZE - 1;
                else if (_world.ExitTileTypeAny(rightTileX, rect.YMin, rightTileX, neightborTileYMax, TileType.Wall, TileType.Door))
                    newXMax = rightTileX - 1;
            }

            var missingXMin = newXMin - rect.XMin;
            var missingXMax = rect.XMax - newXMax;

            for (int i = 0; i < missingXMin; i++)
            {
                var neighborX1 = newXMax + 1;
                var neighborX2 = newXMax + BORDER_SIZE + 1;
                var neighborYMax = rect.YMin + heightMin - 1;

                if (!ValidPosition(neighborX1, neighborYMax) || !ValidPosition(neighborX2, neighborYMax))
                    break;

                if (_world.ExitTileTypeAny(neighborX1, rect.YMin, neighborX1, neighborYMax, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborX2, rect.YMin, neighborX2, neighborYMax, TileType.Corridor, TileType.Room))
                    break;
                
                newXMax++;
            }

            for (int i = 0; i < missingXMax; i++)
            {
                var neighborX1 = newXMin - 1;
                var neighborX2 = newXMin - BORDER_SIZE - 1;
                var neighborYMax = rect.YMin + heightMin - 1;

                if (!ValidPosition(neighborX1, neighborYMax) || !ValidPosition(neighborX2, neighborYMax))
                    break;

                if (_world.ExitTileTypeAny(neighborX1, rect.YMin, neighborX1, neighborYMax, TileType.Wall, TileType.Door) || _world.ExitTileTypeAny(neighborX2, rect.YMin, neighborX2, neighborYMax, TileType.Corridor, TileType.Room))
                    break;

                newXMin--;
            }

            if (newXMin < WidthMin || newXMax > WidthMax)
                return null;

            if (newXMax - newXMin + 1 < widthMin)
                return null;

            if (newXMin > rect.CenterX || newXMax < rect.CenterX)
                return null;

            var newYMin = rect.YMin;
            var newYMax = rect.YMax;

            for (int y = rect.YMax; y >= rect.YMin; y--)
            {
                if(y > HeightMax)
                {
                    newYMax = y - 1;
                    continue;
                }

                for (int x = newXMin; x <= newXMax; x++)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type == TileType.Corridor || tile.Type == TileType.Room)
                    {
                        newYMax = y - BORDER_SIZE - 1;
                        break;
                    }
                    else if (tile.Type == TileType.Wall || tile.Type == TileType.Door)
                    {
                        newYMax = y - 1;
                        break;
                    }
                }
            }

            if (newYMin < HeightMin || newYMax > HeightMax)
                return null;

            if (newYMax - newYMin + 1 < heightMin)
                return null;

            if (newYMin > rect.CenterY || newYMax < rect.CenterY)
                return null;

            return new Rect(newXMin, newYMin, newXMax, newYMax);
        }
       
        Tile GetDoorTile(Rect rect, Direction doorDir, bool aligned = false) => doorDir switch
        {
            Direction.North => GetDoorTileInNorth(rect, aligned),
            Direction.East => GetDoorTileInEast(rect, aligned),
            Direction.West => GetDoorTileInWest(rect, aligned),
            Direction.South => GetDoorTileInSouth(rect, aligned)
        };

        Tile GetDoorTileInNorth(Rect rect, bool aligned = false)
        {
            if (aligned)
            {
                var borderTile = _world.GetTile(rect.CenterX, rect.YMax + BORDER_SIZE);
                var topTile = _world.GetTile(rect.CenterX, rect.YMax + BORDER_SIZE + 1);
                if (topTile.Type == TileType.Corridor || topTile.Type == TileType.Door)
                    return borderTile;
            }

            var allTiles = new List<Tile>();
            var candidatedTiles = new List<Tile>();
            for (int x = rect.XMin; x <= rect.XMax; x++)
            {
                var borderTile = _world.GetTile(x, rect.YMax + BORDER_SIZE);
                var topTile = _world.GetTile(x, rect.YMax + BORDER_SIZE + 1);
                if (topTile.Type == TileType.Corridor || topTile.Type == TileType.Door)
                {
                    candidatedTiles.Add(borderTile);
                }

                allTiles.Add(borderTile);
            }

            if (candidatedTiles.Count <= 0)
                return allTiles.GetRandomElement();

            return candidatedTiles.GetRandomElement();
        }

        Tile GetDoorTileInEast(Rect rect, bool aligned = false)
        {
            if (aligned)
            {
                var borderTile = _world.GetTile(rect.XMax + BORDER_SIZE, rect.CenterY);
                var rightTile = _world.GetTile(rect.XMax + BORDER_SIZE + 1, rect.CenterY);
                if (rightTile.Type == TileType.Corridor || rightTile.Type == TileType.Room)
                    return borderTile;
            }

            var allTiles = new List<Tile>();
            var candidatedTile = new List<Tile>();
            for (int y = rect.YMin; y <= rect.YMax; y++)
            {
                var borderTile = _world.GetTile(rect.XMax + BORDER_SIZE, y);
                var rightTile = _world.GetTile(rect.XMax + BORDER_SIZE + 1, y);
                if (rightTile.Type == TileType.Corridor || rightTile.Type == TileType.Room)
                {
                    candidatedTile.Add(borderTile);
                }

                allTiles.Add(borderTile);
            }

            if (candidatedTile.Count <= 0)
                return allTiles.GetRandomElement();

            return candidatedTile.GetRandomElement();
        }

        Tile GetDoorTileInWest(Rect rect, bool aligned = false)
        {
            if (aligned)
            {
                var borderTile = _world.GetTile(rect.XMin - BORDER_SIZE, rect.CenterY);
                var leftTile = _world.GetTile(rect.XMin - BORDER_SIZE - 1, rect.CenterY);
                if (leftTile.Type == TileType.Corridor || leftTile.Type == TileType.Door)
                    return borderTile;
            }

            var allTiles = new List<Tile>();
            var candidatedTile = new List<Tile>();
            for (int y = rect.YMin; y <= rect.YMax; y++)
            {
                var borderTile = _world.GetTile(rect.XMin - BORDER_SIZE, y);
                var leftTile = _world.GetTile(rect.XMin - BORDER_SIZE - 1, y);
                if (leftTile.Type == TileType.Corridor || leftTile.Type == TileType.Door)
                {
                    candidatedTile.Add(borderTile);
                }

                allTiles.Add(borderTile);
            }

            if(candidatedTile.Count <= 0)
                return allTiles.GetRandomElement();

            return candidatedTile.GetRandomElement();
        }

        Tile GetDoorTileInSouth(Rect rect, bool aligned = false)
        {
            if (aligned)
            {
                var borderTile = _world.GetTile(rect.CenterX, rect.YMin - BORDER_SIZE);
                var bottomTile = _world.GetTile(rect.CenterX, rect.YMin - BORDER_SIZE - 1);
                if (bottomTile.Type == TileType.Corridor || bottomTile.Type == TileType.Door)
                    return borderTile;
            }

            var allTiles = new List<Tile>();
            var candidatedTile = new List<Tile>();
            for (int x = rect.XMin; x <= rect.XMax; x++)
            {
                var borderTile = _world.GetTile(x, rect.YMin - BORDER_SIZE);
                var bottomTile = _world.GetTile(x, rect.YMin - BORDER_SIZE - 1);
                if (bottomTile.Type == TileType.Corridor || bottomTile.Type == TileType.Door)
                {
                    candidatedTile.Add(borderTile);
                }

                allTiles.Add(borderTile);
            }

            if(candidatedTile.Count <= 0)
                return allTiles.GetRandomElement();

            return candidatedTile.GetRandomElement();
        }

        bool ValidPosition(int x, int y) => x >= WidthMin && x <= WidthMax && y >= HeightMin && y <= HeightMax;
    }
}
