using System;
using System.Collections.Generic;
using TDefragLib.Helper;
using System.Collections;
using TDefragWpf;
using TDefragWpf.Properties;
using System.Collections.ObjectModel;
using TDefragWpf.Library.Common;
using TDefragWpf.Library.Helper;
using TDefragWpf.Library.Structures;

namespace TDefragLib
{
    public class MainLibrary
    {
        public MainLibrary(MainWindow mainWindow)
        {
            MainForm = mainWindow;

            Data = new Information();

            ItemCollection = new Collection<ItemStruct>();

            FragmentCollection = new Dictionary<ulong, Fragment>();
            
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

        private Int64 NumSquares { get; set; }

        public void StartDefrag(String path, Int64 numberOfSquares)
        {
            if (String.IsNullOrEmpty(path))
                return;

            NumSquares = numberOfSquares;
            DefragPath(path);
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

            NtfsVolumeDataBuffer ntfsData = Data.Volume.NtfsVolumeData;

            Data.BytesPerCluster = ntfsData.BytesPerCluster;

            Data.MasterFileTableExcludes.Add(new Excludes(ntfsData.MasterFileTableStartLogicalClusterNumber, ntfsData.MasterFileTableEndLogicalClusterNumber));
            Data.MasterFileTableExcludes.Add(new Excludes(ntfsData.MasterFileTableZoneStart, ntfsData.MasterFileTableZoneEnd));
            Data.MasterFileTableExcludes.Add(new Excludes(ntfsData.MasterFileTable2StartLogicalClusterNumber, ntfsData.MasterFileTable2EndLogicalClusterNumber));

            InitDiskSquareStructures();

            ParseDiskBitmap();

            AnalyzeVolume();

            // Close volume
            Data.Volume.Close();
        }

        Double clustersPerSquare { get; set; }

        SquareCluster[] SquareClusterStates { get; set; }

        /// <summary>
        /// InitDiskSquareStructures
        /// </summary>
        private void InitDiskSquareStructures()
        {
            clustersPerSquare = (Double)((Double)Data.NumberOfClusters / (Double)NumSquares);

            SquareClusterStates = new SquareCluster[NumSquares];

            for (Int32 ii = 0; ii < NumSquares; ii++)
                SquareClusterStates[ii] = new SquareCluster();
        }

        /// <summary>
        /// AnalyzeVolume
        /// </summary>
        private void AnalyzeVolume()
        {
            ScanNtfs.AnalyzeVolume();
        }

        /// <summary>
        /// ItemCollection
        /// </summary>
        public Collection<ItemStruct> ItemCollection { get; set; }

        /// <summary>
        /// FragmentCollection
        /// </summary>
        private Dictionary<UInt64, Fragment> FragmentCollection { get; set; }

        /// <summary>
        /// Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number).
        /// </summary>
        /// <param name="newItem"></param>
        public void AddItemToList(ItemStruct newItem)
        {
            if (newItem == null)
            {
                return;
            }

            lock (ItemCollection)
            {
                ItemCollection.Add(newItem);

                lock (FragmentCollection)
                {
                    //List<Fragment> frList = newItem.FragmentList.Select(a => a).Where(a => a.IsLogical).ToList();
                        //(from fr in newItem.FragmentList
                        // where fr.IsLogical
                        // select fr).ToList();

                    foreach (Fragment fr in newItem.FragmentList)
                    {
                        if (fr.IsVirtual)
                            continue;

                        fr.Item = newItem;
                        fr.ClusterState = eClusterState.Free;

                        if (!FragmentCollection.ContainsKey(fr.LogicalClusterNumber))
                        {
                            FragmentCollection.Add(fr.LogicalClusterNumber, fr);
                        }
                    }
                }
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
                SegmentEnd = fragment.Length;
                //SegmentEnd = Math.Min(fragment.Length, Data.CountAllClusters - RealVcn);

                // Walk through all the segments of the file. A segment is usually the same as a fragment,
                // but if a fragment spans across a boundary then we must determine the color of the left 
                // and right parts individually. So we pretend the fragment is divided into segments
                // at the various possible boundaries.

                while (SegmentBegin < SegmentEnd)
                {
                    // Determine the color with which to draw this segment.

                    //if (revertColoring == false)
                    //{
                        ClusterState = eClusterState.Unfragmented;

                        if (item.Exclude) 
                            ClusterState = eClusterState.Unmovable;
                        else if (item.Unmovable) 
                            ClusterState = eClusterState.Unmovable;
                        else if (Fragmented)
                            ClusterState = eClusterState.Fragmented;
                        else if (item.SpaceHog)
                            ClusterState = eClusterState.SpaceHog;

                        //if ((Vcn + SegmentBegin < busyOffset) &&
                        //    (Vcn + SegmentEnd > busyOffset))
                        //{
                        //    SegmentEnd = busyOffset - Vcn;
                        //}

                        //if ((Vcn + SegmentBegin >= busyOffset) &&
                        //    (Vcn + SegmentBegin < busyOffset + busySize))
                        //{
                        //    if (SegmentEnd > busyOffset + busySize - Vcn)
                        //    {
                        //        SegmentEnd = busyOffset + busySize - Vcn;
                        //    }

                        //    ClusterState = eClusterState.Busy;
                        //}
                    //}
                    //else
                    //{
                    //    ClusterState = eClusterState.Free;

                    //    for (int i = 0; i < 3; i++)
                    //    {
                    //        if ((fragment.LogicalClusterNumber + SegmentBegin < Data.MasterFileTableExcludes[i].StartLcn) &&
                    //            (fragment.LogicalClusterNumber + SegmentEnd > Data.MasterFileTableExcludes[i].StartLcn))
                    //        {
                    //            SegmentEnd = Data.MasterFileTableExcludes[i].StartLcn - fragment.LogicalClusterNumber;
                    //        }

                    //        if ((fragment.LogicalClusterNumber + SegmentBegin >= Data.MasterFileTableExcludes[i].StartLcn) &&
                    //            (fragment.LogicalClusterNumber + SegmentBegin < Data.MasterFileTableExcludes[i].EndLcn))
                    //        {
                    //            if (fragment.LogicalClusterNumber + SegmentEnd > Data.MasterFileTableExcludes[i].EndLcn)
                    //            {
                    //                SegmentEnd = Data.MasterFileTableExcludes[i].EndLcn - fragment.LogicalClusterNumber;
                    //            }

                    //            ClusterState = eClusterState.Mft;
                    //        }
                    //    }
                    //}

                    SetClusterState(item.LogicalClusterNumber + SegmentBegin, item.LogicalClusterNumber + SegmentEnd, isFileError ? eClusterState.Error : ClusterState);

                    // Next segment
                    SegmentBegin = SegmentEnd;
                }

                // Next fragment
                RealVcn += fragment.Length;
            }
        }

        /// <summary>
        /// ParseDiskBitmap
        /// </summary>
        public void ParseDiskBitmap()
        {
            if (Data == null)
                return;

            UInt64 totalClusters = Data.NumberOfClusters;

            // Fetch a block of cluster data.

            TDefragLib.Helper.UnsafeNativeMethods.BitmapData bitmapData = Data.Volume.VolumeBitmap;

            UInt64 currentClusterIndex = 0;
            UInt64 maxClusterIndex = (UInt64)bitmapData.Buffer.Length;

            while (currentClusterIndex < maxClusterIndex)
            {
                UInt64 clusterBegin = (UInt64)currentClusterIndex;
                UInt64 clusterEnd = Math.Min((UInt64)(clusterBegin + clustersPerSquare), (UInt64)totalClusters - 1); ;

                eClusterState currentState = eClusterState.Free;

                Boolean Allocated = bitmapData.Buffer[(Int32)clusterBegin];

                if (!Allocated)
                {
                    for (UInt64 clusterNumber = clusterBegin; clusterNumber <= clusterEnd; clusterNumber++)
                    {
                        Allocated = bitmapData.Buffer[(Int32)clusterNumber];

                        if (Allocated == true) break;
                    }
                }

                if (Allocated)
                {
                    currentState = eClusterState.Allocated;
                }

                SetClusterState(clusterBegin, clusterEnd, currentState);

                currentClusterIndex += (UInt64)clustersPerSquare;
            }

            // Show the MFT zones

            foreach (Excludes exclude in Data.MasterFileTableExcludes)
            {
                if (exclude.StartLcn <= 0)
                    continue;

                SetClusterState(exclude.StartLcn, exclude.EndLcn, eClusterState.Mft);
            }
        }

        /// <summary>
        /// SetClusterState
        /// </summary>
        /// <param name="clusterBegin"></param>
        /// <param name="clusterEnd"></param>
        /// <param name="clusterState"></param>
        private void SetClusterState(UInt64 clusterBegin, UInt64 clusterEnd, eClusterState clusterState)
        {
            UInt32 squareIndexBegin = (UInt32)((double)clusterBegin / clustersPerSquare);
            UInt32 squareIndexEnd = (UInt32)((double)clusterEnd / clustersPerSquare);

            for (UInt32 squareIndex = squareIndexBegin; squareIndex <= squareIndexEnd; squareIndex++)
            {
                SquareCluster squareCluster = SquareClusterStates[squareIndex];
                
                squareCluster.ChangeState(clusterState);

                if (squareCluster.IsDirty)
                {
                    MainForm.SetClusterState(squareIndex, clusterState);

                    squareCluster.IsDirty = false;
                }
            }
        }

        public Information Data { get; set; }

        Ntfs.Scan ScanNtfs { get; set; }

        private MainWindow MainForm;
    }
}
