
using System.Diagnostics;

namespace TunnelingAlgorithm
{
    [DebuggerDisplay("{X}, {Y}")]
    public struct Position
    {
        public int X { get; }
        public int Y { get; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
