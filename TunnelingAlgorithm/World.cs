using System;
using System.Collections.Generic;
using System.Linq;
using TunnelingAlgorithm.Configurations;
using OpzaUtil.Linq;

namespace TunnelingAlgorithm
{
    public enum Direction
    {
        North,
        East,
        West,
        South
    }

    public enum RoomType
    {
        None,
        Office,
        Lab,
        Restaurant,
        Warehouse,
        Isolation,
        Security,
        Communication,
        Rest,
        Infirmary,
        launchSite
    }

    internal class World
    {
        int _width;
        int _height;
        Tile[,] _tiles;

        public int Width => _width;
        public int Height => _height;

        public Tile[,] Tiles => _tiles;


        public World(int width, int height)
        {
            _width = width;
            _height = height;

            InitTiles();
        }

        public Tile GetTile(int x, int y)
        {
            if (!ValidPosition(x, y))
                return null;

            return _tiles[x, y];
        }

        public Tile[,] GetTiles(int minX, int minY, int maxX, int maxY)
        {
            if (minX < 0)
                minX = 0;

            if(minY < 0)
                minY = 0;

            if(maxX >= _width)
                maxX = _width - 1;

            if(maxY >= _height)
                maxY = _height - 1;

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            var tiles = new Tile[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = _tiles[minX + x, minY + y];
                }
            }

            return tiles;

        }

        public bool ExitTileTypeAll(Rect rect, params TileType[] types)
            => ExitTileTypeAll(rect.XMin, rect.YMin, rect.XMax, rect.YMax, types);

        public bool ExitTileTypeAll(int xMin, int yMin, int xMax, int yMax, params TileType[] types)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    var tile = GetTile(x, y);
                    if (tile == null)
                        return false;

                    if (!types.Contains(tile.Type))
                        return false;
                }
            }

            return true;
        }

        public bool ExitTileTypeAny(Rect rect, params TileType[] types)
            => ExitTileTypeAny(rect.XMin, rect.YMin, rect.XMax, rect.YMax, types);

        public bool ExitTileTypeAny(int xMin, int yMin, int xMax, int yMax, params TileType[] types)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    var tile = GetTile(x, y);

                    if (tile != null && types.Contains(tile.Type))
                        return true;
                }
            }

            return false;
        }

        public bool ValidPosition(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;

        public Tunneler[] Generate(Config config, int seed)
        {
            InitTiles();

            RandomEnumerable.Seed = seed;
            var rand = new Random(seed);

            var rootTunnelers = Tunneler.CreateRootTunnelers(this, config, seed);

            var lastTunnelers = new List<Tunneler>();
            var parentTunnelers = new List<Tunneler>(rootTunnelers);
            var allTunnelers = new List<Tunneler>();

            var generation = 0;
            while (parentTunnelers.Count > 0 && generation <= config.GenerationCount)
            {
                allTunnelers.AddRange(parentTunnelers);

                var childTunnelers = new List<Tunneler>();
                while (parentTunnelers.Any(tunneler => tunneler.Alive))
                {
                    foreach (var tunneler in parentTunnelers.Where(tunneler => tunneler.Alive))
                    {
                        tunneler.BuildCorridor(true);
                        if (!tunneler.Alive)
                        {
                            if (generation < config.GenerationCount - 1)
                            {
                                childTunnelers.AddRange(tunneler.CreateScaleUpChilds());
                                childTunnelers.AddRange(tunneler.CreateScaleDownChilds());

                                var canBornChildCount = tunneler.SplitPoints.Count(splitPoint => splitPoint.NonConnectedCount > 0);
                                for (int i = 0; i < canBornChildCount; i++)
                                {
                                    if (rand.Next(0, 100) < config.ProbBornTunneler[generation])
                                    {
                                        var child = tunneler.CreateChild();
                                        if (child == null)
                                            break;

                                        childTunnelers.Add(child);
                                    }
                                }

                                if(childTunnelers.Count < config.CountBornTunnelerMin[generation])
                                {
                                    for (int i = childTunnelers.Count; i < config.CountBornTunnelerMin[generation]; i++)
                                    {
                                        var child = tunneler.CreateChild();
                                        if (child == null)
                                            break;

                                        childTunnelers.Add(child);
                                    }
                                }
                            }
                        }
                    }                   
                }

                lastTunnelers.ForEach(tunneler => tunneler.SplitPoints.ForEach(splitPoint => splitPoint.UpdateState(this)));
                foreach (var lastTunneler in lastTunnelers)
                {
                    var canBuildRoomCount = lastTunneler.SplitPoints.Count(splitPoint => splitPoint.NonConnectedCount > 0);
                    for (int i = 0; i < canBuildRoomCount; i++)
                    {
                        if (rand.Next(0, 100) < config.ProbBuildRoom[generation])
                        {
                            lastTunneler.BuildRoom();
                        }
                    }
                }

                lastTunnelers = parentTunnelers;
                parentTunnelers = childTunnelers;
                generation++;
            }

            foreach (var lastTunneler in lastTunnelers)
            {
                var canBuildRoomCount = lastTunneler.SplitPoints.Sum(splitPoint => splitPoint.NonConnectedCount);
                for (int i = 0; i < canBuildRoomCount; i++)
                {
                    if (rand.Next(0, 100) < config.ProbBuildRoom[generation - 1])
                    {
                        lastTunneler.BuildRoom();
                    }
                }
            }

            allTunnelers.ForEach(tunneler => tunneler.BuildRoomAll());

            return allTunnelers.ToArray();

            //System.Diagnostics.Debug.WriteLine($"Ext Seed : {ExtensionUtility.Seed}");
            ////System.Diagnostics.Debug.WriteLine($"Tunneler Seed : {tunnelers[0].Seed}");


        }

        void InitTiles()
        {
            _tiles = new Tile[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _tiles[x, y] = new Tile(x, y);
                }
            }
        }
    }
}