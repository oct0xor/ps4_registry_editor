using System;

namespace Ps4EditLib.PsRegistry
{
    public class RegFile
    {
        public string Storage;
        public string File;
        public int Size;

        public RegFile(string storage, string file, int size)
        {
            this.Storage = storage;
            this.File = file;
            this.Size = size;
        }

        public static RegFile Open(byte[] data)
        {
            var file = PsRegistry.RegFiles.Find(x => x.Size == data.Length);

            if (file == null && BitConverter.ToUInt32(data, 4) == 0x2A2A2A2A)
            {
                file = PsRegistry.RegFiles.Find(x => x.Storage == "regdatahdd.db");
            }

            return file;
        }
    }
}