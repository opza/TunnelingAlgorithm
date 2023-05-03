using System;
using System.Collections.Generic;
using System.Text;

namespace TunnelingAlgorithm
{
    public class RoomData
    {
        RoomType _roomType;

        int _width;
        int _height;
        List<(int x, int y)> _min = new List<(int x, int y)>();
        

        public RoomType RoomType => _roomType;

        public int Width => _width;
        public int Height => _height;
        public (int x, int y)[] Min => _min.ToArray();

        public int Count => _min.Count;

        public RoomData(RoomType roomType, int width, int height)
        {
            _roomType = roomType;
            _width = width;
            _height = height;
        }

        public void AddPosition(int xMin, int yMin) => _min.Add((xMin, yMin));

    }
}
