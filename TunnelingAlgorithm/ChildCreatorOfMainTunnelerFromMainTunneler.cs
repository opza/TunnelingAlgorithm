﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpzaUtil;
using OpzaUtil.Linq;

namespace TunnelingAlgorithm
{
    internal class ChildCreatorOfMainTunnelerFromMainTunneler : ChildCreator<MainTunneler, MainTunneler>
    {
        public ChildCreatorOfMainTunnelerFromMainTunneler(MainTunneler parent, int seed) : base(parent, seed)
        {
        }

        public override MainTunneler CreateChild(int seed)
        {
            if (_parent.SplitPoints.Length <= 0)
                return null;

            var candidatedSplitPoints = new List<SplitPoint>(_parent.SplitPoints);
            candidatedSplitPoints.Shuffle();

            foreach (var splitPoint in candidatedSplitPoints)
            {
                var dirs = Enum.GetValues(typeof(Direction))
                    .ToEnumerable<Direction>()
                    .ToList();

                dirs.Remove(GetReverseDirection(_parent.RootDir));
                dirs.Shuffle();

                foreach (var dir in dirs)
                {
                    if (splitPoint[dir] == ConnectState.Connected)
                        continue;

                    var childTunnelSize = _parent.TunnelSize;

                    var widthHalf = childTunnelSize / 2;
                    var heightHalf = childTunnelSize / 2;

                    var pivot = new Position();
                    switch (dir)
                    {
                        case Direction.North:
                            pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMax + 1);
                            break;
                        case Direction.East:
                            pivot = new Position(splitPoint.XMax + 1, splitPoint.Rect.CenterY - heightHalf);
                            break;
                        case Direction.West:
                            pivot = new Position(splitPoint.XMin - 1, splitPoint.Rect.CenterY - heightHalf);
                            break;
                        case Direction.South:
                            pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMin - 1);
                            break;
                    }

                    if (pivot.X < _parent.WidthMin || pivot.X > _parent.WidthMax || pivot.Y < _parent.HeightMin || pivot.Y > _parent.HeightMax)
                        continue;

                    splitPoint[dir] = ConnectState.Connected;

                    return new MainTunneler(_parent.World, _parent.Config, _parent.BuildedRooms, _parent.Generation + 1, pivot, childTunnelSize, _parent.RootDir, dir, seed, false);

                }
            }

            return null;
        }

        public MainTunneler[] CreateChildAll(SplitPointType type, int childTunnelSize, int seed)
        {
            var childs = new List<MainTunneler>();

            if (_parent.SplitPoints.Length <= 0 || childTunnelSize <= 0)
                return childs.ToArray();

            var candidatedSplitPoints = _parent.SplitPoints.Where(splitPoint => splitPoint.Type == type).ToList();
            candidatedSplitPoints.Shuffle();

            foreach (var splitPoint in candidatedSplitPoints)
            {
                var dirs = Enum.GetValues(typeof(Direction)).ToEnumerable<Direction>();
                foreach (var dir in dirs)
                {
                    if (splitPoint[dir] == ConnectState.Connected)
                        continue;

                    var widthHalf = childTunnelSize / 2;
                    var heightHalf = childTunnelSize / 2;

                    var pivot = new Position();
                    switch (dir)
                    {
                        case Direction.North:
                            pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMax + 1);
                            break;
                        case Direction.East:
                            pivot = new Position(splitPoint.XMax + 1, splitPoint.Rect.CenterY - heightHalf);
                            break;
                        case Direction.West:
                            pivot = new Position(splitPoint.XMin - 1, splitPoint.Rect.CenterY - heightHalf);
                            break;
                        case Direction.South:
                            pivot = new Position(splitPoint.Rect.CenterX - widthHalf, splitPoint.YMin - 1);
                            break;
                    }

                    if (pivot.X < _parent.WidthMin || pivot.X > _parent.WidthMax || pivot.Y < _parent.HeightMin || pivot.Y > _parent.HeightMax)
                        continue;

                    splitPoint[dir] = ConnectState.Connected;

                    childs.Add(new MainTunneler(_parent.World, _parent.Config, _parent.BuildedRooms, _parent.Generation + 1, pivot, childTunnelSize, _parent.RootDir, dir, seed, false));

                }
            }

            return childs.ToArray();
        }

        Direction GetReverseDirection(Direction direction) => direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.South => Direction.North
        };
    }
}
