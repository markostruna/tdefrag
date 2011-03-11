using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.FS.Ntfs;
using TDefragLib.Helper;
using System.IO;
using TDefragLib.FileSystem.Ntfs;
using System.Diagnostics;
using System.Threading;
using TDefragWpf.Properties;
using System.Globalization;

namespace TDefragLib.Ntfs
{
    class Scan
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;

        public Scan(MainLib parent)
        {
            Lib = parent;
        }

        public static void ShowLogMessage(String message)
        {
            MainLib.ShowMessage(message);
        }

        /// <summary>
        /// If expression is true, exception is thrown
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="message"></param>
        /// <param name="throwException"></param>
        public static void ErrorCheck(Boolean expression, String message, Boolean throwException)
        {
            if (expression && throwException)
            {
                //throw new Exception(message);
            }
        }

        public void AnalyzeVolume()
        {
            // Read the boot block from the disk.
            FS.IBootSector bootSector = Lib.Data.Volume.BootSector;

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.FileSystemType.Ntfs)
            {
                ShowLogMessage(Resources.ErrorNotNtfsDisk);
                return;
            }

            DiskInformation diskInfo = new DiskInformation(bootSector);

            Lib.Data.BytesPerCluster = diskInfo.BytesPerCluster;

            if (diskInfo.SectorsPerCluster > 0)
            {
                Lib.Data.NumberOfClusters = diskInfo.TotalSectors / diskInfo.SectorsPerCluster;
            }

