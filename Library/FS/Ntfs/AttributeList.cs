using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using TDefragWpf.Library.FS.Ntfs;

namespace TDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Length = {Length}")]
    class AttributeList : IAttribute
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AttributeList Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            AttributeList list = new AttributeList();

            list.AttributeType = AttributeType.Parse(reader);

            if (list.AttributeType.IsEndOfList)
                return list;

            list.Length = reader.ReadUInt16();
            list.NameLength = reader.ReadByte();
            list.NameOffset = reader.ReadByte();
            list.LowestVirtualClusterNumber = reader.ReadUInt64();
            
            list.FileReferenceNumber = InodeReference.Parse(reader);
            
            list.Instance = reader.ReadUInt16();
            list.AlignmentOrReserved = new UInt16[3];
            list.AlignmentOrReserved[0] = reader.ReadUInt16();
            list.AlignmentOrReserved[1] = reader.ReadUInt16();
            list.AlignmentOrReserved[2] = reader.ReadUInt16();
            
            return list;
        }

        /// <summary>
        /// Type
        /// </summary>
        public AttributeType AttributeType { get; set; }

        /// <summary>
        /// Only the lower word is used (Uint16)
        /// </summary>
        public UInt32 Length { get; set; }

        /// <summary>
        /// NameLength
        /// </summary>
        public Byte NameLength { get; set; }

        /// <summary>
        /// Only the lower byte is used (Byte)
        /// </summary>
        public UInt16 NameOffset { get; set; }

        /// <summary>
        /// LowestVirtualClusterNumber
        /// </summary>
        public UInt64 LowestVirtualClusterNumber { get; set; }

        /// <summary>
        /// FileReferenceNumber
        /// </summary>
        public InodeReference FileReferenceNumber { get; set; }

        /// <summary>
        /// Instance
        /// </summary>
        public UInt16 Instance { get; set; }

        /// <summary>
        /// AlignmentOrReserved
        /// </summary>
        public UInt16[] AlignmentOrReserved { get; set; }
    }
}
