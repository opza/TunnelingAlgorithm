

using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows.Documents;
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

        public MainViewModel()
        {          
            _bitmap = new Bitmap(_worldWidth, _worldHeight);

            GenerateCommand = new ActionCommand(GenerateWorld);
        }

        void GenerateWorld()
        {
            //var tunnelers = _world.Generate();

            var tiles = _generator.Generate(_worldWidth, _worldHeight, PARAM_PATH);

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

            //for (int y = 0; y < _world.Height; y++)
            //{
            //    for (int x = 0; x < _world.Width; x++)
            //    {
            //        var tile = _world.GetTile(x, _world.Height - y - 1);
            //        Color color;
            //        switch (tile.Type)
            //        {
            //            case TileType.Rock:
            //                color = Color.Brown;
            //                break;
            //            case TileType.Corridor:
            //                color = Color.White;
            //                break;
            //            case TileType.Room:
            //                color = Color.Gray;
            //                break;
            //            case TileType.Wall:
            //                color = Color.Black;
            //                break;
            //            case TileType.Door:
            //                color = Color.Green;
            //                break;
            //            default:
            //                continue;
            //        }

            //        _bitmap.SetPixel(x, y, color);
            //    }
            //}

            //foreach (var tunneler in tunnelers)
            //{
            //    foreach (var splitPoint in tunneler.SplitPoints)
            //    {
            //        for (int y = splitPoint.YMin; y <= splitPoint.YMax; y++)
            //        {
            //            for (int x = splitPoint.XMin; x <= splitPoint.XMax; x++)
            //            {
            //                _bitmap.SetPixel(x, world.Height - y - 1, Color.LightPink);
            //            }
            //        }
            //    }
            //}

            OnPropertyChanged(nameof(BitmapImage));
        }


        void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
