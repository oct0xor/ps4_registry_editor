using System;
using System.Linq;
using Ps4EditLib.Exceptions;
using Ps4EditLib.Extensions;

namespace Ps4EditLib.Reader
{
    public class ObfuscatedContainerReader : ContainerReaderBase
    {
        public ObfuscatedContainerReader(byte[] data, bool backup) : base()
        {
            base.ObfuscatedContainer = true;
            if (!Initiate(data, backup))
            {
                throw new InvalidArgumentException("Invalid arguments");
            }
        }

        private bool Initiate(byte[] data, bool backup)
        {
            Crypto.XorData(data, 0, 0x20);

            // var version = BitConverter.ToUInt32(data, 0);            //unused
            var entriesCount = BitConverter.ToUInt16(data, 4);
            //  var entriesCount2 = BitConverter.ToUInt16(data, 6);     //unused
            //  var binarySize = BitConverter.ToUInt32(data, 8);        //unused

            var hdrHash = BitConverter.ToUInt32(data, 0xC);

            var hdr = data.Take(0x20).ToArray();

            hdr.Store32(0xC, 0);

            var hdrHash2 = Crypto.CalcHash(hdr, hdr.Length, 4).Swap32();

            Crypto.XorData(data, 0, 0x20);

            if (hdrHash != hdrHash2)
            {
                throw new InvalidChecksumException("Header");
            }

            for (var i = 0; i < entriesCount; i++)
            {
                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);

                var regIdEnc1 = BitConverter.ToUInt32(data, 0x20 + i * 0x10);
                var type = (EntryType)BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 4);
                var size = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 6);
                var regIdEnc2 = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 8);
                var entryHash = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 0xA);
                var value = BitConverter.ToUInt32(data, 0x20 + i * 0x10 + 0xC);

                var entry = data.Skip(0x20 + i * 0x10).Take(0x10).ToArray().Store16(0xA, 0);

                var entryHash2 = Crypto.CalcHash(entry, entry.Length, 2).Swap16();

                if (entryHash != entryHash2)
                {
                    throw new InvalidChecksumException("Entry");
                }

                uint regId;

                if (backup)
                {
                    Crypto.DecryptRegId(Crypto.RegMgrBackupRegIdKey, regIdEnc1, regIdEnc2, out regId);
                }
                else
                {
                    Crypto.DecryptRegId(Crypto.RegMgrEapRegIdKey, regIdEnc1, regIdEnc2, out regId);
                }

                var category = GetCategory(regId);

                if (type == EntryType.Integer)
                {
                    var bin = data.Skip((int)(0x20 + i * 0x10 + 0xC)).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, 0x20 + i * 0x10 + 0xC, value, category, bin));
                }
                else if (type == EntryType.String || type == EntryType.Binary)
                {
                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);

                    var binHash = BitConverter.ToUInt32(data, (int)(0x20 + entriesCount * 0x10 + value));

                    var bin = data.Skip((int)(0x20 + entriesCount * 0x10 + value + 4)).Take(size).ToArray();

                    var binHash2 = Crypto.CalcHash(bin, bin.Length, 4).Swap32();

                    if (binHash != binHash2)
                    {
                        throw new InvalidChecksumException("Data");
                    }

                    Entries.Add(new Entry(i, regId, type, size, (int)(0x20 + entriesCount * 0x10 + value + 4), value, category, bin));

                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);
                }

                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);
            }

            return true;
        }
    }
}