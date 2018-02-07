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
    }
}