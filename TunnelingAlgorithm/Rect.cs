using System.Diagnostics;
using System.Numerics;

namespace TunnelingAlgorithm
{
    [DebuggerDisplay("{XMin}, {YMin}, {XMax}, {YMax}")]
    public struct Rect
    {
        public Position MinPosition { get; set; }
        public Position MaxPosition { get; set; }

        public int XMin => MinPosition.X;
        public int YMin => MinPosition.Y;
        public int XMax => MaxPosition.X;
        public int YMax => MaxPosition.Y;

        public int Width => XMax - XMin + 1;
        public int Height => YMax - YMin + 1;

        public int CenterX => XMin + (Width / 2);
        public int CenterY => YMin + (Height / 2);

        public static Rect Shift(Rect rect, int xAxis, int yAxis)
        {
            var shiftedMinPos = new Position(rect.XMin + xAxis, rect.YMin + yAxis);
            var shiftedMaxPos = new Position(rect.XMax+ xAxis, rect.YMax + yAxis);

            return new Rect(shiftedMinPos, shiftedMaxPos);
        }

        public Rect(Position minPosition, Position maxPosition)
        {
            MinPosition = minPosition;
            MaxPosition = maxPosition;
        }

        public Rect(int xMin, int yMin, int xMax, int yMax)
        {
            MinPosition = new Position(xMin, yMin);
            MaxPosition = new Position(xMax, yMax);
        }
    }
}
