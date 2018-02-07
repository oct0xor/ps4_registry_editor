using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PS4_REGISTRY_EDITOR
{
    internal class Entry
    {
        public int I;
        public uint RegId;
        public ushort Type;
        public ushort Size;
        public int Offset;
        public uint Value;
        public byte[] Data;
        public string Category;

        public Entry(int i, uint regId, ushort type, ushort size, int offset, uint value, string category)
        {
            this.I = i;
            this.RegId = regId;
            this.Type = type;
            this.Size = size;
            this.Offset = offset;
            this.Value = value;
            this.Data = null;
            this.Category = category;
        }

        public Entry(int i, uint regId, ushort type, ushort size, int offset, uint value, string category, byte[] data)
        {
            this.I = i;
            this.RegId = regId;
            this.Type = type;
            this.Size = size;
            this.Offset = offset;
            this.Value = value;
            this.Data = data;
            this.Category = category;
        }
    }

    internal class Reader
    {
        public readonly List<Entry> Entries = new List<Entry>();
        public bool ObfuscatedContainer { get; private set; }

        public Reader(byte[] data, byte[] idx)
        {
            if (!DataContainerReader(data, idx))
            {
                throw new Exception("Invalid arguments");
            }
        }

        public Reader(byte[] data, bool backup)
        {
            if (!ObfuscatedContainerReader(data, backup))
            {
                throw new Exception("Invalid arguments");
            }
        }

        private string GetCategory(uint regId)
        {
            var info = Registry.RegTable.Find(x => x.RegId == regId);
            var category = info == null ? $"Unknown 0x{regId:X8}" : info.Path;

            return category;
        }

        private bool ObfuscatedContainerReader(byte[] data, bool backup)
        {
            ObfuscatedContainer = true;

            Crypto.XorData(data, 0, 0x20);

            var version = BitConverter.ToUInt32(data, 0);
            var entriesCount = BitConverter.ToUInt16(data, 4);
            var entriesCount2 = BitConverter.ToUInt16(data, 6);
            var binarySize = BitConverter.ToUInt32(data, 8);

            var hdrHash = BitConverter.ToUInt32(data, 0xC);

            var hdr = data.Take(0x20).ToArray();

            Utils.Store32(hdr, 0xC, 0);

            var hdrHash2 = Utils.Swap32((uint)Crypto.CalcHash(hdr, hdr.Length, 4));

            Crypto.XorData(data, 0, 0x20);

            if (hdrHash != hdrHash2)
            {
                MessageBox.Show("Header hash is wrong!");
                return false;
            }

            for (var i = 0; i < entriesCount; i++)
            {
                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);

                var regIdEnc1 = BitConverter.ToUInt32(data, 0x20 + i * 0x10);
                var type = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 4);
                var size = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 6);
                var regIdEnc2 = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 8);
                var entryHash = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 0xA);
                var value = BitConverter.ToUInt32(data, 0x20 + i * 0x10 + 0xC);

                var entry = data.Skip(0x20 + i * 0x10).Take(0x10).ToArray();
                Utils.Store16(entry, 0xA, 0);
                var entryHash2 = Utils.Swap16((ushort)Crypto.CalcHash(entry, entry.Length, 2));

                if (entryHash != entryHash2)
                {
                    MessageBox.Show("Entry hash is wrong!");
                    return false;
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

                if (type == Registry.Integer)
                {
                    var bin = data.Skip((int)(0x20 + i * 0x10 + 0xC)).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, 0x20 + i * 0x10 + 0xC, value, category, bin));
                }
                else if (type == Registry.String || type == Registry.Binary)
                {
                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);

                    var binHash = BitConverter.ToUInt32(data, (int)(0x20 + entriesCount * 0x10 + value));

                    var bin = data.Skip((int)(0x20 + entriesCount * 0x10 + value + 4)).Take(size).ToArray();

                    var binHash2 = Utils.Swap32((uint)Crypto.CalcHash(bin, bin.Length, 4));

                    if (binHash != binHash2)
                    {
                        MessageBox.Show("Data hash is wrong!");
                        return false;
                    }

                    Entries.Add(new Entry(i, regId, type, size, (int)(0x20 + entriesCount * 0x10 + value + 4), value, category, bin));

                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);
                }

                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);
            }

            return true;
        }

        private bool DataContainerReader(byte[] data, byte[] idx)
        {
            var version = BitConverter.ToUInt32(idx, 0);

            if (version > 0x999999)
            {
                MessageBox.Show("Encrypted system.idx is not supported yet");
                return false;
            }

            var magic = data[4];

            if (magic != 0x2A)
            {
                MessageBox.Show("Encrypted system.dat is not supported yet");
                return false;
            }

            var entriesCount = BitConverter.ToUInt16(idx, 4);
            var updateCount = BitConverter.ToUInt16(idx, 6);

            for (var i = 0; i < entriesCount; i++)
            {
                var regId = BitConverter.ToUInt32(idx, 0x24 + i * 0x10);
                var size = BitConverter.ToUInt16(idx, 0x24 + i * 0x10 + 4);
                var type = idx[0x24 + i * 0x10 + 6];
                var flag = idx[0x24 + i * 0x10 + 7];
                var offset = BitConverter.ToInt32(idx, 0x24 + i * 0x10 + 8);

                var regId2 = BitConverter.ToUInt32(data, offset + 0x10);
                var size2 = BitConverter.ToUInt16(data, offset + 0x10 + 4);

                if (flag != 0)
                {
                    MessageBox.Show("regdatahd2.db is not supported yet");
                    return false;
                }

                if (regId != regId2)
                {
                    MessageBox.Show("RegId's are not equal!");
                    return false;
                }

                if (size != size2)
                {
                    MessageBox.Show("RegId size's are not equal!");
                    return false;
                }

                var category = GetCategory(regId);

                if (type == Registry.Integer)
                {
                    var value = BitConverter.ToUInt32(data, offset + 0x10 + 8);
                    var bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, value, category, bin));
                }
                else if (type == Registry.String || type == Registry.Binary)
                {
                    var bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, (uint)offset, category, bin));
                }
            }

            return false;
        }
    }
}