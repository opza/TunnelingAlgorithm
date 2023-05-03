using System;
using System.Linq;
using System.Collections.Generic;
using TunnelingAlgorithm.Configurations;
using OpzaUtil.Linq;
using OpzaUtil;

namespace TunnelingAlgorithm
{
    internal class JoinTunneler : Tunneler
    {
        ChildCreatorOfMainTunnelerFromJoinTunneler _childCreatorOfMainTunneler;
        ChildCreatorOfJoinTunnelerFromJoinTunneler _childCreatorOfJoinTunneler;

        int _straightCount;

        public JoinTunneler(World world, Config config, RoomData[] buildedRooms, int gen, Position startPivot, Direction dir, int? seed) : base(world, config, buildedRooms, gen, startPivot, config.JoinTunnelerSize, dir, seed)
        {
            _childCreatorOfMainTunneler = new ChildCreatorOfMainTunnelerFromJoinTunneler(this);
            _childCreatorOfJoinTunneler = new ChildCreatorOfJoinTunnelerFromJoinTunneler(this);
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

            if (HittingCorridor(_currCorridor.rect, _currCorridor.dir))
            {               
                Kill();
                return;
            }

            if(HittingRoom(_currCorridor.rect, _currCorridor.dir))
            {
                var sideDoorSplitPoint = CreateCorridorSplitPoint(SplitPointType.None, _currCorridor.rect, _currCorridor.dir);
                sideDoorSplitPoint.UpdateState(_world);

                var sideDoor = GetSideDoor(sideDoorSplitPoint, _currCorridor.dir);
                if (sideDoor.HasValue)
                    BuildSideDoor(sideDoor.Value);

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
                {
                    var buildSuccess = BuildRoom(_splitPoints.Last(), true);
                    if (!buildSuccess)
                    {
                        _currLife++;
                        _splitPoints.ForEach(splitPoint => splitPoint.UpdateState(_world));
                        return;
                    }
                }

                var sideDoor = GetSideDoor(_splitPoints.Last());
                if (sideDoor.HasValue)
                    BuildSideDoor(sideDoor.Value);
            }

            _splitPoints.ForEach(splitPoint => splitPoint.UpdateState(_world));

        }

        public override Tunneler CreateChild(int? seed = null)
        {
            if (_rand.Next(0, 100) < _config.ProbChangeJoinTunneler[_gen])
                return _childCreatorOfJoinTunneler.CreateChild(seed);

            return _childCreatorOfMainTunneler.CreateChild(seed);
        }

        public override Tunneler[] CreateScaleUpChilds(int? seed = null) => _childCreatorOfMainTunneler.CreateChildAll(SplitPointType.ScaleUp, _tunnelSize + 2, seed);

        public override Tunneler[] CreateScaleDownChilds(int? seed = null) => _childCreatorOfMainTunneler.CreateChildAll(SplitPointType.ScaleDown, _tunnelSize - 2, seed);

        Position? GetSideDoor(SplitPoint splitPoint)
        {
            var candidates = new List<Position>();

            var dirs = Enum.GetValues(typeof(Direction))
                .ToEnumerable<Direction>()
                .ToArray();

            dirs.Shuffle();

            foreach (var dir in dirs)
            {
                if (splitPoint[dir] == ConnectState.NonConnected)
                    continue;

                var candidatesInDirection = dir switch
                {
                    Direction.North => GetSideDoorCandidatesInNorth(splitPoint),
                    Direction.East => GetSideDoorCandidatesInEast(splitPoint),
                    Direction.West => GetSideDoorCandidatesInWest(splitPoint),
                    Direction.South => GetSideDoorCandidatesInSouth(splitPoint)
                };

                candidates.AddRange(candidatesInDirection);
            }

            if (candidates.Count <= 0)
                return null;

            return candidates.GetRandomElement();
        }

        Position? GetSideDoor(SplitPoint splitPoint, Direction dir)
        {
            if (splitPoint[dir] == ConnectState.NonConnected)
                return null;

            var candidatesInDirection = dir switch
            {
                Direction.North => GetSideDoorCandidatesInNorth(splitPoint),
                Direction.East => GetSideDoorCandidatesInEast(splitPoint),
                Direction.West => GetSideDoorCandidatesInWest(splitPoint),
                Direction.South => GetSideDoorCandidatesInSouth(splitPoint)
            };


            return candidatesInDirection.GetRandomElementOrDefualt();
        }


