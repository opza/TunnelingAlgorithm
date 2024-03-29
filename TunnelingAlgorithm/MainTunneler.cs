﻿using System;
using System.Linq;
using TunnelingAlgorithm.Configurations;
using OpzaUtil.Linq;
using OpzaUtil;

namespace TunnelingAlgorithm
{
    internal class MainTunneler : Tunneler
    {
        ChildCreatorOfMainTunnelerFromMainTunneler _childCreatorOfMainTunneler;
        ChildCreatorOfJoinTunnelerFromMainTunneler _childCreatorJoinTunneler;

        Direction _rootDir;
        int _straightCount;
        bool _hasEnterSplitPoint;

        public Direction RootDir => _rootDir;

        public MainTunneler(World world, Config config, RoomData[] buildedRooms, int gen, Position startPivot, int tunnelSize, Direction rootDir, Direction dir, int seed, bool hasEnterSplitPoint = false)
            : base(world, config, buildedRooms, gen, startPivot, tunnelSize, dir, seed)
        {
            _childCreatorOfMainTunneler = new ChildCreatorOfMainTunnelerFromMainTunneler(this, _rand.Next());
            _childCreatorJoinTunneler = new ChildCreatorOfJoinTunnelerFromMainTunneler(this, _rand.Next());

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

        public override Tunneler CreateChild()
        {
            var childSeed = _rand.Next();

            if (_rand.Next(0, 100) < _config.ProbChangeJoinTunneler[_gen])
                return _childCreatorJoinTunneler.CreateChild(childSeed);

            return _childCreatorOfMainTunneler.CreateChild(childSeed);
        }

        public override Tunneler[] CreateScaleUpChilds()
        {
            var childSeed = _rand.Next();
            return _childCreatorOfMainTunneler.CreateChildAll(SplitPointType.ScaleUp, _tunnelSize + 2, childSeed);
        }

        public override Tunneler[] CreateScaleDownChilds()
        {
            var childSeed = _rand.Next();
            return _childCreatorOfMainTunneler.CreateChildAll(SplitPointType.ScaleDown, _tunnelSize - 2, childSeed);
        }

        protected override Direction[] GetCandidatedDirections(Direction currDir)
        {
            var candidatedDirs = Enum.GetValues(typeof(Direction))
                .ToEnumerable<Direction>()
                .ToList();

            candidatedDirs.Remove(GetReverseDirection(currDir));
            candidatedDirs.Remove(currDir);

            candidatedDirs.Remove(GetReverseDirection(_rootDir));

            candidatedDirs.Shuffle();

            return candidatedDirs.ToArray();
        }
    }
}
