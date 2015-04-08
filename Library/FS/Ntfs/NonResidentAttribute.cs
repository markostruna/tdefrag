using System;
using System.IO;

namespace TDefragLib.FileSystem.Ntfs
{
    public class NonresidentAttribute : Attribute
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static new NonresidentAttribute Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

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

        /// <summary>
        /// StartingVirtualClusterNumber
        /// </summary>
        public UInt64 StartingVirtualClusterNumber { get; set; }

        /// <summary>
        /// LastVirtualClusterNumber
        /// </summary>
        public UInt64 LastVirtualClusterNumber { get; set; }

        /// <summary>
        /// RunArrayOffset
        /// </summary>
        public UInt16 RunArrayOffset { get; set; }

        /// <summary>
        /// CompressionUnit
        /// </summary>
        public Byte CompressionUnit { get; set; }

        /// <summary>
        /// [5]
        /// </summary>
        public Byte[] AlignmentOrReserved { get; set; }

        /// <summary>
        /// AllocatedSize
        /// </summary>
        public UInt64 AllocatedSize { get; set; }

        /// <summary>
        /// DataSize
        /// </summary>
        public UInt64 DataSize { get; set; }

        /// <summary>
        /// InitializedSize
        /// </summary>
        public UInt64 InitializedSize { get; set; }

        /// <summary>
        /// Only when compressed
        /// </summary>
        public UInt64 CompressedSize { get; set; }
    }
}
