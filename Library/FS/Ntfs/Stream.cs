using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Class describing Stream
    /// 
    /// The NTFS scanner will construct an ItemStruct list in memory, but needs some
    /// extra information while constructing it. The following structs wrap the ItemStruct
    /// into a new struct with some extra info, discarded when the ItemStruct list is
    /// ready.
    /// 
    /// A single Inode can contain multiple streams of data. Every stream has it's own
    /// list of fragments. The name of a stream is the same as the filename plus two
    /// extensions separated by colons:
    /// 
    /// filename:"stream name":"stream type"
    /// 
    /// For example:
    ///   myfile.dat:stream1:$DATA
    ///   
    /// The "stream name" is an empty string for the default stream, which is the data
    /// of regular files. The "stream type" is one of the following strings:
    ///    0x10      $STANDARD_INFORMATION
    ///    0x20      $ATTRIBUTE_LIST
    ///    0x30      $FILE_NAME
    ///    0x40  NT  $VOLUME_VERSION
    ///    0x40  2K  $OBJECT_ID
    ///    0x50      $SECURITY_DESCRIPTOR
    ///    0x60      $VOLUME_NAME
    ///    0x70      $VOLUME_INFORMATION
    ///    0x80      $DATA
    ///    0x90      $INDEX_ROOT
    ///    0xA0      $INDEX_ALLOCATION
    ///    0xB0      $BITMAP
    ///    0xC0  NT  $SYMBOLIC_LINK
    ///    0xC0  2K  $REPARSE_POINT
    ///    0xD0      $EA_INFORMATION
    ///    0xE0      $EA
    ///    0xF0  NT  $PROPERTY_SET
    ///   0x100  2K  $LOGGED_UTILITY_STREAM
    /// </summary>
    [DebuggerDisplay("'{Name}':{Type},{Bytes}b")]
    public class Stream
    {
        public Stream(String name, AttributeType type)
        {
            Name = name;
            Type = type;
            Fragments = new FragmentCollection();
            NumClusters = 0;
        }

        public override string ToString()
        {
            return String.Format("Stream:{0} [{1}]", Name, Type);
        }

        public void ParseRunData(BinaryReader reader, UInt64 startingVirtualClusterNumber)
        {
            if (reader == null)
                return;

            // Walk through the RunData and add the extents.
            Int64 LogicalClusterNumber = 0;
            UInt64 VirtualClusterNumber = startingVirtualClusterNumber;
            UInt64 runLength = 0;
            Int64 runOffset = 0;
            
            while (RunData.Parse(reader, out runLength, out runOffset))
            {
                // the offset is relative to the starting cluster of the previous run
                LogicalClusterNumber += runOffset;

                if (runOffset != 0)
                {
                    NumClusters += runLength;
                }

                Fragments.Add(LogicalClusterNumber, VirtualClusterNumber, runLength, runOffset == 0);
                
                VirtualClusterNumber += runLength;
            }
        }

        /// <summary>
        /// Construct the full stream name from the filename, the stream name, and the stream type.
        /// </summary>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <param name="thisStream"></param>
        /// <returns></returns>
        public String ConstructStreamName(String fileName1, String fileName2)
        {
            String fileName = fileName1 ?? fileName2;

            String streamName = Name;

            // If the StreamName is empty and the StreamType is Data then return only the
            // FileName. The Data stream is the default stream of regular files.
            //
            if ((String.IsNullOrEmpty(streamName)) && Type.IsData)
                return fileName;

            // If the StreamName is "$I30" and the StreamType is AttributeIndexAllocation then
            // return only the FileName. This must be a directory, and the Microsoft 
            // defragmentation API will automatically select this stream.
            //
            if ((streamName == "$I30") && Type.IsIndexAllocation)
                return fileName;

            //  If the StreamName is empty and the StreamType is Data then return only the
            //  FileName. The Data stream is the default stream of regular files.
            if (String.IsNullOrEmpty(streamName) && String.IsNullOrEmpty(Type.StreamName))
                return fileName;

            Int32 Length = 3;

            if (fileName != null)
                Length += fileName.Length;

            if (streamName != null)
                Length += streamName.Length;

            Length += Type.StreamName.Length;

            if (Length == 3) return (null);

            StringBuilder p1 = new StringBuilder();

            if (!String.IsNullOrEmpty(fileName))
                p1.Append(fileName);

            p1.Append(":");

            if (!String.IsNullOrEmpty(streamName))
                p1.Append(streamName);

            p1.Append(":");

            p1.Append(Type.StreamName);

            return p1.ToString();
        }
        
        /// <summary>
        /// "stream name" 
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// "stream type"
        /// </summary>
        public AttributeType Type { get; set; }

        /// <summary>
        /// The fragments of the stream.
        /// </summary>
        public FragmentCollection Fragments { get; set; }

        /// <summary>
        /// Total number of clusters.
        /// </summary>
        public UInt64 NumClusters { get; set; }

        /// <summary>
        ///  Total number of bytes.
        /// </summary>
        public UInt64 TotalBytes { get; set; }

    }
}
