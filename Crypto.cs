using System;
using System.Numerics;

namespace PS4_REGISTRY_EDITOR
{
    internal static class Crypto
    {
        public static byte[] RegMgrBackupRegIdKey = {
            0x1F, 0x26, 0xFD, 0x8D, 0xBF, 0x0A, 0x8D, 0x92, 0x7F, 0x6B, 0xA0, 0x12, 0xB4, 0x0E, 0x8F, 0xB1
        };

        public static byte[] RegMgrEapRegIdKey = {
            0xAC, 0x1D, 0x20, 0xEE, 0x53, 0xE3, 0x24, 0x1D, 0x27, 0x6C, 0x2D, 0xC4, 0x7D, 0xA1, 0xF0, 0xA5
        };

        public static void EncryptRegId(byte[] key, uint regId, out uint enc1, out ushort enc2)
        {
            var m = BitConverter.ToUInt32(BigInteger.Multiply(0x4EC4EC4EC4EC4EC5, regId).ToByteArray(), 8);

            var eOffset = regId - 0xD * (m >> 2);

            var e0 = (uint)(key[eOffset + 3]) ^ regId & 0xFF;
            var e1 = (uint)(key[eOffset + 2] << 8) ^ regId & 0xFF00;
            var e2 = (uint)(key[eOffset + 1] << 0x10) ^ regId & 0xFF0000;
            var e3 = (uint)(key[eOffset] << 0x18) ^ regId & 0xFF000000;

            enc1 = e0 | e1 | e2 | e3;

            enc2 = (ushort)(regId - 0xD * (m >> 2));
        }

        public static void DecryptRegId(byte[] key, uint enc1, ushort enc2, out uint regId)
        {
            var d0 = key[enc2 + 3] ^ enc1 & 0xFF;
            var d1 = (uint)(key[enc2 + 2] << 8) ^ enc1 & 0xFF00;
            var d2 = (uint)(key[enc2 + 1] << 0x10) ^ enc1 & 0xFF0000;
            var d3 = (uint)(key[enc2] << 0x18) ^ enc1 & 0xFF000000;

            regId = d0 | d1 | d2 | d3;
        }

        public static ulong CalcHash(byte[] data, int size, int hashsize)
        {
            const ulong seed = 0x89BB1CE061850272;

            var iv = seed;

            var cnt = 0;

            while (cnt < size)
            {
                for (var i = 0; i < 8; i++)
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

            return iv >> (0x40 - 8 * hashsize);
        }

        public static void XorData(byte[] data, int offset, int size)
        {
            const ulong key = 0xB9942494ACB75823;

            var cnt = 0;

            while (cnt < size)
            {
                for (var i = 0; i < 8; i++)
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