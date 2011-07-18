using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib
{
    /// <summary>
    /// List in memory of all the files on disk, sorted by LCN (Logical Cluster Number).
    /// </summary>
    public class ItemStruct
    {
        public ItemStruct(FileSystem.Ntfs.Stream stream)
        {
            if (stream == null)
            {
                return;
            }

            FragmentList = stream.Fragments;
            Error = false;
        }

        /// <summary>
        /// Return the location on disk (LCN, Logical
        /// Cluster Number) of an item.
        /// </summary>
        public UInt64 LogicalClusterNumber
        {
            get
            {
                return FragmentList.LogicalClusterNumber;
            }
        }

        /// <summary>
        /// Return the number of fragments in the item.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public int FragmentCount
        {
            get
            {
                return FragmentList.FragmentCount;
            }
        }

        /// <summary>
        /// Return true if the block in the item starting at Offset with Size clusters
        /// is fragmented, otherwise return false.
        /// 
        /// Note: this function does not ask Windows for a fresh list of fragments,
        ///       it only looks at cached information in memory.
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Boolean IsFragmented(UInt64 offset, UInt64 size)
        {
            UInt64 FragmentBegin = 0;
            UInt64 FragmentEnd = 0;
            UInt64 NextLcn = 0;

            // Walk through all fragments. If a fragment is found where either the begin or the end of 
            // the fragment is inside the block then the file is fragmented and return true.

            foreach (Fragment fragment in FragmentList)
            {
                // Virtual fragments do not occupy space on disk and do not count as fragments.
                if (fragment.IsLogical == false)
                    continue;

                // Treat aligned fragments as a single fragment. Windows will frequently split files
                // in fragments even though they are perfectly aligned on disk, especially system 
                // files and very large files. The defragger treats these files as unfragmented.

                if ((NextLcn != 0) && (fragment.LogicalClusterNumber != NextLcn))
                {
                    // If the fragment is above the block then return false;
                    // the block is not fragmented and we don't have to scan any further.

                    if (FragmentBegin >= offset + size)
                        return false;

                    // If the first cluster of the fragment is above the first cluster of the block,
                    // or the last cluster of the fragment is before the last cluster of the block,
                    // then the block is fragmented, return true.

                    if ((FragmentBegin > offset) || ((FragmentEnd - 1 >= offset) && (FragmentEnd < offset + size)))
                    {
                        return true;
                    }

                    FragmentBegin = FragmentEnd;
                }

                FragmentEnd += fragment.Length;
                NextLcn = fragment.NextLogicalClusterNumber;
            }

            // Handle the last fragment.
            if (FragmentBegin >= offset + size)
                return false;

            if ((FragmentBegin > offset) || ((FragmentEnd - 1 >= offset) && (FragmentEnd < offset + size)))
            {
                return true;
            }

            // Return false, the item is not fragmented inside the block.
            return false;
        }

        public String GetPath(Boolean shortPath)
        {
            String path = String.Empty;

            path = shortPath ? ShortFileName : LongFileName;

            if (String.IsNullOrEmpty(path))
            {
                path = shortPath ? LongFileName : ShortFileName;
            }

            if (String.IsNullOrEmpty(path))
            {
                path = String.Empty;
            }

            return path;
        }

        public String GetCompletePath(String mountPoint, Boolean shortPath)
        {
            String path = String.Empty;
            ItemStruct parent = ParentDirectory;

            while (parent != null)
            {
                path = parent.GetPath(shortPath) + "\\" + path;

                parent = parent.ParentDirectory;
            }

	        /* Append all the strings. */
	        path = mountPoint + "\\" + path;

            return path;
        }

        public ItemStruct Parent                  /* Parent item. */
        { set; get; }

        public ItemStruct Smaller                 /* Next smaller item. */
        { set; get; }

        public ItemStruct Bigger                  /* Next bigger item. */
        { set; get; }


        public String LongFileName                /* Long filename. */
        { set; get; }

        public String LongPath                    /* Full path on disk, long filenames. */
        { set; get; }

        public String ShortFileName               /* Short filename (8.3 DOS). */
        { set; get; }

        public String ShortPath                   /* Full path on disk, short filenames. */
        { set; get; }


        public UInt64 Size                        /* Total number of bytes. */
        { set; get; }

        public UInt64 CountClusters               /* Total number of clusters. */
        { set; get; }

        public UInt64 CreationTime                /* 1 second = 10000000 */
        { set; get; }

        public UInt64 MasterFileTableChangeTime
        { set; get; }

        public UInt64 LastAccessTime
        { set; get; }


        /* List of fragments. */
        public FragmentCollection FragmentList { get; private set; }

        public UInt64 ParentIndexNode                 /* The Inode number of the parent directory. */
        { set; get; }

        public ItemStruct ParentDirectory
        { set; get; }

        public Boolean IsDirectory                /* YES: it's a directory. */
        { set; get; }

        public Boolean Unmovable                  /* YES: file can't/couldn't be moved. */
        { set; get; }

        public Boolean Exclude                    /* YES: file is not to be defragged/optimized. */
        { set; get; }

        public Boolean SpaceHog                   /* YES: file to be moved to end of disk. */
        { set; get; }

        public Boolean Error
        { set; get; }

    };
}
