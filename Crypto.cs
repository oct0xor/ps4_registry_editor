using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace PS4_REGISTRY_EDITOR
{
    static class Crypto
    {
        public static byte[] RegMgrBackupRegIdKey = {
            0x1F, 0x26, 0xFD, 0x8D, 0xBF, 0x0A, 0x8D, 0x92, 0x7F, 0x6B, 0xA0, 0x12, 0xB4, 0x0E, 0x8F, 0xB1
        };

        public static byte[] RegMgrEapRegIdKey = {
            0xAC, 0x1D, 0x20, 0xEE, 0x53, 0xE3, 0x24, 0x1D, 0x27, 0x6C, 0x2D, 0xC4, 0x7D, 0xA1, 0xF0, 0xA5
        };

        public static void EncryptRegId(byte[] key, uint regId, out uint enc1, out ushort enc2)
        {
            uint m = BitConverter.ToUInt32(BigInteger.Multiply(0x4EC4EC4EC4EC4EC5, regId).ToByteArray(), 8);

            uint e0 = key[(regId - 0xD * (m >> 2) + 3)] ^ regId & 0xFF;
            uint e1 = (uint)(key[(regId - 0xD * (m >> 2) + 2)] << 8) ^ regId & 0xFF00;
            uint e2 = (uint)(key[(regId - 0xD * (m >> 2) + 1)] << 0x10) ^ regId & 0xFF0000;
            uint e3 = (uint)(key[(regId - 0xD * (m >> 2))] << 0x18) ^ regId & 0xFF000000;

            enc1 = e0 | e1 | e2 | e3;

            enc2 = (ushort)(regId - 0xD * (m >> 2));
        }

        public static void DecryptRegId(byte[] key, uint enc1, ushort enc2, out uint regId)
        {
            uint d0 = key[enc2 + 3] ^ enc1 & 0xFF;
            uint d1 = (uint)(key[enc2 + 2] << 8) ^ enc1 & 0xFF00;
            uint d2 = (uint)(key[enc2 + 1] << 0x10) ^ enc1 & 0xFF0000;
            uint d3 = (uint)(key[enc2] << 0x18) ^ enc1 & 0xFF000000;

            regId = d0 | d1 | d2 | d3;
        }

        public static ulong CalcHash(byte[] data, int size, int hashsize)
        {
            ulong seed = 0x89BB1CE061850272;

            ulong iv = seed;

            int cnt = 0;

            while (cnt < size)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (cnt == size)
                        break;

                    iv += (((seed >> 8 * i) & 0xFF) ^ data[cnt]) << 8 * i;

                    if (data[cnt] != 0)
                        iv *= data[cnt];
                    else
                        iv *= 3;

                    cnt += 1;
                }
            }

            return iv >> (0x40 - (8 * hashsize));
        }

        public static void XorData(byte[] data, int offset, int size)
        {
            ulong key = 0xB9942494ACB75823;

            int cnt = 0;

            while (cnt < size)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (cnt == size)
                        break;

                    data[offset + cnt] ^= (byte)(key >> 8 * i);

                    cnt += 1;
                }
            }
        }
    }
}
