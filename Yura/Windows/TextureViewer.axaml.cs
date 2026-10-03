using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System.IO;
using Yura.Formats;
using Yura.Shared.IO;
using Yura.Shared.Util;

namespace Yura
{
    public partial class TextureViewer : Window
    {
        public TextureViewer() { AvaloniaXamlLoader.Load(this); }

        public Endianness Endianness { get; set; }
        public Platform Platform { get; set; }

        public byte[] Texture
        {
            set
            {
                var reader = new DataReader(value, Endianness);
                reader.ReadInt32();                 // magic
                var start = reader.ReadInt32();     // texture data offset
                reader.BaseStream.Position += 12;   // skip unknown
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();

                reader.BaseStream.Position = start;

                int stride = width * 4;
                var textureData = new byte[height * stride];
                int read = 0;
                while (read < textureData.Length)
                {
                    int n = reader.BaseStream.Read(textureData, read, textureData.Length - read);
                    if (n <= 0) break;
                    read += n;
                }

                Bitmap image = Platform switch
                {
                    Platform.Wii => TextureDecoder.DecodeCMPR(width, height, textureData),
                    Platform.Ps3 => TextureDecoder.DecodePS3(width, height, textureData),
                    _ => CreateRawBgra(width, height, textureData)
                };

                this.FindControl<Image>("TextureImage")!.Source = image;
            }
        }

        private static WriteableBitmap CreateRawBgra(int width, int height, byte[] data)
        {
            var bmp = new WriteableBitmap(
                new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul);

            using var fb = bmp.Lock();
            int rowBytes = width * 4;
            for (int y = 0; y < height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    data, y * rowBytes,
                    fb.Address + y * fb.RowBytes, rowBytes);
            }
            return bmp;
        }
    }
}
