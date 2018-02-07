namespace Ps4EditLib.PsRegistry
{
    public class RegInfo
    {
        public uint RegId;
        public int Type;
        public int Size;
        public string Path;

        public RegInfo(uint regId, int type, int size, string path)
        {
            this.RegId = regId;
            this.Type = type;
            this.Size = size;
            this.Path = path;
        }
    }
}