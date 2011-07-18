using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    public class NonresidentAttribute : Attribute
    {
        private NonresidentAttribute()
        {
        }

        public static new NonresidentAttribute Parse(BinaryReader reader)
        {
            if (reader == null)
            {
                return null;
            }

            NonresidentAttribute a = new NonresidentAttribute();

            a.InternalParse(reader);
            a.StartingVirtualClusterNumber = reader.ReadUInt64();
            a.LastVirtualClusterNumber = reader.ReadUInt64();
            a.RunArrayOffset = reader.ReadUInt16();
            a.CompressionUnit = reader.ReadByte();
            a.AlignmentOrReserved = reader.ReadBytes(5);
            a.AllocatedSize = reader.ReadUInt64();
            a.DataSize = reader.ReadUInt64();
            a.InitializedSize = reader.ReadUInt64();
            a.CompressedSize = reader.ReadUInt64();
            return a;
        }

        public UInt64 StartingVirtualClusterNumber
        { get; private set; }

        public UInt64 LastVirtualClusterNumber
        { get; private set; }

        public UInt16 RunArrayOffset
        { get; private set; }

        public Byte CompressionUnit
        { get; private set; }

        public Byte[] AlignmentOrReserved/*[5]*/
        { get; private set; }

        public UInt64 AllocatedSize
        { get; private set; }

        public UInt64 DataSize
        { get; private set; }

        public UInt64 InitializedSize
        { get; private set; }

        // Only when compressed
        public UInt64 CompressedSize
        { get; private set; }
    }
}
