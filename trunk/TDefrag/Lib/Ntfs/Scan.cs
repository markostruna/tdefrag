using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.FS.Ntfs;
using TDefragLib.Helper;
using System.IO;
using TDefragLib.FileSystem.Ntfs;
using System.Diagnostics;

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

        /// <summary>
        /// If expression is true, exception is thrown
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="message"></param>
        /// <param name="throwException"></param>
        public void ErrorCheck(Boolean expression, String message, Boolean throwException)
        {
            if (expression && throwException)
            {
                //throw new Exception(message);
            }
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
            
            FragmentList MftDataFragments = null;
            FragmentList MftBitmapFragments = null;

            UInt64 MftDataBytes = 0;
            UInt64 MftBitmapBytes = 0;

            Boolean Result = InterpretMftRecord(diskInfo, null, 0, 0,
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                TDefragLib.FS.Ntfs.Helper.BinaryReader(diskBuffer), diskInfo.BytesPerMftRecord);

/*            ShowDebug(6, String.Format(LogMessage.messages[30], MftDataBytes, MftBitmapBytes));

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
            const String validRecordType = "FILE";

            String recordType = diskBuffer.GetRecordType(bufferStart, validRecordType.Length);

            // If this is not a FILE record then return FALSE
            if (validRecordType.Equals(recordType) == false)
            {
                Lib.ShowMessage("This is not a valid MFT record, it does not begin with FILE (maybe trying to read past the end?).");

                return false;
            }

            // Walk through all the sectors and restore the last 2 bytes with the value from the Usa array.
            // If we encounter bad sector data then return with false. 
            RecordHeader RecordHeader = diskBuffer.GetRecordHeader(0);

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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="diskInfo"></param>
        /// <param name="inodeArray"></param>
        /// <param name="inodeNumber"></param>
        /// <param name="maxInode"></param>
        /// <param name="mftDataFragments"></param>
        /// <param name="mftDataBytes"></param>
        /// <param name="mftBitmapFragments"></param>
        /// <param name="mftBitmapBytes"></param>
        /// <param name="reader"></param>
        /// <param name="bufLength"></param>
        /// <returns></returns>
        Boolean InterpretMftRecord(
            DiskInformation diskInfo, Array inodeArray,
            UInt64 inodeNumber, UInt64 maxInode,
            ref FragmentList mftDataFragments, ref UInt64 mftDataBytes,
            ref FragmentList mftBitmapFragments, ref UInt64 mftBitmapBytes,
            BinaryReader reader, UInt64 bufLength)
        {
            //Trace.WriteLine(this, String.Format(
            //    "InterpretMftRecord Inode: {0:G}, Max: {1:G}", inodeNumber, maxInode));

            Int64 position = reader.BaseStream.Position;

            FileRecordHeader fileRecordHeader = diskBuffer.GetFileRecordHeader(0);

            // If the record is not in use then quietly exit
            if (!fileRecordHeader.IsInUse)
            {
                ShowLogMessage(String.Format("Inode {0:G} is not in use.", inodeNumber));
                return false;
            }

            // If the record has a BaseFileRecord then ignore it. It is used by an
            // AttributeAttributeList as an extension of another m_iNode, it's not an
            // Inode by itself.
            //
            UInt64 BaseInode = fileRecordHeader.BaseFileRecord.BaseInodeNumber;

            if (BaseInode != 0)
            {
                //ShowDebug(6, String.Format("Ignoring Inode {0:G}, it's an extension of Inode {1:G}", inodeNumber, BaseInode));
                return true;
            }

            // ShowDebug(6, String.Format("Processing Inode {0:G}...", InodeNumber));

            // Show a warning if the Flags have an unknown value.
            if (fileRecordHeader.IsUnknown)
            {
                // ShowDebug(6, String.Format("  Inode {0:G} has Flags = {1:G}", InodeNumber, fileRecordHeader.Flags));
            }

            // I think the MFTRecordNumber should always be the InodeNumber, but it's an XP
            // extension and I'm not sure about Win2K.
            // 
            // Note: why is the MFTRecordNumber only 32 bit? Inode numbers are 48 bit.
            //
            ErrorCheck(fileRecordHeader.MFTRecordNumber != inodeNumber,
                String.Format("Warning: Inode {0:G} contains a different MFTRecordNumber {1:G}",
                      inodeNumber, fileRecordHeader.MFTRecordNumber), true);

            ErrorCheck(
                fileRecordHeader.AttributeOffset >= bufLength,
                String.Format("Error: attributes in m_iNode {0:G} are outside the FILE record, the MFT may be corrupt.",
                      inodeNumber),
                 true);

            ErrorCheck(
                fileRecordHeader.BytesInUse > bufLength,
                String.Format("Error: in m_iNode {0:G} the record is bigger than the size of the buffer, the MFT may be corrupt.",
                      inodeNumber),
                true);

            InodeDataStructure inodeData = new InodeDataStructure(inodeNumber);

            inodeData.IsDirectory = fileRecordHeader.IsDirectory;
            inodeData.MftDataFragments = mftDataFragments;
            inodeData.MftDataLength = mftDataBytes;

            // Make sure that directories are always created.
            if (inodeData.IsDirectory)
            {
                //AttributeType attributeType = AttributeTypeEnum.AttributeIndexAllocation;
                //TranslateRundataToFragmentlist(inodeData, "$I30", attributeType, null, 0, 0, 0);
            }

            // Interpret the attributes.
            reader.BaseStream.Seek(position + fileRecordHeader.AttributeOffset, SeekOrigin.Begin);

            Boolean Result = true;

            try
            {
                Boolean Res = ProcessAttributes(diskInfo, inodeData,
                    reader, bufLength - fileRecordHeader.AttributeOffset, UInt16.MaxValue, 0);

                if (Res == false)
                    Result = false;
            }
            catch (Exception)
            {
                Result = false;
                _countProcessAttributesIssues++;
                Trace.WriteLine(this, String.Format("ProcessAttributes failed for {0} (cnt={1})",
                    inodeData.LongFilename, _countProcessAttributesIssues));
            }
/*
            // Save the MftDataFragments, MftDataBytes, MftBitmapFragments, and MftBitmapBytes.
            if (inodeNumber == 0)
            {
                mftDataFragments = inodeData.MftDataFragments;
                mftDataBytes = inodeData.MftDataLength;
                mftBitmapFragments = inodeData.MftBitmapFragments;
                mftBitmapBytes = inodeData.MftBitmapLength;
            }

            int countFiles = 0;

            // Create an item in the Data->ItemTree for every stream.
            foreach (Stream stream in inodeData.Streams)
            {
                // Create and fill a new item record in memory.
                ItemStruct Item = new ItemStruct(stream);

                Item.LongFilename = ConstructStreamName(inodeData.LongFilename, inodeData.ShortFilename, stream);
                Item.LongPath = null;

                Item.ShortFilename = ConstructStreamName(inodeData.ShortFilename, inodeData.LongFilename, stream);
                Item.ShortPath = null;

                //Item.Bytes = inodeData.TotalBytes;
                Item.Bytes = stream.TotalBytes;

                //Item.Clusters = 0;
                Item.NumClusters = stream.Clusters;

                Item.CreationTime = inodeData.CreationTime;
                Item.MftChangeTime = inodeData.MftChangeTime;
                Item.LastAccessTime = inodeData.LastAccessTime;

                Item.ParentInode = inodeData.ParentInode;
                Item.IsDirectory = inodeData.IsDirectory;
                Item.Unmovable = false;
                Item.Exclude = false;
                Item.SpaceHog = false;
                Item.Error = !Result;

                // Increment counters
                if (Item.IsDirectory)
                {
                    _lib.Data.CountDirectories++;
                }

                _lib.Data.CountAllFiles++;

                if (stream.Type.IsData)
                {
                    _lib.Data.CountAllBytes += inodeData.TotalBytes;
                }

                _lib.Data.CountAllClusters += stream.Clusters;

                if (Item.FragmentCount > 1)
                {
                    _lib.Data.CountFragmentedItems++;
                    _lib.Data.CountFragmentedBytes += inodeData.TotalBytes;

                    if (stream != null) _lib.Data.CountFragmentedClusters += stream.Clusters;
                }

                // Add the item record to the sorted item tree in memory.
                _lib.AddItemToList(Item);

                //  Also add the item to the array that is used to construct the full pathnames.
                //
                //  NOTE:
                //  If the array already contains an entry, and the new item has a shorter
                //  filename, then the entry is replaced. This is needed to make sure that
                //  the shortest form of the name of directories is used.
                //
                ItemStruct InodeItem = null;

                if (inodeArray != null && inodeNumber < maxInode)
                {
                    InodeItem = (ItemStruct)inodeArray.GetValue((Int64)inodeNumber);
                }

                String InodeLongFilename = String.Empty;

                if (InodeItem != null)
                {
                    InodeLongFilename = InodeItem.LongFilename;
                }

                if (InodeLongFilename.CompareTo(Item.LongFilename) > 0)
                {
                    inodeArray.SetValue(Item, (Int64)inodeNumber);
                }

                //if ((Item != null) && (countFiles % 300 == 0))
                //    ShowDebug(2, "File: " + (String.IsNullOrEmpty(Item.LongFilename) ? (String.IsNullOrEmpty(Item.ShortFilename) ? "" : Item.ShortFilename) : Item.LongFilename));

                countFiles++;

                // Draw the item on the screen.
                if (_lib.Data.Reparse == false)
                {
                    _lib.ColorizeItem(Item, 0, 0, false, Item.Error);
                }
                else
                {
                    _lib.ParseDiskBitmap();
                }
            }
            */
            return true;
        }

        /// <summary>
        /// Process a list of attributes and store the gathered information in the Item
        /// struct. Return FALSE if an error occurred.
        /// </summary>
        /// <param name="diskInfo"></param>
        /// <param name="inodeData"></param>
        /// <param name="reader"></param>
        /// <param name="bufLength"></param>
        /// <param name="instance"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        Boolean ProcessAttributes(
            DiskInformation diskInfo, InodeDataStructure inodeData,
            BinaryReader reader, UInt64 bufLength,
            UInt16 instance, int depth)
        {
            //Trace.WriteLine(this, String.Format(
            //    "ProcessAttributes Inode: {0:G}, Len: {1:G}", inodeData.Inode, bufLength));

            TDefragLib.FileSystem.Ntfs.Attribute attribute;
            Int64 position = reader.BaseStream.Position;

            Boolean FunctionResult = true;
            Boolean Result = true;

            // Walk through all the attributes and gather information. AttributeLists are
            // skipped and interpreted later.
            //
            for (UInt32 offset = 0; offset < bufLength; offset += attribute.Length)
            {
                attribute = diskBuffer.GetAttribute(position + offset);

                if (attribute.Type.IsEndOfList) break;

                // Exit the loop if end-marker.
                if ((offset + 4 <= bufLength) && attribute.Type.IsInvalid) break;

                ErrorCheck(
                    (offset + 4 > bufLength) ||
                    (attribute.Length < 3) ||
                    (offset + attribute.Length > bufLength),
                    String.Format("Error: attribute in m_iNode {0:G} is bigger than the data, the MFT may be corrupt.", inodeData.Inode), true);

                // Skip AttributeList's for now.
                if (attribute.Type.IsAttributeList) continue;

                // If the Instance does not equal the m_attributeNumber then ignore the attribute.
                // This is used when an AttributeList is being processed and we only want a specific instance.

                if ((instance != UInt16.MaxValue) && (instance != attribute.Number)) continue;

/*
                reader.BaseStream.Seek(position + offset, SeekOrigin.Begin);
                if (attribute.IsNonResident)
                {
                    Result = ParseNonResidentAttribute(inodeData, reader, offset,
                        attribute, position);
                }
                else
                {
                    Result = ParseResidentAttribute(inodeData, reader, offset,
                        attribute, position);
                }
*/
                if (Result == false)
                {
                    FunctionResult = false;
                }
            }

            // Walk through all the attributes and interpret the AttributeLists. We have to
            // do this after the DATA and BITMAP attributes have been interpreted, because
            // some MFT's have an AttributeList that is stored in fragments that are
            // defined in the DATA attribute, and/or contain a continuation of the DATA or
            // BITMAP attributes.
            //
            for (UInt32 offset = 0; offset < bufLength; offset += attribute.Length)
            {
                attribute = diskBuffer.GetAttribute(position + offset);

                if (attribute.Type.IsEndOfList || attribute.Type.IsInvalid)
                {
                    break;
                }

                if (!attribute.Type.IsAttributeList)
                {
                    continue;
                }

                //ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.Number, attribute.Type.GetStreamTypeName()));

/*                reader.BaseStream.Seek(position + offset, SeekOrigin.Begin);
                if (attribute.IsNonResident)
                {
                    Result = ParseNonResidentAttributesFull(diskInfo, inodeData, reader,
                        depth, attribute, position, offset);
                }
                else
                {
                    Result = ParseResidentAttributesFull(diskInfo, inodeData, reader,
                        depth, position, offset);
                }
                */
                if (Result == false)
                {
                    FunctionResult = false;
                }
            }

            return FunctionResult;
        }

        private MainLib Lib;
        private DiskBuffer diskBuffer;

        private Int32 _countProcessAttributesIssues = 0;    // 855
    }

}
