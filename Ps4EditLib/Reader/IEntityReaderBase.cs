using System.Collections.Generic;

namespace Ps4EditLib.Reader
{
    public interface IEntityReader
    {
        List<Entry> Entries { get; }
        bool ObfuscatedContainer { get; }
    }
}