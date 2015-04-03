using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragWpf.Library.FS.Ntfs;

namespace TDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("{AttributeType}: LEN={Length} ")]
    public class Attribute : IAttribute
    {
        public static Attribute Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            Attribute attribute = new Attribute();

            attribute.InternalParse(reader);

            return attribute;
        }

        protected void InternalParse(BinaryReader reader)
        {
            if (reader == null)
                return;

            AttributeType = AttributeType.Parse(reader);

            if (AttributeType.IsEndOfList)
                return;

            Length = reader.ReadUInt32();
            IsNonresident = reader.ReadBoolean();
            NameLength = reader.ReadByte();
            NameOffset = reader.ReadUInt16();
            Flags = (AttributeFlags)reader.ReadUInt16();
            Number = reader.ReadUInt16();
        }

        public AttributeType AttributeType { get; set; }

        public UInt32 Length { get; set; }

        public Boolean IsNonresident { get; set; }

        public Byte NameLength { get; set; }

        public UInt16 NameOffset { get; set; }

        public AttributeFlags Flags { get; set; }

        public UInt16 Number { get; set; }
    }
}
