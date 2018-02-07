using System.Collections.Generic;

namespace Ps4EditLib.Reader
{
    public abstract class ContainerReaderBase : IEntityReader
    {
        public List<Entry> Entries { get; protected set; }
        public bool ObfuscatedContainer { get; protected set; }

        protected ContainerReaderBase()
        {
            Entries = new List<Entry>();
        }

        protected string GetCategory(uint regId)
        {
            var info = PsRegistry.Preferences.RegTable.Find(x => x.RegId == regId);
            var category = info == null ? $"Unknown 0x{regId:X8}" : info.Path;

            return category;
        }
    }
}