using System;
using System.Collections.Generic;
using System.IO;

namespace CAN_Tool.Libs
{
    // Converts a ".bmp" file into the raw pixel format the panel's bootloader stores in its
    // external SPI flash: 24-bit RGB (BMP's BGR byte order swapped to RGB), no metadata,
    // pixels reordered column-by-column starting from the rightmost column of the source image
    // (storage[row = width-1-x][col = y] = source(x, y)). This layout was determined by
    // reverse engineering a flash dump against the matching source .bmp files.
    //
    // Deliberately permissive, matching the original Delphi tool's SDIMAIN.PAS: it never checks
    // the 'BM' signature, bit depth, or compression - it just reads raw bytes at fixed offsets
    // (10 = pixel data offset, 0x12/0x13 = width LE16, 0x16/0x17 = height LE16) and treats
    // whatever follows as W*H*3 bytes of BGR pixel data. Some ".bmp" files in the image sets
    // (localization strings / glyph resources) are not real bitmaps at all - they are raw data
    // blobs smuggled through the image pipeline as a "picture" with height=1 (so width*3 is
    // just the byte count). Rejecting anything that isn't a strict, valid Windows bitmap would
    // silently drop those files, so this mirrors the original tool's tolerance instead of
    // re-validating structure it never actually checked.
    public static class PanelImageConverter
    {
        public static byte[] ConvertBmpToPanelFormat(string bmpPath, out int width, out int height)
        {
            var file = File.ReadAllBytes(bmpPath);
            if (file.Length < 24)
                throw new InvalidDataException("File is too small to contain a width/height header");

            var offset = file[10]; // matches the original tool: only the low byte of the offset field is used
            var w = file[0x12] | (file[0x13] << 8);
            var h = file[0x16] | (file[0x17] << 8);
            if (w <= 0 || h <= 0)
                throw new NotSupportedException($"Unsupported/zero dimensions {w}x{h}");

            width = w;
            height = h;

            var rowBytes = w * 3 + (w % 4); // == real BMP row padding to a 4-byte boundary
            var rows = new byte[rowBytes * h];
            var available = Math.Max(0, Math.Min(rows.Length, file.Length - offset));
            if (available > 0)
                Array.Copy(file, offset, rows, 0, available);
            // any bytes beyond EOF are left zeroed, same as the original tool's oversized
            // preallocated buffer would implicitly do

            var output = new byte[w * h * 3];
            for (var y = 0; y < h; y++)
            {
                var rowOffset = (h - 1 - y) * rowBytes; // BMP pixel rows are stored bottom-up
                for (var x = 0; x < w; x++)
                {
                    var b = rows[rowOffset + x * 3 + 0];
                    var g = rows[rowOffset + x * 3 + 1];
                    var r = rows[rowOffset + x * 3 + 2];

                    var destPixel = (w - 1 - x) * h + y;
                    output[destPixel * 3 + 0] = r;
                    output[destPixel * 3 + 1] = g;
                    output[destPixel * 3 + 2] = b;
                }
            }
            return output;
        }

        // Run-length compresses a byte stream as (value, count) pairs, count in [1, 128] -
        // matches the encoding the panel bootloader's CAN image path expects (recovered from
        // the original Delphi PC tool's sender and confirmed against a working flash dump).
        // Mainly a bandwidth win: icon-style images with large flat-color regions shrink a
        // lot, which matters a great deal at 250 kbit/s with one CRC8-checked CAN frame per
        // 7 payload bytes.
        public static byte[] RunLengthEncode(byte[] data)
        {
            var output = new List<byte>(data.Length / 4 + 2);
            if (data.Length == 0) return output.ToArray();

            var current = data[0];
            var count = 0;
            foreach (var v in data)
            {
                if (count == 0 || v == current)
                {
                    if (count == 0) current = v;
                    count++;
                }
                else
                {
                    output.Add(current);
                    output.Add((byte)count);
                    current = v;
                    count = 1;
                }

                if (count >= 128)
                {
                    output.Add(current);
                    output.Add((byte)count);
                    count = 0;
                }
            }
            if (count > 0)
            {
                output.Add(current);
                output.Add((byte)count);
            }
            return output.ToArray();
        }
    }
}
