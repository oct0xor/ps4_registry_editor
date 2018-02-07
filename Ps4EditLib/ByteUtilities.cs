using System;
using System.Collections.Generic;
using System.Text;

namespace Ps4EditLib
{
    public static class ByteUtilities
    {
        public static string ByteArrayToString(IEnumerable<byte> ba)
        {
            var hex = new StringBuilder();
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);

            return hex.ToString();
        }

        public static byte[] StringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}