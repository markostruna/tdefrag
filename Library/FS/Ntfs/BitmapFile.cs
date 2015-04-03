using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.FS.Ntfs;
using TDefragLib.Helper;
using System.Linq.Expressions;

namespace TDefragLib.FileSystem.Ntfs
{
    public class BitmapFile
    {
        public BitmapFile(Volume volume, DiskInformation diskInfo, FragmentCollection fragments,
            UInt64 bitmapBytes, UInt64 dataBytes)
        {
            BitmapBytes = bitmapBytes;
            DataBytes = dataBytes;
            DiskInfo = diskInfo;

            MasterFileTableBitmapBitArray = volume.Load(diskInfo, fragments);
        }

        /// <summary>
        /// Construct an array of all the items in memory, indexed by m_iNode.
        ///
        /// NOTE:
        /// The maximum number of Inodes is primarily determined by the size of the
        /// bitmap. But that is rounded up to 8 Inodes, and the MFT can be shorter.
        /// </summary>
        public UInt64 MaxInode { get { return Math.Min(BitmapBytes * 8, DiskInfo.BytesToInode(DataBytes)); } }

        public UInt64 UsedInodes
        {
            get
            {
                UInt64 used = 0;
                UInt64 c = 0;

                foreach (bool bit in MasterFileTableBitmapBitArray)
                {
                    if (++c > MaxInode)
                        break;
                    if (bit)
                        used++;
                }
                return used;
            }
        }

        public BitArray Bits { get { return MasterFileTableBitmapBitArray; } }

        private UInt64 BitmapBytes { get; set; }
        private UInt64 DataBytes { get; set; }
        private DiskInformation DiskInfo { get; set; }

        public BitArray MasterFileTableBitmapBitArray { get; set; }
    }
}
