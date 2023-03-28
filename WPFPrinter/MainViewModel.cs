

using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using TunnelingAlgorithm;

namespace WPFPrinter
{
    public class MainViewModel : INotifyPropertyChanged
    {
        const string PARAM_PATH = "D:\\Code\\TunnelingAlgorithm\\TunnelingAlgorithm\\Param.json";

        public event PropertyChangedEventHandler? PropertyChanged;

        WorldGenerator _generator = new WorldGenerator();

        int _worldWidth = 150;
        int _worldHeight = 150;

        Bitmap _bitmap;

        public ActionCommand GenerateCommand { get; set; }
        public BitmapImage BitmapImage => _bitmap.ConvertToBitmapImage();
        public float EmptyPercentage { get; set; }

        public MainViewModel()
        {          
            _bitmap = new Bitmap(_worldWidth, _worldHeight);

            GenerateCommand = new ActionCommand(GenerateWorld);
        }

        void GenerateWorld()
        {
            var tiles = _generator.Generate(_worldWidth, _worldHeight, PARAM_PATH);
            var emptyTileCount = 0f;

            for (int y = 0; y < _worldHeight; y++)
            {
                for (int x = 0; x < _worldWidth; x++)
                {
                    var tile = tiles[x, _worldHeight - y - 1];
                    Color color;
                    switch (tile.Type)
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

            EmptyPercentage = emptyTileCount / (_worldWidth * _worldHeight) * 100;

            OnPropertyChanged(nameof(EmptyPercentage));
            OnPropertyChanged(nameof(BitmapImage));
        }


        void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
