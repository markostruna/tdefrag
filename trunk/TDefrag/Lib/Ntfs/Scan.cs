using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.FS.Ntfs;
using TDefragLib.Helper;

namespace TDefragLib.Ntfs
{
    class Scan
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;

        public Scan(MainLib parent)
        {
            Lib = parent;
        }

        public void ShowLogMessage(String message)
        {
            Lib.ShowMessage(message);
        }

        public void AnalyzeVolume()
        {
            // Read the boot block from the disk.
            FS.IBootSector bootSector = Lib.Data.volume.BootSector;

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.Filesystem.NTFS)
            {
                ShowLogMessage("This is not NTFS disk.");
                return;
            }

            DiskInformation diskInfo = new DiskInformation(bootSector);

            Lib.Data.BytesPerCluster = diskInfo.BytesPerCluster;

            if (diskInfo.SectorsPerCluster > 0)
            {
                Lib.Data.NumClusters = diskInfo.TotalSectors / diskInfo.SectorsPerCluster;
            }

            ShowLogMessage("This is an NTFS disk.");

            ShowLogMessage(String.Format("  Disk cookie: {0:X}", bootSector.OemId));
            ShowLogMessage(String.Format("  BytesPerSector: {0:G}", diskInfo.BytesPerSector));
            ShowLogMessage(String.Format("  TotalSectors: {0:G}", diskInfo.TotalSectors));

            ShowLogMessage(String.Format("  SectorsPerCluster: {0:G}", diskInfo.SectorsPerCluster));
            ShowLogMessage(String.Format("  SectorsPerTrack: {0:G}", bootSector.SectorsPerTrack));
            ShowLogMessage(String.Format("  NumberOfHeads: {0:G}", bootSector.NumberOfHeads));
            ShowLogMessage(String.Format("  MftStartLcn: {0:G}", diskInfo.MftStartLcn));
            ShowLogMessage(String.Format("  Mft2StartLcn: {0:G}", diskInfo.Mft2StartLcn));
            ShowLogMessage(String.Format("  BytesPerMftRecord: {0:G}", diskInfo.BytesPerMftRecord));
            ShowLogMessage(String.Format("  ClustersPerIndexRecord: {0:G}", diskInfo.ClustersPerIndexRecord));
            ShowLogMessage(String.Format("  MediaType: {0:X}", bootSector.MediaType));
            ShowLogMessage(String.Format("  VolumeSerialNumber: {0:X}", bootSector.Serial));

            // Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation API cannot move these inodes.
            Lib.Data.MftLockedClusters = diskInfo.BytesPerCluster / diskInfo.BytesPerMftRecord;

            // Read the $MFT record from disk into memory, which is always the first record in the MFT.
            UInt64 tempLcn = diskInfo.MftStartLcn * diskInfo.BytesPerCluster;

            diskBuffer = new DiskBuffer((Int64)MFTBUFFERSIZE);

            // read MFT record
            Boolean result = Lib.Data.volume.ReadFromCluster(tempLcn, diskBuffer.Buffer, 0,
                (Int32)diskInfo.BytesPerMftRecord);

            if (result == false)
            {
                ShowLogMessage("Could not read buffer!!");
            }

            // Update sequence numbers in all sectors
            UpdateSequenceNumbers(diskInfo, 0, diskInfo.BytesPerMftRecord);

            // Extract data from the MFT record and put into an Item struct in memory.
            // If there was an error then exit.
            
/*            FragmentList MftDataFragments = null;
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

                UpdateSequenceNumbers(diskInfo,
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

        /// <summary>
        /// Updates Sequence Numbers.
        /// 
        /// - To protect against disk failure, the last 2 bytes of every sector in the MFT are
        ///   not stored in the sector itself, but in the "Usa" array in the header (described
        ///   by UsaOffset and UsaCount). The last 2 bytes are copied into the array and the
        ///   Update Sequence Number is written in their place.
        /// - The Update Sequence Number is stored in the first item (item zero) of the "Usa" array.
        /// - The number of bytes per sector is defined in the $Boot record.
        /// </summary>
        /// 
        /// <param name="DiskInfo">Disk information</param>
        /// <param name="bufferStart">Start of the buffer</param>
        /// <param name="BufLength">Length of the buffer</param>
        /// 
        /// <returns>
        /// Return true if everything is ok, false if the MFT data is corrupt.
        /// 
        /// NOTE:
        /// This can also happen when we have read a record past the end of the MFT, maybe it has
        /// shrunk while we were processing.
        /// </returns>
        private Boolean UpdateSequenceNumbers(DiskInformation DiskInfo, Int64 bufferStart, UInt64 BufLength)
        {
            UInt32 record = BitConverter.ToUInt32(diskBuffer.Buffer, (Int32)bufferStart);

            // If this is not a FILE record then return FALSE
            if (record != 0x454c4946)
            {
                Lib.ShowMessage("This is not a valid MFT record, it does not begin with FILE (maybe trying to read past the end?).");

                return false;
            }

            // Walk through all the sectors and restore the last 2 bytes with the value from the Usa array.
            // If we encounter bad sector data then return with false. 
            RecordHeader RecordHeader = RecordHeader.Parse(FS.Ntfs.Helper.BinaryReader(diskBuffer));

            UInt64 Increment = DiskInfo.BytesPerSector / sizeof(UInt16);
            UInt64 index = Increment - 1;

            for (UInt16 i = 1; i < RecordHeader.UsaCount; i++)
            {
                // Check if we are inside the buffer.
                if (index * sizeof(UInt16) >= BufLength)
                {
                    Lib.ShowMessage("Warning: USA data indicates that data is missing, the MFT may be corrupt.");

                    return false;
                }

                // Check if the last 2 bytes of the sector contain the Update Sequence Number.
                if (diskBuffer.Buffer[index * sizeof(UInt16) + 0] != diskBuffer.Buffer[RecordHeader.UsaOffset + 0] ||
                    diskBuffer.Buffer[index * sizeof(UInt16) + 1] != diskBuffer.Buffer[RecordHeader.UsaOffset + 1])
                {
                    Lib.ShowMessage("Error: USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.");

                    return false;
                }

                // Replace the last 2 bytes in the sector with the value from the Usa array.
                diskBuffer.Buffer[index * sizeof(UInt16) + 0] = diskBuffer.Buffer[RecordHeader.UsaOffset + i * sizeof(UInt16) + 0];
                diskBuffer.Buffer[index * sizeof(UInt16) + 1] = diskBuffer.Buffer[RecordHeader.UsaOffset + i * sizeof(UInt16) + 1];

                // Go to the next sector
                index += Increment;
            }

            return true;
        }

        private MainLib Lib;
        private DiskBuffer diskBuffer;
    }
}
