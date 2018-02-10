using System;
using System.Linq;
using Ps4EditLib.Exceptions;

namespace Ps4EditLib.Reader
{
    public class DataContainerReader : ContainerReaderBase
    {
        public DataContainerReader(byte[] data, byte[] idx) : base()
        {
            if (!Initiate(data, idx))
            {
                throw new InvalidArgumentException("Invalid arguments");
            }
        }

        private bool Initiate(byte[] data, byte[] idx)
        {
            var version = BitConverter.ToUInt32(idx, 0);

            if (version > 0x999999)
            {
                throw new NotSupportedException("Encrypted system.idx is not supported yet");
            }

            var magic = data[4];

            if (magic != 0x2A)
            {
                throw new NotSupportedException("Encrypted system.dat is not supported yet");
            }

            var entriesCount = BitConverter.ToUInt16(idx, 4);
            var updateCount = BitConverter.ToUInt16(idx, 6);

            for (var i = 0; i < entriesCount; i++)
            {
                var regId = BitConverter.ToUInt32(idx, 0x24 + i * 0x10);
                var size = BitConverter.ToUInt16(idx, 0x24 + i * 0x10 + 4);
                var type = (EntryType)idx[0x24 + i * 0x10 + 6];
                var flag = idx[0x24 + i * 0x10 + 7];
                var offset = BitConverter.ToInt32(idx, 0x24 + i * 0x10 + 8);

                var regId2 = BitConverter.ToUInt32(data, offset + 0x10);
                var size2 = BitConverter.ToUInt16(data, offset + 0x10 + 4);

                if (flag != 0)
                {
                    throw new NotSupportedException("regdatahd2.db is not supported yet");
                }

                if (regId != regId2)
                {
                    throw new InEqualityException("RegId's are not equal!");
                }

                if (size != size2)
                {
                    throw new InEqualityException("RegId size's are not equal!");
                }

                var category = GetCategory(regId);

                if (type == EntryType.Integer)
                {
                    var value = BitConverter.ToUInt32(data, offset + 0x10 + 8);
                    var bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, value, category, bin));
                }
                else if (type == EntryType.String || type == EntryType.Binary)
                {
                    var bin = data.Skip(offset + 0x10 + 8).Take(size).ToArray();
                    Entries.Add(new Entry(i, regId, type, size, offset + 0x10 + 8, (uint)offset, category, bin));
                }
            }

            return true;
        }
    }
}