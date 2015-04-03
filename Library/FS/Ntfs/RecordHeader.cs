using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FS.Ntfs
{
    public class RecordHeader
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static RecordHeader Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            RecordHeader r = new RecordHeader();
            
            r.Type = reader.ReadUInt32();
            r.UpdateSequenceArrayOffset = reader.ReadUInt16();
            r.UpdateSequenceArrayCount = reader.ReadUInt16();
            r.LogFileSequenceNumber = reader.ReadUInt64();

            return r;
        }

        /// <summary>
        /// File type, for example 'FILE'
        /// </summary>
        public UInt32 Type { get; set; }

        /// <summary>
        /// Offset to the Update Sequence Array
        /// </summary>
        public UInt16 UpdateSequenceArrayOffset { get; set; }

        /// <summary>
        /// Size in words of Update Sequence Array
        /// </summary>
        public UInt16 UpdateSequenceArrayCount { get; set; }

        /// <summary>
        /// $LogFile Sequence Number (LSN)
        /// </summary>
        public UInt64 LogFileSequenceNumber { get; set; }

    }
}
