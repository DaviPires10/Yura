using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;
using Yura.Shared.IO;

namespace Yura.Formats
{
    public static class TextureDecoder
    {
        // Wii CMPR (S3TC-ish, RGB565 blocks, 8x8 tile layout)
        public static WriteableBitmap DecodeCMPR(int width, int height, byte[] buffer)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);

            var reader = new DataReader(buffer, Endianness.LittleEndian);
            var pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y += 8)
            for (int x = 0; x < width; x += 8)
            for (int y2 = 0; y2 < 8; y2 += 4)
            for (int x2 = 0; x2 < 8; x2 += 4)
            {
                var c0 = reader.ReadUInt16();
                var c1 = reader.ReadUInt16();
                var bits = reader.ReadUInt32();

                ConvertRgb565(c0, out var r0, out var g0, out var b0);
                ConvertRgb565(c1, out var r1, out var g1, out var b1);

                for (int y3 = 3; y3 >= 0; y3--)
                for (int x3 = 3; x3 >= 0; x3--)
                {
                    int newx = x + x2 + x3;
                    int newy = y + y2 + y3;

                    uint control = bits & 3;
                    bits >>= 2;

                    byte r = 0, g = 0, b = 0;

                    switch (control)
                    {
                        case 0: r = r0; g = g0; b = b0; break;
                        case 1: r = r1; g = g1; b = b1; break;
                        case 2:
                            if (c0 > c1) { r = (byte)((2 * r0 + r1) / 3); g = (byte)((2 * g0 + g1) / 3); b = (byte)((2 * b0 + b1) / 3); }
                            else         { r = (byte)((r0 + r1) / 2);     g = (byte)((g0 + g1) / 2);     b = (byte)((b0 + b1) / 2); }
                            break;
                        case 3:
                            if (c0 > c1) { r = (byte)((r0 + 2 * r1) / 3); g = (byte)((g0 + 2 * g1) / 3); b = (byte)((b0 + 2 * b1) / 3); }
                            else         { r = 0; g = 0; b = 0; }
                            break;
                    }

                    if (newx < width && newy < height)
                    {
                        int off = ((newy * width) + newx) * 4;
                        pixels[off + 0] = b;
                        pixels[off + 1] = g;
                        pixels[off + 2] = r;
                        pixels[off + 3] = 255;
                    }
                }
            }

            CopyToBitmap(bitmap, pixels);
            return bitmap;
        }

        // PS3 swizzled (Morton order) BGRA
        public static WriteableBitmap DecodePS3(int width, int height, byte[] buffer)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            var pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var idx = Morton2D(x, y);
                var src = (int)(idx * 4);
                var dst = (y * width + x) * 4;

                // source is BGRA already
                pixels[dst + 0] = buffer[src + 0];
                pixels[dst + 1] = buffer[src + 1];
                pixels[dst + 2] = buffer[src + 2];
                pixels[dst + 3] = buffer[src + 3];
            }

            CopyToBitmap(bitmap, pixels);
            return bitmap;
        }

        private static void CopyToBitmap(WriteableBitmap bitmap, byte[] pixels)
        {
            using var fb = bitmap.Lock();
            // WriteableBitmap stride may not equal width*4, so copy row by row.
            int rowBytes = bitmap.PixelSize.Width * 4;
            for (int y = 0; y < bitmap.PixelSize.Height; y++)
            {
                Marshal.Copy(pixels, y * rowBytes, fb.Address + y * fb.RowBytes, rowBytes);
            }
        }

        private static void ConvertRgb565(ushort color, out byte r, out byte g, out byte b)
        {
            r = (byte)(((color >> 11) & 0x1f) << 3);
            g = (byte)(((color >> 5) & 0x3f) << 2);
            b = (byte)((color & 0x1f) << 3);
        }

        private static long Morton2D(int x, int y)
        {
            x = (x | (x << 16)) & 0x0000FFFF;
            x = (x | (x << 8)) & 0x00FF00FF;
            x = (x | (x << 4)) & 0x0F0F0F0F;
            x = (x | (x << 2)) & 0x33333333;
            x = (x | (x << 1)) & 0x55555555;

            y = (y | (y << 16)) & 0x0000FFFF;
            y = (y | (y << 8)) & 0x00FF00FF;
            y = (y | (y << 4)) & 0x0F0F0F0F;
            y = (y | (y << 2)) & 0x33333333;
            y = (y | (y << 1)) & 0x55555555;

            return x | (y << 1);
        }
    }
}
