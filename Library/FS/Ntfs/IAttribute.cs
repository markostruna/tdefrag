using System;

namespace TDefragLib.FileSystem.Ntfs
{
    public interface IAttribute
    {
        AttributeType AttributeType { get; }

        UInt32 Length { get; }

        Byte NameLength { get; }

        UInt16 NameOffset {get;}
    }
}
