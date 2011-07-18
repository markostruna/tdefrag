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
        private RecordHeader()
        {
        }

        public static RecordHeader Parse(BinaryReader reader)
        {
            if (reader == null)
            {
                return null;
            }

            RecordHeader r = new RecordHeader();
            
            r.Type = reader.ReadUInt32();
            r.UpdateSequenceArrayOffset = reader.ReadUInt16();
            r.UpdateSequenceArrayCount = reader.ReadUInt16();
            r.LogFileSequenceNumber = reader.ReadUInt64();
            return r;
        }

        public UInt32 Type;                        /* File type, for example 'FILE' */

        public UInt16 UpdateSequenceArrayOffset;   /* Offset to the Update Sequence Array */
        public UInt16 UpdateSequenceArrayCount;    /* Size in words of Update Sequence Array */

        public UInt64 LogFileSequenceNumber;       /* $LogFile Sequence Number (LSN) */

    }
}
