using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.Helper;
using System.Collections;
using TDefrag;
using TDefragWPF;
using TDefragWpf.Properties;
using System.Collections.ObjectModel;
using TDefragWpf.Library.Common;

namespace TDefragLib
{
    public class MainLibrary
    {
        public MainLibrary(MainWindow mainWindow)
        {
            MainForm = mainWindow;

            Data = new Information();

            ScanNtfs = new Ntfs.Scan(this);
        }

        public void ShowMessage(String message)
        {
            MainForm.AddLine(message);
        }

        public void UpdateProgress(Double progress)
        {
            MainForm.UpdateProgress(progress);
        }

        public void StartDefrag(String path)
        {
            //ShowMessage("Defragmentation started!");

            if (!String.IsNullOrEmpty(path))
            {
                DefragPath(path);
            }
        }

        private void DefragPath(String Path)
        {
            if (Helper.UnsafeNativeMethods.ElevatePermissions() != 0)
            {
                ShowMessage(Resources.ErrorElevatePermissionsFailed);
            }

            Data.Volume = new Volume(Path);

            // Open volume for reading
            Data.Volume.Open(); 

            // Check if volume was opened for reading
            if (!Data.Volume.IsOpen)
            {
                ShowMessage(Resources.ErrorUnableToOpenVolume);
            }

            // Get Disk Information
            BitArray bitmap = Data.Volume.VolumeBitmap.Buffer;

            Data.NumberOfClusters = (UInt64)bitmap.Count;

            Helper.UnsafeNativeMethods.NtfsVolumeDataBuffer ntfsData = Data.Volume.NtfsVolumeData;

            Data.BytesPerCluster = ntfsData.BytesPerCluster;

            Data.MasterFileTableExcludes[0].Start = ntfsData.MasterFileTableStartLogicalClusterNumber;
            Data.MasterFileTableExcludes[0].End = ntfsData.MasterFileTableStartLogicalClusterNumber + (UInt64)(ntfsData.MasterFileTableValidDataLength / ntfsData.BytesPerCluster);

            Data.MasterFileTableExcludes[1].Start = ntfsData.MasterFileTableZoneStart;
            Data.MasterFileTableExcludes[1].End = ntfsData.MasterFileTableZoneEnd;

            Data.MasterFileTableExcludes[2].Start = ntfsData.MasterFileTable2StartLogicalClusterNumber;
            Data.MasterFileTableExcludes[2].End = ntfsData.MasterFileTable2StartLogicalClusterNumber + (UInt64)(ntfsData.MasterFileTableValidDataLength / ntfsData.BytesPerCluster);

            ParseDiskBitmap();

            AnalyzeVolume();

            // Close volume
            Data.Volume.Close();
        }

        private void AnalyzeVolume()
        {
            ScanNtfs.AnalyzeVolume();
        }

        private Collection<ItemStruct> _ItemCollection;

        public Collection<ItemStruct> ItemCollection
        { get { return _ItemCollection; } }

        /* Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number). */
        public void AddItemToList(ItemStruct newItem)
        {
            if (_ItemCollection == null)
            {
                _ItemCollection = new Collection<ItemStruct>();
            }

            lock (_ItemCollection)
            {
                _ItemCollection.Add(newItem);
            }
        }

