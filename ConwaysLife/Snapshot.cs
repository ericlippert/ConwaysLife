using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ConwaysLife
{
    static class Snapshot
    {
        static ImageCodecInfo jpgCodec = null;
        static EncoderParameters jpgParameters = null;

        static Snapshot()
        {
            jpgCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.MimeType == "image/jpeg");
            if (jpgCodec == null)
                return;
            jpgParameters = new EncoderParameters(1);
            var quality = new EncoderParameter(Encoder.Quality, 75L);
            jpgParameters.Param[0] = quality;
        }

        public static void SaveImage(Image image)
        {
            var name = Path.GetRandomFileName().Replace(".", "") + ".jpg";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, name);
            image.Save(path, jpgCodec, jpgParameters);
        }
    }
}
