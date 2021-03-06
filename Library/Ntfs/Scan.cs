﻿using System;
using System.Collections.Generic;
using System.Linq;
using TDefragLib.FS.Ntfs;
using TDefragLib.Helper;
using TDefragLib.FileSystem.Ntfs;
using System.Diagnostics;
using TDefragWpf.Properties;
using System.Globalization;
using TDefragWpf.Library.FS.Ntfs;

namespace TDefragLib.Ntfs
{
    class Scan
    {
        const UInt64 MFTBUFFERSIZE = 256 * 1024;

        /// <summary>
        /// Scan
        /// </summary>
        /// <param name="parent"></param>
        public Scan(MainLibrary parent)
        {
            MainLibraryClass = parent;
        }

        /// <summary>
        /// ShowLogMessage
        /// </summary>
        /// <param name="message"></param>
        public void ShowLogMessage(String message)
        {
            MainLibraryClass.ShowMessage(message);
        }

        /// <summary>
        /// UpdateProgress
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateProgress(Double progress)
        {
            MainLibraryClass.UpdateProgress(progress);
        }

        /// <summary>
        /// AnalyzeVolume
        /// </summary>
        public void AnalyzeVolume()
        {
            // Read the boot block from the disk.
            FS.IBootSector bootSector = MainLibraryClass.Data.Volume.BootSector;

            if (bootSector == null)
            {
                ShowLogMessage(Resources.ErrorCouldNotLoadBootSector);
                return;
            }

            // Test if the boot block is an NTFS boot block.
            if (bootSector.Filesystem != FS.FileSystemType.Ntfs)
            {
                ShowLogMessage(Resources.ErrorNotNtfsDisk);
                return;
            }

            DiskInformation diskInfo = new DiskInformation(bootSector);

            MainLibraryClass.Data.BytesPerCluster = diskInfo.BytesPerCluster;
            MainLibraryClass.Data.NumberOfClusters = diskInfo.NumberOfSectors;

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
            MainLibraryClass.Data.MasterFileTableLockedClusters = diskInfo.BytesPerCluster / diskInfo.BytesPerMasterFileTableRecord;

            // Read the $MFT record from disk into memory, which is always the first record in the MFT.
            UInt64 tempLcn = diskInfo.MasterFileTableStartLogicalClusterNumber * diskInfo.BytesPerCluster;

            DiskBuffer diskBuffer = new DiskBuffer((Int64)MFTBUFFERSIZE);

            // read MFT record
            Boolean result = MainLibraryClass.Data.Volume.ReadFromCluster(tempLcn, diskBuffer.Buffer, 0, (Int32)diskInfo.BytesPerMasterFileTableRecord);

            if (result == false)
            {
                ShowLogMessage(Resources.ErrorCouldNotReadBuffer);
                return;
            }

            // Update sequence numbers in all sectors
            UpdateSequenceNumbers(diskInfo, diskBuffer, 0, diskInfo.BytesPerMasterFileTableRecord);

            // Extract data from the MFT record and put into an Item struct in memory.
            // If there was an error then exit.

            diskBuffer.ReaderPosition = 0;

            Boolean Result = InterpretMftRecord(diskInfo, null, 0, 0,
                diskBuffer, diskInfo.BytesPerMasterFileTableRecord);

            //ShowLogMessage(String.Format("MftDataBytes = {0:G}, MftBitmapBytes = {0:G}", MftDataBytes, MftBitmapBytes));

            BitmapFile bitmapFile = new BitmapFile(MainLibraryClass.Data.Volume,
                diskInfo, MftBitmapFragments, MftBitmapBytes, MftDataBytes);

            UInt64 MaxInode = bitmapFile.MaxInode;

            ItemStruct[] InodeArray = new ItemStruct[MaxInode];
            //InodeArray[0] = _lib.Data.ItemTree;
            //ItemStruct Item = null;

            MainLibraryClass.Data.TasksCompleted = 0;
            MainLibraryClass.Data.TasksCount = 0;

            DateTime startTime = DateTime.Now;

            MainLibraryClass.Data.TasksCount = bitmapFile.UsedInodes;

            // Read and process all the records in the MFT. The records are read into a
            // buffer and then given one by one to the InterpretMftRecord() subroutine.

            UInt64 BlockStart = 0;
            UInt64 BlockEnd = 0;
            UInt64 InodeNumber = 0;

            UInt64 MftBitmapSize = MftBitmapBytes * 8;

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
                MainLibraryClass.Data.TasksCompleted++;

                if (MainLibraryClass.Data.TasksCompleted >= MainLibraryClass.Data.TasksCount)
                    break;

                // Read a block of inode's into memory
                if (InodeNumber >= BlockEnd)
                {
                    BlockStart = InodeNumber;
                    BlockEnd = BlockStart + diskInfo.BytesToInode(MFTBUFFERSIZE);

                    if (BlockEnd > MftBitmapSize)
                        BlockEnd = MftBitmapSize;

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

                    MainLibraryClass.Data.Volume.ReadFromCluster(lcn, diskBuffer.Buffer, 0, (Int32)diskInfo.InodeToBytes(BlockEnd - BlockStart));
                }

                // Fixup the raw data of this m_iNode
                UInt64 position = diskInfo.InodeToBytes(InodeNumber - BlockStart);

                UpdateSequenceNumbers(diskInfo, diskBuffer, (Int64)position, diskInfo.BytesPerMasterFileTableRecord);

                diskBuffer.ReaderPosition = (Int64)diskInfo.InodeToBytes(InodeNumber - BlockStart);

                // Interpret the m_iNode's attributes.
                Result = InterpretMftRecord(diskInfo, InodeArray, InodeNumber, MaxInode,
                        diskBuffer, diskInfo.BytesPerMasterFileTableRecord);

                if (MainLibraryClass.Data.TasksCompleted % 500 == 0)
                {
                    UpdateProgress((Double)((Double)MainLibraryClass.Data.TasksCompleted / (Double)MainLibraryClass.Data.TasksCount * 100));
                }

                InodeNumber++;
            }

