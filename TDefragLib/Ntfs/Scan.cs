using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.Ntfs
{
    class Scan
    {
        public Scan(MainLib parent)
        {
            Lib = parent;
        }

        public void AnalyzeVolume()
        {
            // Read the boot block from the disk.
            FS.IBootSector bootSector = Lib.Data.volume.BootSector;

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.Filesystem.NTFS)
            {
                return;
            }

/*            DiskInformation diskInfo = new DiskInformation(bootSector);

            _lib.Data.BytesPerCluster = diskInfo.BytesPerCluster;

            if (diskInfo.SectorsPerCluster > 0)
            {
                _lib.Data.TotalClusters = diskInfo.TotalSectors / diskInfo.SectorsPerCluster;
            }

            ShowDebug(0, "This is an NTFS disk.");

            ShowDebug(2, String.Format(LogMessage.messages[18], bootSector.OemId));
            ShowDebug(2, String.Format(LogMessage.messages[19], diskInfo.BytesPerSector));
            ShowDebug(2, String.Format(LogMessage.messages[20], diskInfo.TotalSectors));
            ShowDebug(2, String.Format(LogMessage.messages[21], diskInfo.SectorsPerCluster));

            ShowDebug(2, String.Format(LogMessage.messages[22], bootSector.SectorsPerTrack));
            ShowDebug(2, String.Format(LogMessage.messages[23], bootSector.NumberOfHeads));
            ShowDebug(2, String.Format(LogMessage.messages[24], diskInfo.MftStartLcn));
            ShowDebug(2, String.Format(LogMessage.messages[25], diskInfo.Mft2StartLcn));
            ShowDebug(2, String.Format(LogMessage.messages[26], diskInfo.BytesPerMftRecord));
            ShowDebug(2, String.Format(LogMessage.messages[27], diskInfo.ClustersPerIndexRecord));

            ShowDebug(2, String.Format(LogMessage.messages[28], bootSector.MediaType));
            ShowDebug(2, String.Format(LogMessage.messages[29], bootSector.Serial));

            // Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation API cannot move these inodes.
            _lib.Data.Disk.MftLockedClusters = diskInfo.BytesPerCluster / diskInfo.BytesPerMftRecord;

            // Read the $MFT record from disk into memory, which is always the first record in the MFT.
            UInt64 tempLcn = diskInfo.MftStartLcn * diskInfo.BytesPerCluster;

            ByteArray Buffer = new ByteArray((Int64)MFTBUFFERSIZE);

            _lib.Data.Disk.ReadFromCluster(tempLcn, Buffer.Bytes, 0,
                (Int32)diskInfo.BytesPerMftRecord);

            FixupRawMftdata(diskInfo, Buffer, 0, diskInfo.BytesPerMftRecord);

            // Extract data from the MFT record and put into an Item struct in memory. If
            // there was an error then exit.
            //
            FragmentList MftDataFragments = null;
            FragmentList MftBitmapFragments = null;

            UInt64 MftDataBytes = 0;
            UInt64 MftBitmapBytes = 0;

            Boolean Result = InterpretMftRecord(diskInfo, null, 0, 0,
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                Helper.BinaryReader(Buffer), diskInfo.BytesPerMftRecord);

            ShowDebug(6, String.Format(LogMessage.messages[30], MftDataBytes, MftBitmapBytes));

            BitmapFile bitmapFile = new BitmapFile(_lib.Data.Disk,
                diskInfo, MftBitmapFragments, MftBitmapBytes, MftDataBytes);

            UInt64 MaxInode = bitmapFile.MaxInode;

            ItemStruct[] InodeArray = new ItemStruct[MaxInode];
            //InodeArray[0] = _lib.Data.ItemTree;
            //ItemStruct Item = null;

            _lib.Data.PhaseDone = 0;
            _lib.Data.PhaseTodo = 0;

            DateTime startTime = DateTime.Now;

            _lib.Data.PhaseTodo = bitmapFile.UsedInodes;

            // Read and process all the records in the MFT. The records are read into a
            // buffer and then given one by one to the InterpretMftRecord() subroutine.

            UInt64 BlockStart = 0;
            UInt64 BlockEnd = 0;
            UInt64 InodeNumber = 0;

            foreach (bool bit in bitmapFile.Bits)
            {
                if (_lib.Data.Running != RunningState.Running)
                    break;

                if (_lib.Data.Reparse == true)
                    _lib.ParseDiskBitmap();

                // Ignore the m_iNode if the bitmap says it's not in use.
                if (!bit)
                {
                    InodeNumber++;
                    continue;
                }

                // Update the progress counter
                _lib.Data.PhaseDone++;

                if (_lib.Data.PhaseDone >= _lib.Data.PhaseTodo)
                    break;

                // Read a block of inode's into memory
                if (InodeNumber >= BlockEnd)
                {
                    BlockStart = InodeNumber;
                    BlockEnd = BlockStart + diskInfo.BytesToInode(MFTBUFFERSIZE);

                    if (BlockEnd > MftBitmapBytes * 8)
                        BlockEnd = MftBitmapBytes * 8;

                    Fragment foundFragment = MftDataFragments.FindContaining(
                        diskInfo.InodeToCluster(InodeNumber));

                    UInt64 u1 = diskInfo.ClusterToInode(foundFragment.NextVcn);

                    if (BlockEnd > u1)
                        BlockEnd = u1;

                    UInt64 lcn = diskInfo.ClusterToBytes(foundFragment.Lcn - foundFragment.Vcn) + diskInfo.InodeToBytes(BlockStart);

                    //Console.WriteLine("Reading block of {0} Inodes from MFT into memory, {1} bytes from LCN={2}",
                    //    BlockEnd - BlockStart, diskInfo.InodeToBytes(BlockEnd - BlockStart),
                    //    diskInfo.BytesToCluster(lcn));

                    _lib.Data.Disk.ReadFromCluster(lcn,
                        Buffer.Bytes, 0, (Int32)diskInfo.InodeToBytes(BlockEnd - BlockStart));
                }

                // Fixup the raw data of this m_iNode
                UInt64 position = diskInfo.InodeToBytes(InodeNumber - BlockStart);

                FixupRawMftdata(diskInfo,
                        Buffer, (Int64)position,
                    //Buffer.ToByteArray((Int64)position, Buffer.GetLength() - (Int64)(position)), 0, 
                        diskInfo.BytesPerMftRecord);

                // Interpret the m_iNode's attributes.
                Result = InterpretMftRecord(diskInfo, InodeArray, InodeNumber, MaxInode,
                        ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                        Helper.BinaryReader(Buffer, (Int64)diskInfo.InodeToBytes(InodeNumber - BlockStart)),
                        diskInfo.BytesPerMftRecord);

                if (_lib.Data.PhaseDone % 50 == 0)
                {
                    _lib.ShowProgress((Double)(_lib.Data.PhaseDone), (Double)_lib.Data.PhaseTodo);
                }

                InodeNumber++;
            }

            DateTime endTime = DateTime.Now;

            if (endTime > startTime)
            {
                ShowDebug(2, String.Format(LogMessage.messages[31],
                      (Int64)MaxInode * 1000 / (endTime - startTime).TotalMilliseconds));
            }

            using (_lib.Data.Disk)
            {
                if (_lib.Data.Running != RunningState.Running)
                {
                    _lib.itemList = null;
                    return false;
                }

                // Setup the ParentDirectory in all the items with the info in the InodeArray.
                foreach (ItemStruct item in _lib.itemList)
                {
                    item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)item.ParentInode);
                    if (item.ParentInode == 5)
                        item.ParentDirectory = null;
                }
            }
            return true;*/
        }

        private MainLib Lib;
    }
}
