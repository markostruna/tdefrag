using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.FS.Ntfs;

namespace TDefragLib.FileSystem.Ntfs
{
    public class FileRecordHeader
    {
        public static FileRecordHeader Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            FileRecordHeader r = new FileRecordHeader();

            r.RecordHeader = RecordHeader.Parse(reader);

            r.SequenceNumber = reader.ReadUInt16();
            r.LinkCount = reader.ReadUInt16();
            r.AttributeOffset = reader.ReadUInt16();
            r.Flags = reader.ReadUInt16();
            r.BytesInUse = reader.ReadUInt32();
            r.BytesAllocated = reader.ReadUInt32();

            r.BaseFileRecord = InodeReference.Parse(reader);
            
            r.NextAttributeNumber = reader.ReadUInt16();
            r.Padding = reader.ReadUInt16();
            r.MasterFileTableRecordNumber = reader.ReadUInt32();
            r.UpdateSequenceNumber = reader.ReadUInt16();
            
            return r;
        }

        public RecordHeader RecordHeader { get; set; }

        public UInt16 SequenceNumber { get; set; }

        public UInt16 LinkCount { get; set; }

        public UInt16 AttributeOffset { get; set; }

        /// <summary>
        /// Flags. bit 1 = in use, bit 2 = directory, bit 4 & 8 = unknown.
        /// </summary>
        public UInt16 Flags { get; set; }

        public Boolean IsInUse { get { return ((Flags & 1) == 1); } }

        public Boolean IsDirectory { get { return ((Flags & 2) == 2); } }

        public Boolean IsUnknown { get { return ((Flags & 252) != 0); } }

        /// <summary>
        /// Real size of the FILE record 
        /// </summary>
        public UInt32 BytesInUse { get; set; }

        /// <summary>
        /// Allocated size of the FILE record
        /// </summary>
        public UInt32 BytesAllocated { get; set; }

        /// <summary>
        /// File reference to the base FILE record
        /// </summary>
        public InodeReference BaseFileRecord { get; set; }

        /// <summary>
        /// Next Attribute Id
        /// </summary>
        public UInt16 NextAttributeNumber { get; set; }

        /// <summary>
        /// Align to 4 UCHAR boundary (XP)
        /// </summary>
        public UInt16 Padding { get; set; }

        /// <summary>
        /// Number of this MFT Record (XP)
        /// </summary>
        public UInt32 MasterFileTableRecordNumber { get; set; }

        public UInt16 UpdateSequenceNumber { get; set; }

    }
}
