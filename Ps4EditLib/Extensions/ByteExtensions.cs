using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ps4EditLib.Extensions
{
    public static class ByteExtensions
    {
        public static ushort Swap16(this ulong val) => Swap16((ushort)val);

        public static uint Swap32(this ulong val) => Swap32((uint)val);

        public static ushort Swap16(this ushort val)
        {
            return (ushort)((val << 8 & 0xFF00) |
                            (val >> 8 & 0x00FF));
        }

        public static uint Swap32(this uint val)
        {
            return ((val << 24 & 0xFF000000) |
                    (val << 8 & 0x00FF0000) |
                    (val >> 8 & 0x0000FF00) |
                    (val >> 24 & 0x000000FF));
        }

        public static byte[] Store16(this byte[] data, int offset, uint value)
        {
            data[offset + 0] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            return data;
        }

        public static void Store32(this byte[] data, int offset, uint value)
        {
            data[offset + 0] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 0x10);
            data[offset + 3] = (byte)(value >> 0x18);
        }

        public static string HexDump(this byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            var bytesLength = bytes.Length;

            var hexChars = "0123456789ABCDEF".ToCharArray();

            const int firstHexColumn = 8 + 3; // 8 characters for the address,3 spaces

            var firstCharColumn = firstHexColumn
                + bytesPerLine * 3              // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8        // - 1 extra space every 8 characters from the 9th
                + 2;                            // 2 spaces

            var lineLength = firstCharColumn
                + bytesPerLine                  // - characters to show the ascii value
                + Environment.NewLine.Length;   // Carriage return and line feed (should normally be 2)

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            var result = new StringBuilder(expectedLines * lineLength);

            for (var i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = hexChars[(i >> 28) & 0xF];
                line[1] = hexChars[(i >> 24) & 0xF];
                line[2] = hexChars[(i >> 20) & 0xF];
                line[3] = hexChars[(i >> 16) & 0xF];
                line[4] = hexChars[(i >> 12) & 0xF];
                line[5] = hexChars[(i >> 8) & 0xF];
                line[6] = hexChars[(i >> 4) & 0xF];
                line[7] = hexChars[(i >> 0) & 0xF];

                var hexColumn = firstHexColumn;
                var charColumn = firstCharColumn;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        var b = bytes[i + j];
                        line[hexColumn] = hexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = hexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            return result.ToString();
        }
    }
}