using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    [Flags]
    public enum NameTypes : byte
    {
        /// <summary>
        /// This is the largest namespace. It is case sensitive and allows all
        /// Unicode characters except for: '\0' and '/'.  Beware that in
        /// WinNT/2k/2003 by default files which eg have the same name except
        /// for their case will not be distinguished by the standard utilities
        /// and thus a "del filename" will delete both "filename" and "fileName"
        /// without warning.  However if for example Services For Unix (SFU) are
        /// installed and the case sensitive option was enabled at installation
        /// time, then you can create/access/delete such files.
        /// Note that even SFU places restrictions on the filenames beyond the
        /// '\0' and '/' and in particular the following set of characters is
        /// not allowed: '"', '/', '<', '>', '\'.  All other characters,
        /// including the ones no allowed in WIN32 namespace are allowed.
        /// Tested with SFU 3.5 (this is now free) running on Windows XP.
        /// </summary>
        Posix = 0x00,   // POSIX name
        /// <summary>
        /// The standard WinNT/2k NTFS long filenames. Case insensitive.  All
        /// Unicode chars except: '\0', '"', '*', '/', ':', '<', '>', '?', '\',
        /// and '|'.  Further, names cannot end with a '.' or a space.
        /// </summary>
        Ntfs = 0x01,    // long name
        /// <summary>
        /// The standard DOS filenames (8.3 format). Uppercase only.  All 8-bit
        /// characters greater space, except: '"', '*', '+', ',', '/', ':', ';',
        /// '<', '=', '>', '?', and '\'.
        /// </summary>
        Dos = 0x02,      // 8.3 name
        /// <summary>
        /// means that both the Win32 and the DOS filenames are identical and
        /// hence have been saved in this single filename record.
        /// </summary>
        Win32Dos = 0x03
    }

    [DebuggerDisplay("Name = {Name}")]
    public class FileNameAttribute
    {
        private FileNameAttribute()
        {
        }

        [Conditional("DEBUG")]
        public void AssertValid()
        {
            Debug.Assert((NameType == NameTypes.Posix) || (NameType == NameTypes.Ntfs) ||
                (NameType == NameTypes.Dos) || (NameType == NameTypes.Win32Dos));
        }

        public static FileNameAttribute Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            FileNameAttribute filenameAttribute = new FileNameAttribute();

            filenameAttribute.ParentDirectory = InodeReference.Parse(reader);
            
            filenameAttribute.CreationTime = reader.ReadUInt64();
            filenameAttribute.ChangeTime = reader.ReadUInt64();
            filenameAttribute.LastWriteTime = reader.ReadUInt64();
            filenameAttribute.LastAccessTime = reader.ReadUInt64();
            filenameAttribute.AllocatedSize = reader.ReadUInt64();
            filenameAttribute.DataSize = reader.ReadUInt64();
            filenameAttribute.FileAttributes = reader.ReadUInt32();
            filenameAttribute.AlignmentOrReserved = reader.ReadUInt32();
            int nameLength = reader.ReadByte();
            filenameAttribute.NameType = (NameTypes)reader.ReadByte();
            filenameAttribute.Name = TDefragLib.FS.Ntfs.Helper.ParseString(reader, nameLength);
            
            filenameAttribute.AssertValid();
            
            return filenameAttribute;
        }

        /// <summary>
        /// NTFS or DOS name
        /// </summary>
        public NameTypes NameType { get; set; }

        public InodeReference ParentDirectory { get; set; }

        public UInt64 CreationTime { get; set; }

        public UInt64 ChangeTime { get; set; }

        public UInt64 LastWriteTime { get; set; }

        public UInt64 LastAccessTime { get; set; }

        public UInt64 AllocatedSize { get; set; }

        public UInt64 DataSize { get; set; }

        public UInt32 FileAttributes { get; set; }

        public UInt32 AlignmentOrReserved { get; set; }

        public String Name { get; set; }
    }
}