            DateTime endTime = DateTime.Now;

            if (endTime > startTime)
            {
                ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.InfoAnalysisSpeed, (Int64)MaxInode * 1000 / (endTime - startTime).TotalMilliseconds));
            }

            using (MainLibraryClass.Data.Volume)
            {
                //if (Lib.Data.Running != RunningState.Running)
                //{
                //    Lib.itemList = null;
                //    return false;
                //}

                // Setup the ParentDirectory in all the items with the info in the InodeArray.
                foreach (ItemStruct item in MainLibraryClass.ItemCollection)
                {
                    item.ParentDirectory = (ItemStruct)InodeArray.GetValue((Int64)item.ParentIndexNode);

                    if (item.ParentIndexNode == 5)
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
        /// <param name="reader"></param>
        /// <param name="bufLength"></param>
        /// <returns></returns>
        Boolean InterpretMftRecord(
            DiskInformation diskInfo, Array inodeArray,
            UInt64 inodeNumber, UInt64 maxInode,
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
            if (fileRecordHeader.MasterFileTableRecordNumber != inodeNumber)
            {
                ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.WarningInodeContainsDifferentMasterFileTableRecordNumber, inodeNumber, fileRecordHeader.MasterFileTableRecordNumber));
            }

            if (fileRecordHeader.AttributeOffset >= bufLength)
            {
                ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.ErrorAttributesOutsideOfFileRecord, inodeNumber));
            }

            if (fileRecordHeader.BytesInUse > bufLength)
            {
                ShowLogMessage(String.Format(CultureInfo.CurrentCulture, Resources.ErrorRecordBiggerThanBufferSize, inodeNumber));
            }

            InodeDataStructure inodeData = new InodeDataStructure(inodeNumber);

            inodeData.IsDirectory = fileRecordHeader.IsDirectory;
            inodeData.MasterFileTableDataFragments = MftDataFragments;
            inodeData.MasterFileTableDataLength = MftDataBytes;

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
                MftDataFragments = inodeData.MasterFileTableDataFragments;
                MftDataBytes = inodeData.MasterFileTableDataLength;
                MftBitmapFragments = inodeData.MasterFileTableBitmapFragments;
                MftBitmapBytes = inodeData.MasterFileTableBitmapLength;
            }

            int countFiles = 0;

            // Create an item in the Data->ItemTree for every stream.
            foreach (TDefragLib.FileSystem.Ntfs.Stream stream in inodeData.Streams)
            {
                // Create and fill a new item record in memory.
                ItemStruct Item = new ItemStruct(stream);

                Item.LongFileName = stream.ConstructStreamName(inodeData.LongFileName, inodeData.ShortFileName);
                Item.LongPath = null;

                Item.ShortFileName = stream.ConstructStreamName(inodeData.ShortFileName, inodeData.LongFileName);
                Item.ShortPath = null;

                //Item.Bytes = inodeData.TotalBytes;
                Item.Size = stream.TotalBytes;

                //Item.Clusters = 0;
                Item.CountClusters = stream.NumClusters;

                Item.CreationTime = inodeData.CreationTime;
                Item.MasterFileTableChangeTime = inodeData.MasterFileTableChangeTime;
                Item.LastAccessTime = inodeData.LastAccessTime;

                Item.ParentIndexNode = inodeData.ParentInode;
                Item.IsDirectory = inodeData.IsDirectory;
                Item.Unmovable = false;
                Item.Exclude = false;
                Item.SpaceHog = false;
                Item.Error = !Result;

                // Increment counters
                if (Item.IsDirectory)
                {
                    MainLibraryClass.Data.CountDirectories++;
                }

                MainLibraryClass.Data.CountAllFiles++;

                if (stream.Type.IsData)
                {
                    MainLibraryClass.Data.CountAllBytes += inodeData.TotalBytes;
                }

                MainLibraryClass.Data.CountAllClusters += stream.NumClusters;

                if (Item.FragmentCount > 1)
                {
                    MainLibraryClass.Data.CountFragmentedItems++;
                    MainLibraryClass.Data.CountFragmentedBytes += inodeData.TotalBytes;

                    if (stream != null) MainLibraryClass.Data.CountFragmentedClusters += stream.NumClusters;
                }

                // Add the item record to the sorted item tree in memory.
                MainLibraryClass.AddItemToList(Item);

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

                if (Item.IsDirectory == true)
                {
                    inodeArray.SetValue(Item, (Int64)inodeNumber);
                }

                //if ((Item != null) && (countFiles % 300 == 0))
                //    ShowDebug(2, "File: " + (String.IsNullOrEmpty(Item.LongFilename) ? (String.IsNullOrEmpty(Item.ShortFilename) ? "" : Item.ShortFilename) : Item.LongFilename));
                // ShowLogMessage("File: " + Item.LongFilename ?? Item.ShortFilename ?? "<NoName>");

                countFiles++;

                // Draw the item on the screen.
                //if (Lib.Data.Reparse == false)
                //{
                    MainLibraryClass.ColorizeItem(Item, 0, 0, false, Item.Error);
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

            if (inodeData == null)
            {
                ShowLogMessage(Resources.LogReading);
            }

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

        #region Attribute Enum Types

        public static List<AttributeEnumTypeEntry> attributeEnumTypesList { get; set; }

        public static List<AttributeEnumTypeEntry> AttributeEnumTypesList
        {
            get
            {
                if (attributeEnumTypesList != null)
                    return attributeEnumTypesList;

                attributeEnumTypesList = new List<AttributeEnumTypeEntry>();

                // the attribute type code may contain a special value -1 (or 0xFFFFFFFF) which 
                // may be present as a filler to mark the end of an attribute list. In that case,
                // the rest of the attribute should be ignored, and the attribute list should not
                // be scanned further.

                // http://msdn.microsoft.com/en-us/library/bb470038%28VS.85%29.aspx
                // It is a DWORD containing enumerated values

                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xFFFFFFFF, AttributeEnumType.EndOfList, String.Empty));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x00, AttributeEnumType.Invalid, String.Empty));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x10, AttributeEnumType.StandardInformation, "$STANDARD_INFORMATION"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x20, AttributeEnumType.AttributeList, "$ATTRIBUTE_LIST"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x30, AttributeEnumType.FileName, "$FILE_NAME"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x40, AttributeEnumType.ObjectId, "$OBJECT_ID"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x50, AttributeEnumType.SecurityDescriptor, "$SECURITY_DESCRIPTOR"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x60, AttributeEnumType.VolumeName, "$VOLUME_NAME"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x70, AttributeEnumType.VolumeInformation, "$VOLUME_INFORMATION"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x80, AttributeEnumType.Data, "$DATA"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x90, AttributeEnumType.IndexRoot, "$INDEX_ROOT"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xA0, AttributeEnumType.IndexAllocation, "$INDEX_ALLOCATION"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xB0, AttributeEnumType.Bitmap, "$BITMAP"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xC0, AttributeEnumType.ReparsePoint, "$REPARSE_POINT"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xD0, AttributeEnumType.EAInformation, "$EA_INFORMATION"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xE0, AttributeEnumType.EA, "$EA"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xF0, AttributeEnumType.PropertySet, "$PROPERTY_SET"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0x100, AttributeEnumType.LoggedUtilityStream, "$LOGGED_UTILITY_STREAM"));
                attributeEnumTypesList.Add(new AttributeEnumTypeEntry(0xFF, AttributeEnumType.All, String.Empty));

                return attributeEnumTypesList;
            }
        }

        public static Dictionary<UInt32, Int32> attributeEnumTypesListMapping { get; set; }

        public static Dictionary<UInt32, Int32> AttributeEnumTypesListMapping
        {
            get
            {
                if (attributeEnumTypesListMapping != null)
                    return attributeEnumTypesListMapping;

                attributeEnumTypesListMapping = new Dictionary<UInt32, Int32>();

                // the attribute type code may contain a special value -1 (or 0xFFFFFFFF) which 
                // may be present as a filler to mark the end of an attribute list. In that case,
                // the rest of the attribute should be ignored, and the attribute list should not
                // be scanned further.

                // http://msdn.microsoft.com/en-us/library/bb470038%28VS.85%29.aspx
                // It is a DWORD containing enumerated values

                attributeEnumTypesListMapping.Add(0xFFFFFFFF, 0);
                attributeEnumTypesListMapping.Add(0x00, 1);
                attributeEnumTypesListMapping.Add(0x10, 2);
                attributeEnumTypesListMapping.Add(0x20, 3);
                attributeEnumTypesListMapping.Add(0x30, 4);
                attributeEnumTypesListMapping.Add(0x40, 5);
                attributeEnumTypesListMapping.Add(0x50, 6);
                attributeEnumTypesListMapping.Add(0x60, 7);
                attributeEnumTypesListMapping.Add(0x70, 8);
                attributeEnumTypesListMapping.Add(0x80, 9);
                attributeEnumTypesListMapping.Add(0x90, 10);
                attributeEnumTypesListMapping.Add(0xA0, 11);
                attributeEnumTypesListMapping.Add(0xB0, 12);
                attributeEnumTypesListMapping.Add(0xC0, 13);
                attributeEnumTypesListMapping.Add(0xD0, 14);
                attributeEnumTypesListMapping.Add(0xE0, 15);
                attributeEnumTypesListMapping.Add(0xF0, 16);
                attributeEnumTypesListMapping.Add(0x100, 17);
                attributeEnumTypesListMapping.Add(0xFF, 18);

                return attributeEnumTypesListMapping;
            }
        }

        public static AttributeEnumTypeEntry GetAttributeEnumEntry(UInt32 typeCode)
        {
            //AttributeEnumTypeEntry entry = AttributeEnumTypesList.Select(a => a).Where(a => a.TypeCode == typeCode).First();

            AttributeEnumTypeEntry entry = AttributeEnumTypesList[AttributeEnumTypesListMapping[typeCode]];

            return entry;
        }

        #endregion

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

                AttributeEnumTypeEntry entry = GetAttributeEnumEntry(attribute.AttributeType.TypeCode);

                attribute.AttributeType.Type = entry.Type;
                attribute.AttributeType.StreamName = entry.StreamName;

                if (attribute.AttributeType.IsEndOfList) break;

                // Exit the loop if end-marker.
                if ((offset + 4 <= bufLength) && attribute.AttributeType.IsInvalid) break;

                if ((offset + 4 > bufLength) || (attribute.Length < 3) || (offset + attribute.Length > bufLength))
                    ShowLogMessage(String.Format("Error: attribute in m_iNode {0:G} is bigger than the data, the MFT may be corrupt.", inodeData.Inode));

                // Skip AttributeList's for now.
                //if (attribute.AttributeType.IsAttributeList) continue;

                // If the Instance does not equal the m_attributeNumber then ignore the attribute.
                // This is used when an AttributeList is being processed and we only want a specific instance.
                if ((instance != UInt16.MaxValue) && (instance != attribute.Number) && (!attribute.AttributeType.IsAttributeList)) continue;


                diskBuffer.ReaderPosition = position + offset;

                if (!attribute.AttributeType.IsAttributeList)
                {
                    if (attribute.IsNonresident)
                        Result = ParseNonResidentAttribute(inodeData, diskBuffer, offset, attribute, position);
                    else
                        Result = ParseResidentAttribute(inodeData, diskBuffer, offset, attribute, position);
                }
                else
                {
                    if (attribute.IsNonresident)
                        Result = ParseNonResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, attribute, position, offset);
                    else
                        Result = ParseResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, position, offset);
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
            //for (UInt32 offset = 0; offset < bufLength; offset += attribute.Length)
            //{
            //    attribute = diskBuffer.GetAttribute(position + offset);

            //    AttributeEnumTypeEntry entry = GetAttributeEnumEntry(attribute.AttributeType.TypeCode);

            //    attribute.AttributeType.Type = entry.Type;
            //    attribute.AttributeType.StreamName = entry.StreamName;

            //    if (attribute.AttributeType.IsEndOfList || attribute.AttributeType.IsInvalid)
            //        break;

            //    if (!attribute.AttributeType.IsAttributeList)
            //        continue;

            //    //ShowDebug(6, String.Format("  Attribute {0:G}: {1:G}", attribute.Number, attribute.Type.GetStreamTypeName()));

            //    diskBuffer.ReaderPosition = position + offset;

            //    if (attribute.IsNonresident)
            //        Result = ParseNonResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, attribute, position, offset);
            //    else
            //        Result = ParseResidentAttributesFull(diskInfo, inodeData, diskBuffer, depth, position, offset);
                
            //    if (Result == false)
            //        FunctionResult = false;
            //}

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
            if (attribute.AttributeType.IsData && (inodeData.TotalBytes == 0))
            {
                inodeData.TotalBytes = nonResidentAttribute.DataSize;
            }

            // Extract the streamname.
            diskBuffer.ReaderPosition = position + offset + attribute.NameOffset;

            String p1 = diskBuffer.GetString(position + offset + attribute.NameOffset, attribute.NameLength);
            //Trace.WriteLine(this, String.Format("     Stream: {0}", p1));

            // Create a new stream with a list of fragments for this data.
            diskBuffer.ReaderPosition = position + offset + nonResidentAttribute.RunArrayOffset;

            Result = TranslateRundataToFragmentlist(inodeData, p1, attribute.AttributeType,
                diskBuffer, nonResidentAttribute.StartingVirtualClusterNumber, nonResidentAttribute.DataSize);

            // Special case: If this is the $MFT then save data.
            if (inodeData.Inode == 0)
            {
                if (attribute.AttributeType.IsData && (inodeData.MasterFileTableDataFragments == null))
                {
                    inodeData.MasterFileTableDataFragments = inodeData.Streams.First().Fragments;
                    inodeData.MasterFileTableDataLength = nonResidentAttribute.DataSize;
                }

                if (attribute.AttributeType.IsBitmap && (inodeData.MasterFileTableBitmapFragments == null))
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
            if (attribute.AttributeType.IsFileName)
            {
                fileNameAttribute = diskBuffer.GetFileNameAttribute(diskBuffer.ReaderPosition);

                //Trace.WriteLine(this, String.Format("     File: {0}", fileNameAttribute.Name));

                inodeData.ParentInode = fileNameAttribute.ParentDirectory.BaseInodeNumber;

                inodeData.AddName(fileNameAttribute);
            }

            //  The AttributeStandardInformation (0x10) contains the m_creationTime,
            //  m_lastAccessTime, the m_mftChangeTime, and the file attributes.
            if (attribute.AttributeType.IsStandardInformation)
            {
                StandardInformation standardInformation = diskBuffer.GetStandardInformation(diskBuffer.ReaderPosition);

                inodeData.CreationTime = standardInformation.CreationTime;
                inodeData.MasterFileTableChangeTime = standardInformation.MftChangeTime;
                inodeData.LastAccessTime = standardInformation.LastAccessTime;
            }

            // The value of the AttributeData (0x80) is the actual data of the file.
            if (attribute.AttributeType.IsData)
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

            DiskBuffer buffer2 = new DiskBuffer(Buffer2Length); // 40000

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

            if ((diskBuffer == null) || (bufLength == 0))
            {
                ShowLogMessage("    Reading {0:G} bytes from offset {0:G}");
            }

            if (depth > 1000)
            {
                ShowLogMessage("Error: infinite attribute loop");
            }

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
                if (attributeList.AttributeType.IsEndOfList) break;
                if (attributeList.Length < 3) break;
                if (offset + attributeList.Length > (Int64)bufLength) break;

                // Extract the referenced m_iNode. If it's the same as the calling m_iNode then 
                // ignore (if we don't ignore then the program will loop forever, because for 
                // some reason the info in the calling m_iNode is duplicated here...).
                //
                UInt64 RefInode = attributeList.FileReferenceNumber.BaseInodeNumber;

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
                    continue;

                // Fetch the record of the referenced m_iNode from disk.
                UInt64 tempVcn = diskInfo.ClusterToBytes(foundFragment.LogicalClusterNumber) + diskInfo.InodeToBytes(RefInode);

                MainLibraryClass.Data.Volume.ReadFromCluster(tempVcn, buffer2.Buffer, 0,
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

            if ((diskBuffer == null) || (runDataLength == 0))
            {
                ShowLogMessage(Resources.LogReading);
            }

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

                MainLibraryClass.Data.Volume.ReadFromCluster(ExtentLcn, buffer.Buffer,
                    (Int32)(ExtentVcn - offset), (Int32)ExtentLength);

                Vcn += runLength;
            }

            return (buffer.Buffer);
        }

        FragmentCollection MftDataFragments { get; set; }

        FragmentCollection MftBitmapFragments { get; set; }

        UInt64 MftDataBytes { get; set; }

        UInt64 MftBitmapBytes { get; set; }

        private MainLibrary MainLibraryClass;

        private Int32 _countProcessAttributesIssues = 0;    // 855
        private Int32 _countRunDataIssues = 0;              // 479
    }

}
