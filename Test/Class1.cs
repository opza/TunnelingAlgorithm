namespace Test
{
    public record MyPosition
    {
        public int X { get; }
        public int Y { get; }

        public MyPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}