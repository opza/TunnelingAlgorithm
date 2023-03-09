

using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using TunnelingAlgorithm;

namespace WPFPrinter
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        Bitmap _bitmap;
        World _world;

        public ActionCommand GenerateCommand { get; set; }
        public BitmapImage BitmapImage => _bitmap.ConvertToBitmapImage();

        public MainViewModel()
        {          
            _world = new World(150, 150);
            _bitmap = new Bitmap(_world.Width, _world.Height);

            GenerateCommand = new ActionCommand(GenerateWorld);
        }

        void GenerateWorld()
        {
            var tunnelers = _world.Generate();

            for (int y = 0; y < _world.Height; y++)
            {
                for (int x = 0; x < _world.Width; x++)
                {
                    var tile = _world.GetTile(x, _world.Height - y - 1);
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
