using System;
using System.Threading;
using TDefragLib.FS.KnownBootSector;

namespace TDefragLib.FS
{
    /// <summary>
    /// A partition or logical volume
    /// </summary>
    public class Volume
    {
        private const int BootSectorLength = 512;

        private const UInt16 BootSectorSignature = 0xAA55;
        private const Int32 BootSectorSignatureOffset = 510;

        private const String FileSystemTypeNtfsSignature = "4E-54-46-53";
        
        private const Int32 FileSystemTypeNtfsSignatureOffset = 0x03;
        private const Int32 FileSystemTypeNtfsSignatureLength = 4;

        private const String FileSystemTypeFat32Signature = "Unknown";
        
        private const Int32 FileSystemTypeFat32SignatureOffset = 0x52;
        private const Int32 FileSystemTypeFat32SignatureLength = 5;

        private const String FileSystemTypeFat16Signature = "Unknown";
        
        private const Int32 FileSystemTypeFat16SignatureOffset = 0x36;
        private const Int32 FileSystemTypeFat16SignatureLength = 3;

        private IntPtr VolumeHandle;

        /// <summary>
        /// Create a volume by giving a handle
        /// </summary>
        /// <param name="handle"></param>
        public Volume(IntPtr handle)
        {
            VolumeHandle = handle;
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

                Byte[] buffer = new Byte[BootSectorLength];
                Overlapped overlapped = Helper.OverlappedBuilder.Get();

                int bytesRead = Helper.UnsafeNativeMethods.Read(VolumeHandle, buffer, 0, BootSectorLength, overlapped);
                
                if (bytesRead != BootSectorLength)
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
            if (BitConverter.ToUInt16(buffer, BootSectorSignatureOffset) != BootSectorSignature)
                throw new Exception("This seems not to be a valid boot sector!");

            String s = String.Empty;

            s = BitConverter.ToString(buffer, FileSystemTypeNtfsSignatureOffset, FileSystemTypeNtfsSignatureLength);

            if (s.Equals(FileSystemTypeNtfsSignature))
                return FS.FileSystemType.Ntfs;

            s = BitConverter.ToString(buffer, FileSystemTypeFat32SignatureOffset, FileSystemTypeFat32SignatureLength);

            if (s.Equals(FileSystemTypeFat32Signature))
                return FS.FileSystemType.Fat32;

            s = BitConverter.ToString(buffer, FileSystemTypeFat16SignatureOffset, FileSystemTypeFat16SignatureLength);

            if (s.Equals(FileSystemTypeFat16Signature))
                return FS.FileSystemType.Fat16;

            return FS.FileSystemType.UnknownType;
        }
    }
}