        /// <summary>
        /// Colorize an item (file, directory) on the screen in the proper color
        /// (fragmented, unfragmented, unmovable, empty). If specified then highlight
        /// part of the item. If Undraw=YES then remove the item from the screen.
        /// 
        /// NOTE:
        /// The offset and size of the highlight block is in absolute clusters,
        /// not virtual clusters.
        /// </summary>
        /// 
        /// <param name="item"></param>
        /// <param name="busyOffset">Number of first cluster to be highlighted.</param>
        /// <param name="busySize">Number of clusters to be highlighted.</param>
        /// <param name="revertColoring">true to undraw the file from the screen.</param>
        public void ColorizeItem(ItemStruct item, UInt64 busyOffset, UInt64 busySize, Boolean revertColoring, Boolean isFileError)
        {
            UInt64 SegmentBegin;
            UInt64 SegmentEnd;

            eClusterState ClusterState;

            if (item == null)
                return;

            // Determine if the item is fragmented.
            Boolean Fragmented = item.IsFragmented(0, item.CountClusters);

            // Walk through all the fragments of the file.
            UInt64 RealVcn = 0;

            foreach (Fragment fragment in item.FragmentList)
            {
                // Ignore virtual fragments. They do not occupy space on disk and do not require colorization.
                if (fragment.IsVirtual)
                    continue;

                UInt64 Vcn = fragment.VirtualClusterNumber;

                SegmentBegin = 0;
                SegmentEnd = Math.Min(fragment.Length, Data.CountAllClusters - RealVcn);

                // Walk through all the segments of the file. A segment is usually the same as a fragment,
                // but if a fragment spans across a boundary then we must determine the color of the left 
                // and right parts individually. So we pretend the fragment is divided into segments
                // at the various possible boundaries.

                while (SegmentBegin < SegmentEnd)
                {
                    // Determine the color with which to draw this segment.

                    if (revertColoring == false)
                    {
                        ClusterState = eClusterState.Unfragmented;

                        if (item.SpaceHog) ClusterState = eClusterState.SpaceHog;
                        if (Fragmented) ClusterState = eClusterState.Fragmented;
                        if (item.Unmovable) ClusterState = eClusterState.Unmovable;
                        if (item.Exclude) ClusterState = eClusterState.Unmovable;

                        if ((Vcn + SegmentBegin < busyOffset) &&
                            (Vcn + SegmentEnd > busyOffset))
                        {
                            SegmentEnd = busyOffset - Vcn;
                        }

                        if ((Vcn + SegmentBegin >= busyOffset) &&
                            (Vcn + SegmentBegin < busyOffset + busySize))
                        {
                            if (SegmentEnd > busyOffset + busySize - Vcn)
                            {
                                SegmentEnd = busyOffset + busySize - Vcn;
                            }

                            ClusterState = eClusterState.Busy;
                        }
                    }
                    else
                    {
                        ClusterState = eClusterState.Free;

                        for (int i = 0; i < 3; i++)
                        {
                            if ((fragment.LogicalClusterNumber + SegmentBegin < Data.MasterFileTableExcludes[i].Start) &&
                                (fragment.LogicalClusterNumber + SegmentEnd > Data.MasterFileTableExcludes[i].Start))
                            {
                                SegmentEnd = Data.MasterFileTableExcludes[i].Start - fragment.LogicalClusterNumber;
                            }

                            if ((fragment.LogicalClusterNumber + SegmentBegin >= Data.MasterFileTableExcludes[i].Start) &&
                                (fragment.LogicalClusterNumber + SegmentBegin < Data.MasterFileTableExcludes[i].End))
                            {
                                if (fragment.LogicalClusterNumber + SegmentEnd > Data.MasterFileTableExcludes[i].End)
                                {
                                    SegmentEnd = Data.MasterFileTableExcludes[i].End - fragment.LogicalClusterNumber;
                                }

                                ClusterState = eClusterState.Mft;
                            }
                        }
                    }

                    // Colorize the segment.
                    //defragmenter.SetClusterState((Int32)(fragment.Lcn + SegmentBegin), (Int32)(fragment.Lcn + SegmentEnd), ClusterState);

                    MainForm.SetClusterState(item.LogicalClusterNumber, Data.NumberOfClusters, isFileError ? eClusterState.Error : ClusterState);
                    //defragmenter.SetClusterState(Item, Error ? eClusterState.Error : ClusterState);

                    // Next segment
                    SegmentBegin = SegmentEnd;
                }

                // Next fragment
                RealVcn += fragment.Length;
            }
        }

        public void ParseDiskBitmap()
        {
            if (Data == null)
            {
                return;
            }

            Int32 totalClusters = (Int32)Data.NumberOfClusters;

            // Fetch a block of cluster data.

            TDefragLib.Helper.UnsafeNativeMethods.BitmapData bitmapData = Data.Volume.VolumeBitmap;

            Double Index = 0;
            Double IndexMax = bitmapData.Buffer.Length;

            while (Index < IndexMax)
            {
                Int32 currentCluster = (Int32)Index;
                Int32 nextCluster = (Int32)currentCluster;

                eClusterState currentState = eClusterState.Free;

                Boolean Allocated = bitmapData.Buffer[currentCluster];

                while ((nextCluster < totalClusters - 1) && (Allocated == bitmapData.Buffer[nextCluster + 1]))
                {
                    nextCluster++;
                }

                if (Allocated)
                {
                    currentState = eClusterState.Allocated;
                }

                MainForm.SetClusterState((UInt32)currentCluster, (UInt32)nextCluster, (UInt32)totalClusters, currentState);

                Index = nextCluster + 1;
            }

            // Show the MFT zones

            for (int i = 0; i < 3; i++)
            {
                if (Data.MasterFileTableExcludes[i].Start <= 0)
                    continue;

                MainForm.SetClusterState((UInt32)Data.MasterFileTableExcludes[i].Start, (UInt32)Data.MasterFileTableExcludes[i].End, eClusterState.Mft);
            }
        }

        public Information Data
        { set; get; }

        Ntfs.Scan ScanNtfs;

        private MainWindow MainForm;
    }
}
