using TunnelingAlgorithm.Configurations;
using OpzaUtil.Linq;

namespace TunnelingAlgorithm
{
    internal class MainTunneler : Tunneler
    {
        ChilderMainTunnelerFromMainTunneler _childerMainTunneler;
        ChilderJoinTunnelerFromMainTunneler _childerJoinTunneler;

        Direction _rootDir;
        int _straightCount;
        bool _hasEnterSplitPoint;

        public Direction RootDir => _rootDir;

        public MainTunneler(World world, Config config, int gen, Position startPivot, int tunnelSize, Direction rootDir, Direction dir, bool hasEnterSplitPoint = false, int? seed = null)
            : base(world, config, gen, startPivot, tunnelSize, dir, seed)
        {
            _childerMainTunneler = new ChilderMainTunnelerFromMainTunneler(this);
            _childerJoinTunneler = new ChilderJoinTunnelerFromMainTunneler(this);

            _hasEnterSplitPoint = hasEnterSplitPoint;
            _rootDir = rootDir;
        }



        public override void BuildCorridor(bool hasTailRoom)
        {
            if (!Alive)
                return;

            (Rect rect, Direction dir)? nextCorridor = null;
            (Rect rect, SplitPointType type)? hall = null;

            var straightLength = _speed * _straightCount;
            var hasChangedDirectionChance = straightLength >= _tunnelSize && straightLength >= _config.CorridorMinDist[_gen] && _rand.Next(0, 100) < _config.ProbChangeDirection[_gen];

            if (!HasCorridor)
            {
                var rect = GetCorridor(_startPivot, _startDir);
                if (!rect.HasValue)
                {
                    Kill();
                    return;
                }

                nextCorridor = (rect.Value, _startDir);
            }
            else
            {
                if (hasChangedDirectionChance)
                {
                    var corridorAndHall = GetNextCorridorInOtherDirection(_currCorridor.rect, _currCorridor.dir);

                    nextCorridor = corridorAndHall.nextCorridor;
                    hall = corridorAndHall.hall;
                }
                else
                {
                    var corridorAndHall = GetNextCorridor(_currCorridor.rect, _currCorridor.dir);

                    nextCorridor = corridorAndHall.nextCorridor;
                    hall = corridorAndHall.hall;
                }
            }           

            if (!nextCorridor.HasValue)
            {
                Kill();
                return;
            }

            if (_hasEnterSplitPoint && _currLife == _maxLife)
            {
                var enterSplitPoint = CreateCorridorSplitPoint(SplitPointType.None, nextCorridor.Value.rect, GetReverseDirection(nextCorridor.Value.dir));
                enterSplitPoint.UpdateState(_world);

                _splitPoints.Add(enterSplitPoint);
            }

            if (hall.HasValue)
            {
                var hallSplitPoint = new SplitPoint(hall.Value.type, hall.Value.rect);
                hallSplitPoint.UpdateState(_world);

                _splitPoints.Add(hallSplitPoint);

                BuildCorridor(hall.Value.rect);
                BuildBorder(hall.Value.rect);
            }

            BuildCorridor(nextCorridor.Value.rect);
            BuildBorder(nextCorridor.Value.rect);

            if (_currCorridor.dir != nextCorridor.Value.dir || hall.HasValue)
                _straightCount = 1;
            else
                _straightCount++;

            _currCorridor = nextCorridor.Value;

            if(HittingCorridor(_currCorridor.rect, _currCorridor.dir))
            {
                Kill();
                return;
            }

            var tailSplitPoint = CreateCorridorSplitPoint(SplitPointType.None, _currCorridor.rect, _currCorridor.dir);
            tailSplitPoint.UpdateState(_world);

            _splitPoints.Add(tailSplitPoint);

            _currLife--;

            if (!Alive)
            {
                if (hasTailRoom)
                    BuildRoom(_splitPoints.Last(), true);
            }

            _splitPoints.ForEach(splitPoint => splitPoint.UpdateState(_world));
        }

        public override Tunneler CreateChild(int? seed = null)
        {
            if (_rand.Next(0, 100) < _config.ProbChangeJoinTunneler[_gen])
                return _childerJoinTunneler.CreateChild(seed);

            return _childerMainTunneler.CreateChild(seed);
        }

        public override Tunneler[] CreateScaleUpChilds(int? seed = null) => _childerMainTunneler.CreateChildAll(SplitPointType.ScaleUp, _tunnelSize + 2, seed);

        public override Tunneler[] CreateScaleDownChilds(int? seed = null) => _childerMainTunneler.CreateChildAll(SplitPointType.ScaleDown, _tunnelSize - 2, seed);

        protected override Direction[] GetCandidatedDirections(Direction currDir)
        {
            var candidatedDirs = Enum.GetValues<Direction>().ToList();
            candidatedDirs.Remove(GetReverseDirection(currDir));
            candidatedDirs.Remove(currDir);

            //if (_gen < 1)
            //    candidatedDirs.Remove(GetReverseDirection(_startDir));
            //else
            //    candidatedDirs.Remove(GetReverseDirection(_rootDir));

            //candidatedDirs.Remove(GetReverseDirection(_startDir));
            candidatedDirs.Remove(GetReverseDirection(_rootDir));

            candidatedDirs.Shuffle();

            return candidatedDirs.ToArray();
        }

