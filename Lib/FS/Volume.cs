using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
                Byte[] buffer = new Byte[BOOT_SECTOR_SIZE];
                Overlapped overlapped = Helper.OverlappedBuilder.Get();
                int bytesRead = Helper.UnsafeNativeMethods.Read(_handle, buffer, 0, BOOT_SECTOR_SIZE, overlapped);
                if (bytesRead != BOOT_SECTOR_SIZE)
                    throw new Exception("Could not read the boot sector from disk!");
                switch (RecognizeType(buffer))
                {
                    case FileSystemType.Ntfs:
                        return new KnownBootSector.NtfsBootSector(buffer);
                    case FileSystemType.Fat12:
                    case FileSystemType.Fat16:
                    case FileSystemType.Fat32:
                        return new KnownBootSector.FatBootSector(buffer);
                }
                throw new NotSupportedException("Unrecognized volume type");
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
            if (BitConverter.ToUInt16(buffer, 510) != BOOT_SECTOR_SIGNATURE)
                throw new Exception("This seems not to be a valid boot sector!");

            String s = String.Empty;
           
            s = BitConverter.ToString(buffer, 0x03, 4);
            
            if (s == "4E-54-46-53")
                return FS.FileSystemType.Ntfs;
            
            s = BitConverter.ToString(buffer, 0x52, 5);
            
            if (String.IsNullOrEmpty(s))
                return FS.FileSystemType.Fat32;
            
            s = BitConverter.ToString(buffer, 0x36, 3);

            if (String.IsNullOrEmpty(s))
                return FS.FileSystemType.Fat16;

            throw new NotImplementedException("Eventually add the strings for other filesystems");
            //return FS.FileSystemType.UnknownType;
        }
    }
}
