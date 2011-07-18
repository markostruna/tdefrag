using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TDefragLib.FS.KnownBootSector;

namespace TDefragLib.FS
{
    /// <summary>
    /// A partition or logical volume
    /// </summary>
    public class Volume
    {
        private const int BOOT_SECTOR_SIZE = 512;
        private const UInt16 BOOT_SECTOR_SIGNATURE = 0xAA55;

        private IntPtr _handle;

        /// <summary>
        /// Create a volume by giving a handle
        /// </summary>
        /// <param name="handle"></param>
        public Volume(IntPtr handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// Return an abstract representation of the boot sector for this
        /// volume.
        /// </summary>
        public IBootSector BootSector
        {
            get
            {
                BaseBootSector bootSector = null;

                Byte[] buffer = new Byte[BOOT_SECTOR_SIZE];
                Overlapped overlapped = Helper.OverlappedBuilder.Get();

                int bytesRead = Helper.UnsafeNativeMethods.Read(_handle, buffer, 0, BOOT_SECTOR_SIZE, overlapped);
                
                if (bytesRead != BOOT_SECTOR_SIZE)
                    return bootSector;
                
                switch (RecognizeType(buffer))
                {
                    case FileSystemType.Ntfs:
                        bootSector = new KnownBootSector.NtfsBootSector(buffer);
                        break;
                    case FileSystemType.Fat12:
                    case FileSystemType.Fat16:
                    case FileSystemType.Fat32:
                        bootSector = new KnownBootSector.FatBootSector(buffer);
                        break;
                }
                
                return bootSector;
            }
        }

        /// <summary>
        /// Check the filesystem to recognize its type and build the correct
        /// implementation of IBootSector.
        /// </summary>
        /// <param name="buffer">The boot sector</param>
        /// <returns>The FS type</returns>
        private FileSystemType RecognizeType(byte[] buffer)
        {
            FileSystemType fileSystemType = FS.FileSystemType.UnknownType;

            if (BitConverter.ToUInt16(buffer, 510) != BOOT_SECTOR_SIGNATURE)
                throw new Exception("This seems not to be a valid boot sector!");

            String s = String.Empty;

            if (fileSystemType == FS.FileSystemType.UnknownType)
            {
                s = BitConverter.ToString(buffer, 0x03, 4);

                if (s.Equals("4E-54-46-53"))
                {
                    fileSystemType = FS.FileSystemType.Ntfs;
                }
            }

            if (fileSystemType == FS.FileSystemType.UnknownType)
            {
                s = BitConverter.ToString(buffer, 0x52, 5);

                if (String.IsNullOrEmpty(s))
                {
                    fileSystemType = FS.FileSystemType.Fat32;
                }
            }

            if (fileSystemType == FS.FileSystemType.UnknownType)
            {
                s = BitConverter.ToString(buffer, 0x36, 3);

                if (String.IsNullOrEmpty(s))
                {
                    fileSystemType = FS.FileSystemType.Fat16;
                }
            }

            return fileSystemType;
        }
    }
}
