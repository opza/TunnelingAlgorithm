

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using TunnelingAlgorithm;

namespace WPFPrinter
{
    public class MainViewModel : INotifyPropertyChanged
    {
        const string PARAM_PATH = "E:\\Code\\Other\\TunnelingAlgorithm\\Param.json";

        public event PropertyChangedEventHandler? PropertyChanged;

        int _worldWidth = 150;
        int _worldHeight = 150;

        WorldGenerator _worldGenerator;

        Random _rand = new Random();
        Bitmap _bitmap;

        public ActionCommand GenerateCommand { get; set; }
        public BitmapImage BitmapImage => _bitmap.ConvertToBitmapImage();
        public float EmptyPercentage { get; set; }

        public MainViewModel()
        {
            _worldGenerator = new WorldGenerator(PARAM_PATH);
            _bitmap = new Bitmap(_worldWidth, _worldHeight);

            GenerateCommand = new ActionCommand(GenerateWorld);
        }

        void GenerateWorld()
        {
            var seed = _rand.Next();
            Debug.WriteLine($"Seed : {seed}");

            (var tiles, var rooms) = _worldGenerator.Generate(_worldWidth, _worldHeight, seed);
            var emptyTileCount = 0f;

            for (int y = 0; y < _worldHeight; y++)
            {
                for (int x = 0; x < _worldWidth; x++)
                {
                    var tile = tiles[x, _worldHeight - y - 1];
                    Color color;
                    switch (tile)
                    {
                        case TileType.Rock:
                            color = Color.Brown;
                            emptyTileCount++;
                            break;
                        case TileType.Corridor:
                            color = Color.White;
                            break;
                        case TileType.Room:
                            color = Color.Gray;
                            break;
                        case TileType.Wall:
                            color = Color.Black;
                            break;
                        case TileType.Door:
                            color = Color.Green;
                            break;
                        default:
                            continue;
                    }

                    _bitmap.SetPixel(x, y, color);
                }
            }

            var officeData = rooms.First(roomData => roomData.RoomType == RoomType.Office);
            foreach (var min in officeData.Min)
            {
                for (int x = min.x; x < min.x + officeData.Width; x++)
                {
                    for (int y = min.y; y < min.y + officeData.Height; y++)
                    {
                        _bitmap.SetPixel(x, _worldWidth - y - 1, Color.Blue);
                    }
                }
            }

            EmptyPercentage = emptyTileCount / (_worldWidth * _worldHeight) * 100;

            OnPropertyChanged(nameof(EmptyPercentage));
            OnPropertyChanged(nameof(BitmapImage));

            foreach (var roomData in rooms)
            {
                Debug.WriteLine($"{roomData.RoomType} : {roomData.Count}");
            }
        }


        void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