            ShowLogMessage(Resources.InfoNtfsDisk);

            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoCookie, bootSector.OemId));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoBytesPerSector, diskInfo.BytesPerSector));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoTotalSectors, diskInfo.TotalSectors));

            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoSectorsPerCluster, diskInfo.SectorsPerCluster));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoSectorsPerTrack, bootSector.SectorsPerTrack));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoNumberOfHeads, bootSector.NumberOfHeads));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoMftStartLcn, diskInfo.MasterFileTableStartLogicalClusterNumber));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoMft2StartLcn, diskInfo.MasterFileTable2StartLogicalClusterNumber));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoBytesPerMftRecord, diskInfo.BytesPerMasterFileTableRecord));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoClusterPerIndexRecord, diskInfo.ClustersPerIndexRecord));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoMediaType, bootSector.MediaType));
            ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.DiskInfoVolumeSerialNumber, bootSector.Serial));

            // Calculate the size of first 16 Inodes in the MFT. The Microsoft defragmentation API cannot move these inodes.
            Lib.Data.MasterFileTableLockedClusters = diskInfo.BytesPerCluster / diskInfo.BytesPerMasterFileTableRecord;

            // Read the $MFT record from disk into memory, which is always the first record in the MFT.
            UInt64 tempLcn = diskInfo.MasterFileTableStartLogicalClusterNumber * diskInfo.BytesPerCluster;

            DiskBuffer diskBuffer = new DiskBuffer((Int64)MFTBUFFERSIZE);

            // read MFT record
            Boolean result = Lib.Data.Volume.ReadFromCluster(tempLcn, diskBuffer.Buffer, 0,
                (Int32)diskInfo.BytesPerMasterFileTableRecord);

            if (result == false)
            {
                ShowLogMessage(Resources.ErrorCouldNotReadBuffer);
            }

            // Update sequence numbers in all sectors
            UpdateSequenceNumbers(diskInfo, diskBuffer, 0, diskInfo.BytesPerMasterFileTableRecord);

            // Extract data from the MFT record and put into an Item struct in memory.
            // If there was an error then exit.

            FragmentCollection MftDataFragments = null;
            FragmentCollection MftBitmapFragments = null;

            UInt64 MftDataBytes = 0;
            UInt64 MftBitmapBytes = 0;

            diskBuffer.ReaderPosition = 0;

            Boolean Result = InterpretMftRecord(diskInfo, null, 0, 0,
                ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                diskBuffer, diskInfo.BytesPerMasterFileTableRecord);

            //ShowLogMessage(String.Format("MftDataBytes = {0:G}, MftBitmapBytes = {0:G}", MftDataBytes, MftBitmapBytes));

            BitmapFile bitmapFile = new BitmapFile(Lib.Data.Volume,
                diskInfo, MftBitmapFragments, MftBitmapBytes, MftDataBytes);

            UInt64 MaxInode = bitmapFile.MaxInode;

            ItemStruct[] InodeArray = new ItemStruct[MaxInode];
            //InodeArray[0] = _lib.Data.ItemTree;
            //ItemStruct Item = null;

            Lib.Data.TasksCompleted = 0;
            Lib.Data.TasksCount = 0;

            DateTime startTime = DateTime.Now;

            Lib.Data.TasksCount = bitmapFile.UsedInodes;

            // Read and process all the records in the MFT. The records are read into a
            // buffer and then given one by one to the InterpretMftRecord() subroutine.

            UInt64 BlockStart = 0;
            UInt64 BlockEnd = 0;
            UInt64 InodeNumber = 0;

            foreach (bool bit in bitmapFile.Bits)
            {
                //if (Lib.Data.Running != RunningState.Running)
                //    break;

                //if (Lib.Data.Reparse == true)
                //    Lib.ParseDiskBitmap();

                // Ignore the m_iNode if the bitmap says it's not in use.
                if (!bit)
                {
                    InodeNumber++;
                    continue;
                }

                // Update the progress counter
                Lib.Data.TasksCompleted++;

                if (Lib.Data.TasksCompleted >= Lib.Data.TasksCount)
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

                    if (foundFragment == null)
                    {
                        break;
                    }

                    UInt64 u1 = diskInfo.ClusterToInode(foundFragment.NextVirtualClusterNumber);

                    if (BlockEnd > u1)
                        BlockEnd = u1;

                    UInt64 lcn = diskInfo.ClusterToBytes(foundFragment.LogicalClusterNumber - foundFragment.VirtualClusterNumber) + diskInfo.InodeToBytes(BlockStart);

                    //Console.WriteLine("Reading block of {0} Inodes from MFT into memory, {1} bytes from LCN={2}",
                    //    BlockEnd - BlockStart, diskInfo.InodeToBytes(BlockEnd - BlockStart),
                    //    diskInfo.BytesToCluster(logicalClusterNumber));

                    Lib.Data.Volume.ReadFromCluster(lcn,
                        diskBuffer.Buffer, 0, (Int32)diskInfo.InodeToBytes(BlockEnd - BlockStart));
                }

                // Fixup the raw data of this m_iNode
                UInt64 position = diskInfo.InodeToBytes(InodeNumber - BlockStart);

                UpdateSequenceNumbers(diskInfo, diskBuffer, (Int64)position, diskInfo.BytesPerMasterFileTableRecord);

                diskBuffer.ReaderPosition = (Int64)diskInfo.InodeToBytes(InodeNumber - BlockStart);
                // Interpret the m_iNode's attributes.
                Result = InterpretMftRecord(diskInfo, InodeArray, InodeNumber, MaxInode,
                        ref MftDataFragments, ref MftDataBytes, ref MftBitmapFragments, ref MftBitmapBytes,
                        diskBuffer, diskInfo.BytesPerMasterFileTableRecord);

                if (Lib.Data.TasksCompleted % 500 == 0)
                {
                    ShowLogMessage(Lib.Data.TasksCompleted + Resources.stringSlash + Lib.Data.TasksCount);
                //    //Lib.ShowProgress((Double)(Lib.Data.PhaseDone), (Double)Lib.Data.PhaseTodo);
                }

                Thread.Sleep(10);

                InodeNumber++;
            }

            DateTime endTime = DateTime.Now;

            if (endTime > startTime)
            {
                ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.InfoAnalysisSpeed, (Int64)MaxInode * 1000 / (endTime - startTime).TotalMilliseconds));
            }

            using (Lib.Data.Volume)
            {
                //if (Lib.Data.Running != RunningState.Running)
                //{
                //    Lib.itemList = null;
                //    return false;
                //}

                // Setup the ParentDirectory in all the items with the info in the InodeArray.
                foreach (ItemStruct item in Lib.ItemCollection)
                {
                    item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)item.ParentInode);

                    if (item.ParentInode == 5)
                        item.ParentDirectory = null;
                }
            }

            return;
        }

        /// <summary>
        /// Updates Sequence Numbers.
        /// </summary>
        /// 
        /// <remarks>
        /// - To protect against disk failure, the last 2 bytes of every sector in the MFT are
        ///   not stored in the sector itself, but in the "Usa" array in the header (described
        ///   by UsaOffset and UsaCount). The last 2 bytes are copied into the array and the
        ///   Update Sequence Number is written in their place.
        /// - The Update Sequence Number is stored in the first item (item zero) of the "Usa" array.
        /// - The number of bytes per sector is defined in the $Boot record.
        /// </remarks>
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
        private Boolean UpdateSequenceNumbers(DiskInformation DiskInfo, DiskBuffer diskBuffer, Int64 bufferStart, UInt64 BufLength)
        {
            const String validRecordType = "FILE";

            String recordType = diskBuffer.GetString(bufferStart, validRecordType.Length);

            // If this is not a FILE record then return FALSE

            if (validRecordType.Equals(recordType) == false)
            {
                ShowLogMessage(Resources.ErrorNotValidMFTDoesStartWithFILE);

                return false;
            }

            // Walk through all the sectors and restore the last 2 bytes with the value from the Usa array.
            // If we encounter bad sector data then return with false.
 
            RecordHeader RecordHeader = diskBuffer.GetRecordHeader(0);

            UInt64 Increment = DiskInfo.BytesPerSector / sizeof(UInt16);
            UInt64 index = Increment - 1;

            for (UInt16 i = 1; i < RecordHeader.UpdateSequenceArrayCount; i++)
            {
                Int64 sequenceNumberIndex = bufferStart + (Int64)(index * sizeof(UInt16));
                Int64 usaArrayOffset = bufferStart + RecordHeader.UpdateSequenceArrayOffset;

                // Check if we are inside the buffer.
                if (sequenceNumberIndex >= bufferStart + (Int64)BufLength)
                {
                    ShowLogMessage(Resources.ErrorMissingDataUSA);

                    return false;
                }

                UInt16 sectorSequenceNumber = diskBuffer.GetUInt16(sequenceNumberIndex);
                UInt16 sequenceNumber = diskBuffer.GetUInt16(usaArrayOffset);

                Byte[] updateSequenceNumbers = diskBuffer.GetBytes(usaArrayOffset + i * sizeof(UInt16), sizeof(UInt16));

                // Check if the last 2 bytes of the sector contain the Update Sequence Number.
                if (sectorSequenceNumber != sequenceNumber)
                {
                    ShowLogMessage(Resources.ErrorUSAfixupNotEquealToUSN);

                    return false;
                }

                // Replace the last 2 bytes in the sector with the value from the Usa array.
                diskBuffer.Buffer[sequenceNumberIndex + 0] = updateSequenceNumbers[0];
                diskBuffer.Buffer[sequenceNumberIndex + 1] = updateSequenceNumbers[1];

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
            ref FragmentCollection mftDataFragments, ref UInt64 mftDataBytes,
            ref FragmentCollection mftBitmapFragments, ref UInt64 mftBitmapBytes,
            DiskBuffer diskBuffer, UInt64 bufLength)
        {
            //Trace.WriteLine(this, String.Format(
            //    "InterpretMftRecord Inode: {0:G}, Max: {1:G}", inodeNumber, maxInode));

            //Int64 position = reader.BaseStream.Position;
            Int64 position = diskBuffer.ReaderPosition;

            FileRecordHeader fileRecordHeader = diskBuffer.GetFileRecordHeader(position);

            // If the record is not in use then quietly exit
            if (!fileRecordHeader.IsInUse)
            {
                //ShowLogMessage(String.Format("Inode {0:G} is not in use.", inodeNumber));
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
            ErrorCheck(fileRecordHeader.MasterFileTableRecordNumber != inodeNumber,
                String.Format(CultureInfo.CurrentCulture, Resources.WarningInodeContainsDifferentMasterFileTableRecordNumber, inodeNumber, fileRecordHeader.MasterFileTableRecordNumber), true);

            ErrorCheck(fileRecordHeader.AttributeOffset >= bufLength,
                String.Format(CultureInfo.CurrentCulture, Resources.ErrorAttributesOutsideOfFileRecord, inodeNumber), true);

            ErrorCheck(fileRecordHeader.BytesInUse > bufLength,
                String.Format(CultureInfo.CurrentCulture, Resources.ErrorRecordBiggerThanBufferSize, inodeNumber), true);

            InodeDataStructure inodeData = new InodeDataStructure(inodeNumber);

            inodeData.IsDirectory = fileRecordHeader.IsDirectory;
            inodeData.MasterFileTableDataFragments = mftDataFragments;
            inodeData.MasterFileTableDataLength = mftDataBytes;

            // Make sure that directories are always created.
            if (inodeData.IsDirectory)
            {
                AttributeType attributeType = AttributeEnumType.IndexAllocation;

                TranslateRundataToFragmentlist(inodeData, Resources.RootStreamName, attributeType, null, 0, 0);
            }

            // Interpret the attributes.
            diskBuffer.ReaderPosition = position + fileRecordHeader.AttributeOffset;

            Boolean Result = true;

            try
            {
                Result = ProcessAttributes(diskInfo, inodeData, diskBuffer, bufLength - fileRecordHeader.AttributeOffset, UInt16.MaxValue, 0);
            }
            catch (Exception)
            {
                Result = false;

                _countProcessAttributesIssues++;

                Trace.WriteLine(this, String.Format(CultureInfo.CurrentCulture, Resources.ErrorProcessAttributes, inodeData.LongFileName, _countProcessAttributesIssues));
            }

            // Save the MftDataFragments, MftDataBytes, MftBitmapFragments, and MftBitmapBytes.
            if (inodeNumber == 0)
            {
                mftDataFragments =    inodeData.MasterFileTableDataFragments;
                mftDataBytes = inodeData.MasterFileTableDataLength;
                mftBitmapFragments = inodeData.MasterFileTableBitmapFragments;
                mftBitmapBytes = inodeData.MasterFileTableBitmapLength;
            }

            int countFiles = 0;

            // Create an item in the Data->ItemTree for every stream.
            foreach (TDefragLib.FileSystem.Ntfs.Stream stream in inodeData.Streams)
            {
                // Create and fill a new item record in memory.
                ItemStruct Item = new ItemStruct(stream);

                Item.LongFileName = ConstructStreamName(inodeData.LongFileName, inodeData.ShortFileName, stream);
                Item.LongPath = null;

                Item.ShortFileName = ConstructStreamName(inodeData.ShortFileName, inodeData.LongFileName, stream);
                Item.ShortPath = null;

                //Item.Bytes = inodeData.TotalBytes;
                Item.Size = stream.TotalBytes;

                //Item.Clusters = 0;
                Item.CountClusters = stream.Clusters;

                Item.CreationTime = inodeData.CreationTime;
                Item.MasterFileTableChangeTime = inodeData.MasterFileTableChangeTime;
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
                    Lib.Data.CountDirectories++;
                }

                Lib.Data.CountAllFiles++;

                if (stream.Type.IsData)
                {
                    Lib.Data.CountAllBytes += inodeData.TotalBytes;
                }

                Lib.Data.CountAllClusters += stream.Clusters;

                if (Item.FragmentCount > 1)
                {
                    Lib.Data.CountFragmentedItems++;
                    Lib.Data.CountFragmentedBytes += inodeData.TotalBytes;

                    if (stream != null) Lib.Data.CountFragmentedClusters += stream.Clusters;
                }

                // Add the item record to the sorted item tree in memory.
                Lib.AddItemToList(Item);

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
                    InodeLongFilename = InodeItem.LongFileName;
                }

//                if (InodeLongFilename.CompareTo(Item.LongFilename) > 0)
//                {
                    if (Item.IsDirectory == true)
                        inodeArray.SetValue(Item, (Int64)inodeNumber);
//                }

                //if ((Item != null) && (countFiles % 300 == 0))
                //    ShowDebug(2, "File: " + (String.IsNullOrEmpty(Item.LongFilename) ? (String.IsNullOrEmpty(Item.ShortFilename) ? "" : Item.ShortFilename) : Item.LongFilename));
                // ShowLogMessage("File: " + Item.LongFilename ?? Item.ShortFilename ?? "<NoName>");

                countFiles++;

                // Draw the item on the screen.
                //if (Lib.Data.Reparse == false)
                //{
                //    Lib.ColorizeItem(Item, 0, 0, false, Item.Error);
                //}
                //else
                //{
                //    Lib.ParseDiskBitmap();
                //}
            }

            return true;
        }

        /// <summary>
        /// Read the RunData list and translate into a list of fragments. 
        /// </summary>
        /// <param name="inodeData"></param>
        /// <param name="streamName"></param>
        /// <param name="streamType"></param>
        /// <param name="runData"></param>
        /// <param name="runDataLength"></param>
        /// <param name="startingVcn"></param>
        /// <param name="byteCount"></param>
        private Boolean TranslateRundataToFragmentlist(
                    InodeDataStructure inodeData,
                    String streamName,
                    AttributeType streamType,
                    DiskBuffer diskBuffer,
                    UInt64 startingVcn,
                    UInt64 byteCount)
        {
            Boolean Result = true;

            ErrorCheck(inodeData == null, Resources.LogReading, true);

            // Find the stream in the list of streams. If not found then create a new stream.
            TDefragLib.FileSystem.Ntfs.Stream foundStream = inodeData.Streams.FirstOrDefault(x => (x.Name == streamName) && (x.Type.Type == streamType.Type));
            if (foundStream == null)
            {
                //ShowDebug(6, "    Creating new stream: '" + streamName + ":" + streamType.GetStreamTypeName() + "'");
                TDefragLib.FileSystem.Ntfs.Stream newStream = new TDefragLib.FileSystem.Ntfs.Stream(streamName, streamType);
                newStream.TotalBytes = byteCount;

                inodeData.Streams.Add(newStream);
                foundStream = newStream;
            }
            else
            {
                //ShowDebug(6, "    Appending rundata to existing stream: '" + streamName + ":" + streamType.GetStreamTypeName());
                if (foundStream.TotalBytes == 0)
                    foundStream.TotalBytes = byteCount;
            }

            if (diskBuffer == null)
                return false;

            try
            {
                diskBuffer.ParseStreamRunData(foundStream, diskBuffer.ReaderPosition, startingVcn);
            }
            catch (Exception)
            {
                Result = false;

                _countRunDataIssues++;
                Trace.WriteLine(this, String.Format(CultureInfo.CurrentCulture, Resources.LogLcn, foundStream, _countRunDataIssues));
            }

            return Result;
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
            DiskBuffer diskBuffer, UInt64 bufLength,
            UInt16 instance, int depth)
        {
            //Trace.WriteLine(this, String.Format(
            //    "ProcessAttributes Inode: {0:G}, Len: {1:G}", inodeData.Inode, bufLength));

            TDefragLib.FileSystem.Ntfs.Attribute attribute;
            Int64 position = diskBuffer.ReaderPosition;

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


                diskBuffer.ReaderPosition = position + offset;

                if (attribute.IsNonresident)
                {
                    Result = ParseNonResidentAttribute(inodeData, diskBuffer, offset, attribute, position);
                }
                else
                {
                    Result = ParseResidentAttribute(inodeData, diskBuffer, offset, attribute, position);
                }

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

                diskBuffer.ReaderPosition = position + offset;

                if (attribute.IsNonresident)
                {
                    Result = ParseNonResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, attribute, position, offset);
                }
                else
                {
                    Result = ParseResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, position, offset);
                }
                
                if (Result == false)
                {
                    FunctionResult = false;
                }
            }

            return FunctionResult;
        }

        private Boolean ParseNonResidentAttribute(InodeDataStructure inodeData,
            DiskBuffer diskBuffer, UInt32 offset, TDefragLib.FileSystem.Ntfs.Attribute attribute, Int64 position)
        {
            Boolean Result = true;

            //Trace.WriteLine(this, String.Format(
            //    "   ParseNonResidentAttribute Inode: {0:G}, pos: {1:G}", inodeData.Inode, position));
            NonresidentAttribute nonResidentAttribute = diskBuffer.GetNonResidentAttribute(diskBuffer.ReaderPosition);

            // Save the length (number of bytes) of the data.
            if (attribute.Type.IsData && (inodeData.TotalBytes == 0))
            {
                inodeData.TotalBytes = nonResidentAttribute.DataSize;
            }

            // Extract the streamname.
            diskBuffer.ReaderPosition = position + offset + attribute.NameOffset;

            String p1 = diskBuffer.GetString(position + offset + attribute.NameOffset, attribute.NameLength);
            //Trace.WriteLine(this, String.Format("     Stream: {0}", p1));

            // Create a new stream with a list of fragments for this data.
            diskBuffer.ReaderPosition = position + offset + nonResidentAttribute.RunArrayOffset;

            Result = TranslateRundataToFragmentlist(inodeData, p1, attribute.Type,
                diskBuffer, nonResidentAttribute.StartingVirtualClusterNumber, nonResidentAttribute.DataSize);

            // Special case: If this is the $MFT then save data.
            if (inodeData.Inode == 0)
            {
                if (attribute.Type.IsData && (inodeData.MasterFileTableDataFragments == null))
                {
                    inodeData.MasterFileTableDataFragments = inodeData.Streams.First().Fragments;
                    inodeData.MasterFileTableDataLength = nonResidentAttribute.DataSize;
                }

                if (attribute.Type.IsBitmap && (inodeData.MasterFileTableBitmapFragments == null))
                {
                    inodeData.MasterFileTableBitmapFragments = inodeData.Streams.First().Fragments;
                    inodeData.MasterFileTableBitmapLength = nonResidentAttribute.DataSize;
                }
            }

            return Result;
        }

        private Boolean ParseResidentAttribute(InodeDataStructure inodeData,
            DiskBuffer diskBuffer, UInt32 offset, TDefragLib.FileSystem.Ntfs.Attribute attribute, Int64 position)
        {
            Boolean Result = true;

            //Trace.WriteLine(this, String.Format(
            //    "   ParseResidentAttribute Inode: {0:G}, pos: {1:G}", inodeData.Inode, position));

            ResidentAttribute residentAttribute = diskBuffer.GetResidentAttribute(diskBuffer.ReaderPosition);

            Int64 tempOffset = (Int64)(offset + residentAttribute.ValueOffset);

            diskBuffer.ReaderPosition = position + tempOffset;

            FileNameAttribute fileNameAttribute;

            // The AttributeFileName (0x30) contains the filename and the link to the parent directory.
            if (attribute.Type.IsFileName)
            {
                fileNameAttribute = diskBuffer.GetFileNameAttribute(diskBuffer.ReaderPosition);

                //Trace.WriteLine(this, String.Format("     File: {0}", fileNameAttribute.Name));

                inodeData.ParentInode = fileNameAttribute.ParentDirectory.BaseInodeNumber;

                inodeData.AddName(fileNameAttribute);
            }

            //  The AttributeStandardInformation (0x10) contains the m_creationTime,
            //  m_lastAccessTime, the m_mftChangeTime, and the file attributes.
            if (attribute.Type.IsStandardInformation)
            {
                StandardInformation standardInformation = diskBuffer.GetStandardInformation(diskBuffer.ReaderPosition);

                inodeData.CreationTime = standardInformation.CreationTime;
                inodeData.MasterFileTableChangeTime = standardInformation.MftChangeTime;
                inodeData.LastAccessTime = standardInformation.LastAccessTime;
            }

            // The value of the AttributeData (0x80) is the actual data of the file.
            if (attribute.Type.IsData)
            {
                inodeData.TotalBytes = residentAttribute.ValueLength;
            }

            return Result;
        }

        private Boolean ParseResidentAttributesFull(DiskInformation diskInfo,
            InodeDataStructure inodeData, DiskBuffer diskBuffer, int depth,
            Int64 position, UInt32 offset)
        {
            //Trace.WriteLine(this, String.Format(
            //    " ParseResidentAttributesFull Inode: {0:G}, depth: {1:G}", inodeData.Inode, depth));
            ResidentAttribute residentAttribute = diskBuffer.GetResidentAttribute(diskBuffer.ReaderPosition);

            diskBuffer.ReaderPosition = position + offset + residentAttribute.ValueOffset;

            return ProcessAttributeList(diskInfo, inodeData, diskBuffer, residentAttribute.ValueLength, depth);
        }

        private Boolean ParseNonResidentAttributesFull(DiskInformation diskInfo,
            InodeDataStructure inodeData, DiskBuffer diskBuffer, int depth,
            TDefragLib.FileSystem.Ntfs.Attribute attribute, Int64 position, UInt32 offset)
        {
            //Trace.WriteLine(this, String.Format(
            //    " ParseNonResidentAttributesFull Inode: {0:G}, depth: {1:G}", inodeData.Inode, depth));
            NonresidentAttribute nonResidentAttribute = diskBuffer.GetNonResidentAttribute(diskBuffer.ReaderPosition);

            UInt64 Buffer2Length = nonResidentAttribute.DataSize;

            diskBuffer.ReaderPosition = position + offset + nonResidentAttribute.RunArrayOffset;

            DiskBuffer buffer2 = new DiskBuffer(40000);

            buffer2.Buffer = ReadNonResidentData(diskInfo, diskBuffer,
                attribute.Length - nonResidentAttribute.RunArrayOffset, 0, Buffer2Length);

            return ProcessAttributeList(diskInfo, inodeData, buffer2, Buffer2Length, depth);
        }

        /// <summary>
        /// Process a list of attributes and store the gathered information in the Item
        /// struct. Return FALSE if an error occurred.
        /// </summary>
        /// <param name="diskInfo"></param>
        /// <param name="inodeData"></param>
        /// <param name="reader"></param>
        /// <param name="bufLength"></param>
        /// <param name="depth"></param>
        Boolean ProcessAttributeList(
                DiskInformation diskInfo, InodeDataStructure inodeData,
                DiskBuffer diskBuffer, UInt64 bufLength, int depth)
        {
            Boolean Result = true;

            //Trace.WriteLine(this, String.Format(
            //    "Processing AttributeList for Inode {0:G}, {1:G} bytes", inodeData.Inode, bufLength));

            Debug.Assert(inodeData.MasterFileTableDataFragments != null);

            Int64 position = diskBuffer.ReaderPosition;

            DiskBuffer buffer2 = new DiskBuffer(diskInfo.BytesPerMasterFileTableRecord);

            FileRecordHeader FileRecordHeader;

            UInt64 BaseInode;

            ErrorCheck((diskBuffer == null) || (bufLength == 0), "    Reading {0:G} bytes from offset {0:G}", true);
            ErrorCheck((depth > 1000), "Error: infinite attribute loop", false);

            AttributeList attributeList = null;

            // Walk through all the attributes and gather information.
            //
            for (Int64 offset = 0; offset < (Int64)bufLength; offset += attributeList.Length)
            {
                diskBuffer.ReaderPosition = position + offset;

                attributeList = diskBuffer.GetAttributeList(position + offset);

                // Exit if no more attributes. AttributeLists are usually not closed by the 0xFFFFFFFF endmarker.
                // Reaching the end of the buffer is therefore normal and not an error.

                if (offset + 3 > (Int64)bufLength) break;
                if (attributeList.Type.IsEndOfList) break;
                if (attributeList.Length < 3) break;
                if (offset + attributeList.Length > (Int64)bufLength) break;

                // Extract the referenced m_iNode. If it's the same as the calling m_iNode then 
                // ignore (if we don't ignore then the program will loop forever, because for 
                // some reason the info in the calling m_iNode is duplicated here...).
                //
                UInt64 RefInode = attributeList.FileReferenceNumber.BaseInodeNumber;
                //(UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberLowPart +
                //    ((UInt64)attributeList.m_fileReferenceNumber.m_iNodeNumberHighPart << 32);

                if (RefInode == inodeData.Inode) continue;

                // Show debug message.
                //ShowDebug(6, "    List attribute: " + attributeList.Type.GetStreamTypeName());
                //ShowDebug(6, String.Format("      m_lowestVcn = {0:G}, RefInode = {1:G}, InodeSequence = {2:G}, m_instance = {3:G}",
                //      attributeList.m_lowestVcn, RefInode, attributeList.m_fileReferenceNumber.m_sequenceNumber, attributeList.m_instance));

                // Extract the streamname. I don't know why AttributeLists can have names, and
                // the name is not used further down. It is only extracted for debugging purposes.

                if (attributeList.NameLength > 0)
                {
                    diskBuffer.ReaderPosition = position + offset + attributeList.NameOffset;

                    //String p1 = diskBuffer.GetString(position + offset + attributeList.NameOffset, attributeList.NameLength);
                    //String p1 = Helper.ParseString(reader, attributeList.NameLength);
                    //ShowDebug(6, "      AttributeList name = '" + p1 + "'");
                }

                // Find the fragment in the MFT that contains the referenced m_iNode.

                Fragment foundFragment = inodeData.MasterFileTableDataFragments.FindContaining(
                    diskInfo.InodeToCluster(RefInode));

                if (foundFragment == null)
                {
                    continue;
                }

                // Fetch the record of the referenced m_iNode from disk.
                UInt64 tempVcn = diskInfo.ClusterToBytes(foundFragment.LogicalClusterNumber) + diskInfo.InodeToBytes(RefInode);

                Lib.Data.Volume.ReadFromCluster(tempVcn, buffer2.Buffer, 0,
                    (Int32)diskInfo.BytesPerMasterFileTableRecord);

                UpdateSequenceNumbers(diskInfo, buffer2, 0, diskInfo.BytesPerMasterFileTableRecord);

                // If the Inode is not in use then skip.
                FileRecordHeader = buffer2.GetFileRecordHeader(buffer2.ReaderPosition);

                if (!FileRecordHeader.IsInUse)
                {
                    ShowLogMessage(String.Format("      Referenced m_iNode {0:G} is not in use.", RefInode));
                    continue;
                }

                // If the BaseInode inside the m_iNode is not the same as the calling m_iNode then skip.
                BaseInode = FileRecordHeader.BaseFileRecord.BaseInodeNumber;

                if (inodeData.Inode != BaseInode)
                {
                    ShowLogMessage(String.Format("      Warning: m_iNode {0:G} is an extension of m_iNode {1:G}, but thinks it's an extension of m_iNode {2:G}.",
                            RefInode, inodeData.Inode, BaseInode));
                    continue;
                }

                // Process the list of attributes in the m_iNode, by recursively calling the ProcessAttributes() subroutine.
                //ShowDebug(6, String.Format("      Processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.Instance));

                Boolean Res = ProcessAttributes(diskInfo, inodeData,
                    buffer2,
                    diskInfo.BytesPerMasterFileTableRecord - FileRecordHeader.AttributeOffset,
                    attributeList.Instance, depth + 1);

                if (Res == false)
                    Result = false;

                //ShowDebug(6, String.Format("      Finished processing m_iNode {0:G} m_instance {1:G}", RefInode, attributeList.Instance));
            }

            return Result;
        }

        /// <summary>
        /// Read the data that is specified in a RunData list from disk into memory,
        /// skipping the first Offset bytes.
        /// Return a buffer with the data, or null if error.
        /// </summary>
        /// <param name="diskInfo"></param>
        /// <param name="runData"></param>
        /// <param name="runDataLength"></param>
        /// <param name="offset">Bytes to skip from begin of data.</param>
        /// <param name="wantedLength">Number of bytes to read.</param>
        /// <returns></returns>
        Byte[] ReadNonResidentData(
                    DiskInformation diskInfo,
                    DiskBuffer diskBuffer,
                    UInt64 runDataLength,
                    UInt64 offset,
                    UInt64 wantedLength)
        {
            //Trace.WriteLine(this, String.Format(
            //    "ReadNonResidentData {0:G}, {1:G} bytes", offset, runDataLength));

            //ShowLogMessage(Resources.ErrorUSAfixupNotEquealToUSN);

            ErrorCheck((diskBuffer == null) || (runDataLength == 0), Resources.LogReading, true);

            // We have to round up the WantedLength to the nearest sector.
            // For some reason or other Microsoft has decided that raw reading from disk can
            // only be done by whole sector, even though ReadFile() accepts its parameters in bytes.

            if (wantedLength % diskInfo.BytesPerSector > 0)
            {
                wantedLength += diskInfo.BytesPerSector - wantedLength % diskInfo.BytesPerSector;
            }

            if (wantedLength >= UInt32.MaxValue)
            {
                ShowLogMessage(Resources.ErrorSanityCheck);

                return null;
            }

            DiskBuffer buffer = new DiskBuffer(wantedLength);

            // Walk through the RunData and read the requested data from disk.

            Int64 Lcn = 0;
            UInt64 Vcn = 0;

            UInt64 runLength;
            Int64 runOffset;

            while (diskBuffer.ParseRunData(diskBuffer.ReaderPosition, out runLength, out runOffset))
            {
                Lcn += runOffset;

                // Ignore virtual extents.

                if (runOffset == 0)
                    continue;

                // I don't think the RunLength can ever be zero, but just in case.

                if (runLength == 0)
                    continue;

                // Determine how many and which bytes we want to read.
                // If we don't need any bytes from this extent then loop.

                UInt64 ExtentVcn = Vcn * diskInfo.BytesPerCluster;
                UInt64 ExtentLcn = (UInt64)((UInt64)Lcn * diskInfo.BytesPerCluster);

                UInt64 ExtentLength = runLength * diskInfo.BytesPerCluster;

                if (offset >= ExtentVcn + ExtentLength) continue;

                if (offset > ExtentVcn)
                {
                    ExtentLcn = ExtentLcn + offset - ExtentVcn;
                    ExtentLength = ExtentLength - (offset - ExtentVcn);
                    ExtentVcn = offset;
                }

                if (offset + wantedLength <= ExtentVcn) continue;

                if (offset + wantedLength < ExtentVcn + ExtentLength)
                {
                    ExtentLength = offset + wantedLength - ExtentVcn;
                }

                if (ExtentLength == 0) continue;

                // Read the data from the disk. If error then return FALSE.

                //ShowLogMessage(String.Format("    Cannot read {0:G} bytes, maximum is {1:G}.",
                //    ExtentLength, ExtentLcn / diskInfo.BytesPerCluster,
                //    ExtentVcn - offset));

                Lib.Data.Volume.ReadFromCluster(ExtentLcn, buffer.Buffer,
                    (Int32)(ExtentVcn - offset), (Int32)ExtentLength);

                Vcn += runLength;
            }

            return (buffer.Buffer);
        }

        /// <summary>
        /// Construct the full stream name from the filename, the stream name, and the stream type.
        /// </summary>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <param name="thisStream"></param>
        /// <returns></returns>
        private static String ConstructStreamName(String fileName1, String fileName2, TDefragLib.FileSystem.Ntfs.Stream thisStream)
        {
            String fileName = fileName1 ?? fileName2;

            String streamName = null;
            AttributeType type = new AttributeType();

            if (thisStream != null)
            {
                streamName = thisStream.Name;
                type = thisStream.Type;
            }

            // If the StreamName is empty and the StreamType is Data then return only the
            // FileName. The Data stream is the default stream of regular files.
            //
            if ((String.IsNullOrEmpty(streamName)) && type.IsData)
            {
                return fileName;
            }

            // If the StreamName is "$I30" and the StreamType is AttributeIndexAllocation then
            // return only the FileName. This must be a directory, and the Microsoft 
            // defragmentation API will automatically select this stream.
            //
            if ((streamName == "$I30") && type.IsIndexAllocation)
            {
                return fileName;
            }

            //  If the StreamName is empty and the StreamType is Data then return only the
            //  FileName. The Data stream is the default stream of regular files.
            if (String.IsNullOrEmpty(streamName) &&
                String.IsNullOrEmpty(type.StreamName))
            {
                return fileName;
            }

            Int32 Length = 3;

            if (fileName != null)
                Length += fileName.Length;

            if (streamName != null)
                Length += streamName.Length;

            Length += type.StreamName.Length;

            if (Length == 3) return (null);

            StringBuilder p1 = new StringBuilder();

            if (!String.IsNullOrEmpty(fileName))
                p1.Append(fileName);

            p1.Append(":");

            if (!String.IsNullOrEmpty(streamName))
                p1.Append(streamName);

            p1.Append(":");

            p1.Append(type.StreamName);

            return p1.ToString();
        }

        private MainLib Lib;

        private Int32 _countProcessAttributesIssues = 0;    // 855
        private Int32 _countRunDataIssues = 0;              // 479
    }

}
