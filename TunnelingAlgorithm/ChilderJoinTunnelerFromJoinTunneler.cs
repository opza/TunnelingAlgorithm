using System;
using System.Collections.Generic;
using OpzaUtil.Linq;

namespace TunnelingAlgorithm
{
    internal class ChilderJoinTunnelerFromJoinTunneler : Childer<JoinTunneler, JoinTunneler>
    {
        public ChilderJoinTunnelerFromJoinTunneler(JoinTunneler parent, int? seed = null) : base(parent, seed)
        {
        }

        public override JoinTunneler CreateChild(int? childSeed)
        {
            if (_parent.SplitPoints.Length <= 0)
                return null;

            var candidatedSplitPoints = new List<SplitPoint>(_parent.SplitPoints);
            candidatedSplitPoints.Shuffle();

            foreach (var splitPoint in candidatedSplitPoints)
            {
                var dirs = Enum.GetValues<Direction>();
                dirs.Shuffle();

                foreach (var dir in dirs)
                {
                    if (splitPoint[dir] == ConnectState.Connected)
                        continue;

                    var pivot = dir switch
                    {
                        Direction.North => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin, splitPoint.YMax + 1) : new Position(splitPoint.XMax - _parent.Config.JoinTunnelerSize + 1, splitPoint.YMax + 1),
                        Direction.East => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMax + 1, splitPoint.YMin) : new Position(splitPoint.XMax + 1, splitPoint.YMax - _parent.Config.JoinTunnelerSize + 1),
                        Direction.West => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin - 1, splitPoint.YMin) : new Position(splitPoint.XMin - 1, splitPoint.YMax - _parent.Config.JoinTunnelerSize + 1),
                        Direction.South => _rand.Next(0, 2) == 0 ? new Position(splitPoint.XMin, splitPoint.YMin - 1) : new Position(splitPoint.XMax - _parent.Config.JoinTunnelerSize + 1, splitPoint.YMin - 1)
                    };

                    if (pivot.X < _parent.WidthMin || pivot.X > _parent.WidthMax || pivot.Y < _parent.HeightMin || pivot.Y > _parent.HeightMax)
                        continue;

                    splitPoint[dir] = ConnectState.Connected;

                    return new JoinTunneler(_parent.World, _parent.Config, _parent.Generation + 1, pivot, dir, childSeed);
                }
            }

            return null;
        }
    }
}
