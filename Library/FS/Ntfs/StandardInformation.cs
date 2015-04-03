using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("{CreationTime}: Usn={Usn} ")]
    public class StandardInformation
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static StandardInformation Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            StandardInformation s = new StandardInformation();
            
            s.CreationTime = reader.ReadUInt64();
            s.FileChangeTime = reader.ReadUInt64();
            s.MftChangeTime = reader.ReadUInt64();
            s.LastAccessTime = reader.ReadUInt64();
            s.FileAttributes = reader.ReadUInt32();
            s.MaximumVersions = reader.ReadUInt32();
            s.VersionNumber = reader.ReadUInt32();
            s.ClassId = reader.ReadUInt32();
            s.OwnerId = reader.ReadUInt32();
            s.SecurityId = reader.ReadUInt32();
            s.QuotaCharge = reader.ReadUInt64();
            s.Usn = reader.ReadUInt64();
            
            return s;
        }

        /// <summary>
        /// CreationTime
        /// </summary>
        public UInt64 CreationTime { get; set; }

        /// <summary>
        /// FileChangeTime
        /// </summary>
        public UInt64 FileChangeTime { get; set; }

        /// <summary>
        /// MftChangeTime
        /// </summary>
        public UInt64 MftChangeTime { get; set; }

        /// <summary>
        /// LastAccessTime
        /// </summary>
        public UInt64 LastAccessTime { get; set; }

        /// <summary>
        /// READ_ONLY = 0x01, HIDDEN = 0x02, SYSTEM = 0x04, VOLUME_ID = 0x08, ARCHIVE = 0x20, DEVICE = 0x40
        /// </summary>
        public UInt32 FileAttributes { get; set; }

        /// <summary>
        /// MaximumVersions
        /// </summary>
        public UInt32 MaximumVersions { get; set; }

        /// <summary>
        /// VersionNumber
        /// </summary>
        public UInt32 VersionNumber { get; set; }

        /// <summary>
        /// ClassId
        /// </summary>
        public UInt32 ClassId { get; set; }

        /// <summary>
        /// NTFS 3.0 only
        /// </summary>
        public UInt32 OwnerId { get; set; }

        /// <summary>
        /// NTFS 3.0 only
        /// </summary>
        public UInt32 SecurityId { get; set; }

        /// <summary>
        /// NTFS 3.0 only
        /// </summary>
        public UInt64 QuotaCharge { get; set; }

        /// <summary>
        /// NTFS 3.0 only
        /// </summary>
        public UInt64 Usn { get; set; }
    }
}
