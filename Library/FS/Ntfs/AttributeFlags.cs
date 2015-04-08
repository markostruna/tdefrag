using System;

namespace TDefragWpf.Library.FS.Ntfs
{
    [Flags]
    public enum AttributeFlags
    {
        Compressed = 0x0001,
        Encrypted = 0x4000,
        Sparse = 0x8000
    }
}
