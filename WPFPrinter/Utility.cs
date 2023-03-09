
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace WPFPrinter
{
    public static class Utility
    {
        public static BitmapImage ConvertToBitmapImage(this Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            var bitmapImg = new BitmapImage();
            bitmapImg.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bitmapImg.StreamSource = ms;
            bitmapImg.EndInit();

            return bitmapImg;
        }
    }
}
