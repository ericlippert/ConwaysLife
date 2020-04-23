using System.Drawing;
using System.Drawing.Imaging;
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
            var quality = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
            jpgParameters.Param[0] = quality;
        }

        public static void SaveImage(Image image)
        {
            var name = System.IO.Path.GetRandomFileName().Replace(".", "");
            var path = @"C:\Users\ericl\Documents\" + name + ".jpg";
            image.Save(path, jpgCodec, jpgParameters);
        }
    }
}