        Position[] GetSideDoorCandidatesInNorth(SplitPoint splitPoint)
        {
            var candidates = new List<Position>();

            var doorPosY = splitPoint.YMax + 1;
            for (int doorPosX = splitPoint.XMin; doorPosX <= splitPoint.XMax; doorPosX++)
            {
                var currTile = _world.GetTile(doorPosX, doorPosY);
                var topTile = _world.GetTile(doorPosX, doorPosY + BORDER_SIZE);
                if(currTile?.Type == TileType.Wall && topTile?.Type == TileType.Room)
                {
                    var leftTile = _world.GetTile(doorPosX - 1, doorPosY);
                    var rightTile = _world.GetTile(doorPosX + 1, doorPosY);

                    if (leftTile?.Type != TileType.Door && rightTile?.Type != TileType.Door)
                        candidates.Add(new Position(doorPosX, doorPosY));
                }
            }

            return candidates.ToArray();
        }

        Position[] GetSideDoorCandidatesInEast(SplitPoint splitPoint)
        {
            var candidates = new List<Position>();

            var doorPosX = splitPoint.XMax + 1;
            for (int doorPosY = splitPoint.YMin; doorPosY <= splitPoint.YMax; doorPosY++)
            {
                var currTile = _world.GetTile(doorPosX, doorPosY);
                var rightTile = _world.GetTile(doorPosX + Roomer.BORDER_SIZE, doorPosY);
                if(currTile?.Type == TileType.Wall && rightTile?.Type == TileType.Room)
                {
                    var topTile = _world.GetTile(doorPosX, doorPosY + 1);
                    var bottomTile = _world.GetTile(doorPosX, doorPosY - 1);

                    if (topTile?.Type != TileType.Door && bottomTile?.Type != TileType.Door)
                        candidates.Add(new Position(doorPosX, doorPosY));
                }
            }

            return candidates.ToArray();
        }

        Position[] GetSideDoorCandidatesInWest(SplitPoint splitPoint)
        {
            var candidates = new List<Position>();

            var doorPosX = splitPoint.XMin - 1;
            for (int doorPosY = splitPoint.YMin; doorPosY <= splitPoint.YMax; doorPosY++)
            {
                var currTile = _world.GetTile(doorPosX, doorPosY);
                var leftTile = _world.GetTile(doorPosX - 1, doorPosY);
                if(currTile?.Type == TileType.Wall && leftTile?.Type == TileType.Room)
                {
                    var topTile = _world.GetTile(doorPosX, doorPosY + 1);
                    var bottomTile = _world.GetTile(doorPosX, doorPosY - 1);

                    if (topTile?.Type != TileType.Door && bottomTile?.Type != TileType.Door)
                        candidates.Add(new Position(doorPosX, doorPosY));
                }
            }

            return candidates.ToArray();
        }

        Position[] GetSideDoorCandidatesInSouth(SplitPoint splitPoint)
        {
            var candidates = new List<Position>();

            var doorPosY = splitPoint.YMin - 1;
            for (int doorPosX = splitPoint.XMin; doorPosX <= splitPoint.XMax; doorPosX++)
            {
                var currTile = _world.GetTile(doorPosX, doorPosY);
                var bottomTile = _world.GetTile(doorPosX, doorPosY - 1);
                if(currTile?.Type == TileType.Wall && bottomTile?.Type == TileType.Room)
                {
                    var leftTile = _world.GetTile(doorPosX - 1, doorPosY);
                    var rightTile = _world.GetTile(doorPosX + 1, doorPosY);

                    if (leftTile?.Type != TileType.Door && rightTile?.Type != TileType.Door)
                        candidates.Add(new Position(doorPosX, doorPosY));
                }
            }

            return candidates.ToArray();
        }

        void BuildSideDoor(Position doorPos)
        {
            var doorTile = _world.GetTile(doorPos.X, doorPos.Y);
            doorTile.Type = TileType.Door;
        }
    }
}
