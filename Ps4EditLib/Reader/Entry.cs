namespace Ps4EditLib.Reader
{
    public class Entry
    {
        public int I;
        public uint RegId;
        public EntryType Type;
        public ushort Size;
        public int Offset;
        public uint Value;
        public byte[] Data;
        public string Category;

        public Entry(int i, uint regId, EntryType type, ushort size, int offset, uint value, string category)
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

        public Entry(int i, uint regId, EntryType type, ushort size, int offset, uint value, string category, byte[] data)
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
}