        //MainTunneler CreateChildMainTunneler(int? seed)
        //{
        //    if (_splitPoints.Count <= 0)
        //        return null;

        //    var candidatedSplitPoints = new List<SplitPoint>(_splitPoints);
        //    candidatedSplitPoints.Shuffle();

        //    foreach (var splitPoint in candidatedSplitPoints)
        //    {
        //        var dirs = Enum.GetValues<Direction>().ToList();
        //        dirs.Remove(GetReverseDirection(_rootDir));
        //        dirs.Shuffle();

        //        foreach (var dir in dirs)
        //        {
        //            if (splitPoint[dir] == ConnectState.Connected)
        //                continue;

        //            var childTunnelSize = _tunnelSize;

        //            var widthHalf = childTunnelSize / 2;
        //            var heightHalf = childTunnelSize / 2;

        //            var pivot = new Position();
        //            switch (dir)
        //            {
        //                case Direction.North:
        //                    pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMax + 1);
        //                    break;
        //                case Direction.East:
        //                    pivot = new Position(splitPoint.XMax + 1, splitPoint.Rect.CenterY - heightHalf);
        //                    break;
        //                case Direction.West:
        //                    pivot = new Position(splitPoint.XMin - 1, splitPoint.Rect.CenterY - heightHalf);
        //                    break;
        //                case Direction.South:
        //                    pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMin - 1);
        //                    break;
        //            }

        //            if (pivot.X < WidthMin || pivot.X > WidthMax || pivot.Y < HeightMin || pivot.Y > HeightMax)
        //                continue;

        //            splitPoint[dir] = ConnectState.Connected;

        //            return new MainTunneler(_world, _config, _gen + 1, pivot, childTunnelSize, _rootDir, dir, false, seed);

        //        }
        //    }

        //    return null;
        //}

        //MainTunneler[] CreateChildsMainTunneler(SplitPointType type, int childTunnelSize, int? seed = null)
        //{
        //    var childs = new List<MainTunneler>();

        //    if (_splitPoints.Count <= 0 || childTunnelSize <= 0)
        //        return childs.ToArray();

        //    var candidatedSplitPoints = _splitPoints.Where(splitPoint => splitPoint.Type == type).ToList();
        //    candidatedSplitPoints.Shuffle();

        //    foreach (var splitPoint in candidatedSplitPoints)
        //    {
        //        foreach (var dir in Enum.GetValues<Direction>())
        //        {
        //            if (splitPoint[dir] == ConnectState.Connected)
        //                continue;

        //            var widthHalf = childTunnelSize / 2;
        //            var heightHalf = childTunnelSize / 2;

        //            var pivot = new Position();
        //            switch (dir)
        //            {
        //                case Direction.North:
        //                    pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMax + 1);
        //                    break;
        //                case Direction.East:
        //                    pivot = new Position(splitPoint.XMax + 1, splitPoint.Rect.CenterY - heightHalf);
        //                    break;
        //                case Direction.West:
        //                    pivot = new Position(splitPoint.XMin - 1, splitPoint.Rect.CenterY - heightHalf);
        //                    break;
        //                case Direction.South:
        //                    pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMin - 1);
        //                    break;
        //            }

        //            if (pivot.X < WidthMin || pivot.X > WidthMax || pivot.Y < HeightMin || pivot.Y > HeightMax)
        //                continue;

        //            splitPoint[dir] = ConnectState.Connected;

        //            childs.Add(new MainTunneler(_world, _config, _gen + 1, pivot, childTunnelSize, _rootDir, dir, false, seed));

        //        }
        //    }

        //    return childs.ToArray();
        //}   

        //JoinTunneler CreateChildJoinTunneler(int? seed)
        //{
        //    if (_splitPoints.Count <= 0)
        //        return null;

        //    var candidatedSplitPoints = new List<SplitPoint>(_splitPoints);
        //    candidatedSplitPoints.Shuffle();

        //    foreach (var splitPoint in candidatedSplitPoints)
        //    {
        //        var dirs = Enum.GetValues<Direction>();
        //        dirs.Shuffle();

        //        foreach (var dir in dirs)
        //        {
        //            if (splitPoint[dir] == ConnectState.Connected)
        //                continue;

        //            var pivot = dir switch
        //            {
        //                Direction.North => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin, splitPoint.YMax + 1) : new Position(splitPoint.XMax - _config.JoinTunnelerSize + 1, splitPoint.YMax + 1),
        //                Direction.East => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMax + 1, splitPoint.YMin) : new Position(splitPoint.XMax + 1, splitPoint.YMax - _config.JoinTunnelerSize + 1),
        //                Direction.West => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin - 1, splitPoint.YMin) : new Position(splitPoint.XMin - 1, splitPoint.YMax - _config.JoinTunnelerSize + 1),
        //                Direction.South => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin, splitPoint.YMin - 1) : new Position(splitPoint.XMax - _config.JoinTunnelerSize + 1, splitPoint.YMin - 1)
        //            };

        //            if (pivot.X < WidthMin || pivot.X > WidthMax || pivot.Y < HeightMin || pivot.Y > HeightMax)
        //                continue;

        //            splitPoint[dir] = ConnectState.Connected;

        //            return new JoinTunneler(_world, _config, _gen + 1, pivot, dir, seed);
        //        }
        //    }

        //    return null;
        //}
    }
}
