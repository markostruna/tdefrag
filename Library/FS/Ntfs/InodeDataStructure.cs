using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// An inode is the filesystems representation of a file, directory, device,
    /// etc. In NTFS every inode it represented by an MFT FILE record
    /// </summary>
    public class InodeDataStructure
    {
        /// <summary>
        /// Initialize the inode structure
        /// </summary>
        /// <param name="inodeNumber"></param>
        public InodeDataStructure(UInt64 inodeNumber)
        {
            Inode = inodeNumber;
            ParentInode = 5;
            IsDirectory = false;

            IsDirectory = true;

            LongFileName = null;
            ShortFileName = null;
            CreationTime = 0;
            MasterFileTableChangeTime = 0;
            LastAccessTime = 0;
            TotalBytes = 0;
            
            Streams = new StreamCollection();
            
            MasterFileTableDataFragments = null;
            MasterFileTableDataLength = 0;
            MasterFileTableBitmapFragments = null;
            MasterFileTableBitmapLength = 0;
        }

        /* The Inode number. */
        public UInt64 Inode
        { get; private set; }

        /* The m_iNode number of the parent directory. */
        public UInt64 ParentInode
        { get; set; }

        /* true: it's a directory. */
        public Boolean IsDirectory
        { get; set; }

        /* Long filename. */
        public String LongFileName
        { get; private set; }

        /* Short filename (8.3 DOS). */
        public String ShortFileName
        { get; private set; }

        /* Total number of bytes. */
        public UInt64 TotalBytes
        { get; set; }

        /* 1 second = 10000000 */
        public UInt64 CreationTime
        { get; set; }

        public UInt64 MasterFileTableChangeTime
        { get; set; }

        public UInt64 LastAccessTime
        { get; set; }

        /* List of StreamStruct. */
        public StreamCollection Streams
        { get; private set; }

        /// <summary>
        /// The Fragments of the $MFT::$DATA stream.
        /// </summary>
        public FragmentCollection MasterFileTableDataFragments { get; set; }

        /// <summary>
        /// Length of $MFT::$DATA, can be less than what is told by the fragments
        /// </summary>
        public UInt64 MasterFileTableDataLength { get; set; }

        /// <summary>
        /// The Fragments of the $MFT::$BITMAP stream.
        /// </summary>
        public FragmentCollection MasterFileTableBitmapFragments { get; set; }

        /// <summary>
        /// Length of $MFT::$BITMAP, can be less than what is told by the fragments
        /// </summary>
        public UInt64 MasterFileTableBitmapLength { get; set; }

        /// <summary>
        /// Save the filename in either the Long or the Short filename. We only
        /// save the first filename, any additional filenames are hard links. They
        /// might be useful for an optimization algorithm that sorts by filename,
        /// but which of the hardlinked names should it sort? So we only store the
        /// first filename.
        /// </summary>
        /// <param name="attribute"></param>
        public void AddName(FileNameAttribute attribute)
        {
            if (attribute == null)
                return;

            switch (attribute.NameType)
            {
                case NameTypes.Dos:
                    ShortFileName = ShortFileName ?? attribute.Name;
                    break;
                case NameTypes.Ntfs | NameTypes.Dos:
                case NameTypes.Ntfs:
                case NameTypes.Posix:
                    LongFileName = LongFileName ?? attribute.Name;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
