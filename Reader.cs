using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS4_REGISTRY_EDITOR
{
    class Entry
    {
        public int i;
        public uint regId;
        public ushort type;
        public ushort size;
        public int offset;
        public uint value;
        public byte[] data;
        public string category;

        public Entry(int i, uint regId, ushort type, ushort size, int offset, uint value, string category)
        {
            this.i = i;
            this.regId = regId;
            this.type = type;
            this.size = size;
            this.offset = offset;
            this.value = value;
            this.data = null;
            this.category = category;
        }

        public Entry(int i, uint regId, ushort type, ushort size, int offset, uint value, string category, byte[] data)
        {
            this.i = i;
            this.regId = regId;
            this.type = type;
            this.size = size;
            this.offset = offset;
            this.value = value;
            this.data = data;
            this.category = category;
        }
    }

    class Reader
    {
        public bool obfuscatedContainer = false;
        public List<Entry> entries = new List<Entry>();

        private string GetCategory(uint regId)
        {
            RegInfo info = Registry.regTable.Find(x => x.regId == regId);

            string category;
            if (info == null)
            {
                category = String.Format("Unknown 0x{0:X8}", regId);
            }
            else
            {
                category = info.path;
            }

            return category;
        }

        public bool ObfuscatedContainerReader(byte[] data, bool backup)
        {
            obfuscatedContainer = true;

            Crypto.XorData(data, 0, 0x20);

            uint version = BitConverter.ToUInt32(data, 0);
            ushort entriesCount = BitConverter.ToUInt16(data, 4);
            ushort entriesCount2 = BitConverter.ToUInt16(data, 6);
            uint binarySize = BitConverter.ToUInt32(data, 8);

            uint hdrHash = BitConverter.ToUInt32(data, 0xC);

            byte[] hdr = data.Take(0x20).ToArray();

            Utils.Store32(hdr, 0xC, 0);

            uint hdrHash2 = Utils.Swap32((uint)Crypto.CalcHash(hdr, hdr.Length, 4));

            Crypto.XorData(data, 0, 0x20);

            if (hdrHash != hdrHash2)
            {
                MessageBox.Show("Header hash is wrong!");
                return false;
            }

            for (int i = 0; i < entriesCount; i++)
            {
                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);

                uint regIdEnc1 = BitConverter.ToUInt32(data, 0x20 + i * 0x10);
                ushort type = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 4);
                ushort size = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 6);
                ushort regIdEnc2 = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 8);
                ushort entryHash = BitConverter.ToUInt16(data, 0x20 + i * 0x10 + 0xA);
                uint value = BitConverter.ToUInt32(data, 0x20 + i * 0x10 + 0xC);

                byte[] entry = data.Skip(0x20 + i * 0x10).Take(0x10).ToArray();
                Utils.Store16(entry, 0xA, 0);
                ushort entryHash2 = Utils.Swap16((ushort)Crypto.CalcHash(entry, entry.Length, 2));

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

                string category = GetCategory(regId);

                if (type == Registry.INTEGER)
                {
                    byte[] bin = data.Skip((int)(0x20 + i * 0x10 + 0xC)).Take(size).ToArray();
                    entries.Add(new Entry(i, regId, type, size, 0x20 + i * 0x10 + 0xC, value, category, bin));
                }
                else if (type == Registry.STRING || type == Registry.BINARY)
                {
                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);

                    uint binHash = BitConverter.ToUInt32(data, (int)(0x20 + entriesCount * 0x10 + value));

                    byte[] bin = data.Skip((int)(0x20 + entriesCount * 0x10 + value + 4)).Take(size).ToArray();

                    uint binHash2 = Utils.Swap32((uint)Crypto.CalcHash(bin, bin.Length, 4));

                    if (binHash != binHash2)
                    {
                        MessageBox.Show("Data hash is wrong!");
                        return false;
                    }

                    entries.Add(new Entry(i, regId, type, size, (int)(0x20 + entriesCount * 0x10 + value + 4), value, category, bin));

                    Crypto.XorData(data, (int)(0x20 + entriesCount * 0x10 + value), size + 4);
                }

                Crypto.XorData(data, 0x20 + i * 0x10, 0x10);
            }

            return true;
        }

        public bool DataContainerReader(byte[] data, byte[] idx)
        {
            uint version = BitConverter.ToUInt32(idx, 0);

            if (version > 0x999999)
            {
                MessageBox.Show("Encrypted system.idx is not supported yet");
                return false;
            }

            byte magic = data[4];

            if (magic != 0x2A)
            {
                MessageBox.Show("Encrypted system.dat is not supported yet");
                return false;
            }

            ushort entriesCount = BitConverter.ToUInt16(idx, 4);
            ushort updateCount = BitConverter.ToUInt16(idx, 6);

            for (int i = 0; i < entriesCount; i++)
            {
                uint regId = BitConverter.ToUInt32(idx, 0x24 + i * 0x10);
                ushort size = BitConverter.ToUInt16(idx, 0x24 + i * 0x10 + 4);
                byte type = idx[0x24 + i * 0x10 + 6];
                byte flag = idx[0x24 + i * 0x10 + 7];
                int offset = BitConverter.ToInt32(idx, 0x24 + i * 0x10 + 8);

                uint regId2 = BitConverter.ToUInt32(data, offset + 0x10);
                ushort size2 = BitConverter.ToUInt16(data, offset + 0x10 + 4);

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

                string category = GetCategory(regId);

                if (type == Registry.INTEGER)
                {
                    uint value = BitConverter.ToUInt32(data, offset + 0x10 + 8);
                    byte[] bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, value, category, bin));
                }
                else if (type == Registry.STRING || type == Registry.BINARY)
                {
                    byte[] bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, (uint)offset, category, bin));
                }
            }

            return false;
        }
    }
}
