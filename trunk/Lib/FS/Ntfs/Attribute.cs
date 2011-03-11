using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    [Flags]
    public enum AttributeFlags
    {
        Compressed = 0x0001,
        Encrypted = 0x4000,
        Sparse = 0x8000
    }

    [DebuggerDisplay("{Type}: LEN={Length} ")]
    public class Attribute : IAttribute
    {
        protected Attribute()
        {
        }

        public AttributeType Type
        { get; private set; }

        public UInt32 Length
        { get; private set; }

        public Boolean IsNonresident
        { get; private set; }

        public Byte NameLength
        { get; private set; }

        public UInt16 NameOffset
        { get; private set; }

        public AttributeFlags Flags
        { get; private set; }

        public UInt16 Number
        { get; private set; }

        protected void InternalParse(BinaryReader reader)
        {
            if (reader == null)
            {
                return;
            }

            Type = AttributeType.Parse(reader);
            if (Type.Type != AttributeEnumType.EndOfList)
            {
                Length = reader.ReadUInt32();
                IsNonresident = reader.ReadBoolean();
                NameLength = reader.ReadByte();
                NameOffset = reader.ReadUInt16();
                Flags = (AttributeFlags)reader.ReadUInt16();
                Number = reader.ReadUInt16();
            }
        }

        public static Attribute Parse(BinaryReader reader)
        {
            Attribute attribute = new Attribute();
            attribute.InternalParse(reader);
            return attribute;
        }

    }
}
