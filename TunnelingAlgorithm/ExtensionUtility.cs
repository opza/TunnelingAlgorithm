namespace TunnelingAlgorithm
{
    public static class ExtensionUtility
    {
        static Random _random;

        static int _seed;

        public static int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                _random = new Random(_seed);
            }
        }

        static ExtensionUtility()
        {
            _seed = new Random().Next();
            _random = new Random(_seed);
        }

        public static T GetRandomElementOrDefault<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable.Count() <= 0)
                return default;

            return GetRandomElement(enumerable);
        }

        public static T GetRandomElement<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();
            if (list.Count <= 0)
                throw new Exception();

            var randIndex = _random.Next(list.Count);
            return list[randIndex];
        }   


        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                var n = _random.Next(i, list.Count);

                var temp = list[n];
                list[n] = list[i];
                list[i] = temp;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> act)
        {
            foreach (var item in enumerable)
            {
                act?.Invoke(item);
            }
        }

        public static T[] Flat<T>(this T[,] array2d)
        {
            var d0Len = array2d.GetLength(0);
            var d1Len = array2d.GetLength(1);

            var flattenArray = new T[d0Len * d1Len];
            for (int i = 0; i < d0Len; i++)
            {
                for (int j = 0; j < d1Len; j++)
                {
                    var idx = d1Len * i + j;
                    flattenArray[idx] = array2d[i, j];
                }
            }

            return flattenArray;
        }

        public static Direction GetReverseDirection(this Direction direction) => direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.South => Direction.North
        };
    }
}
