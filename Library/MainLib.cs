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
using System.Threading;

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

        private Int64 NumSquares = 1;

        public void StartDefrag(String path, Int64 numberOfSquares)
        {
            if (String.IsNullOrEmpty(path))
        {
                return;
            }

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

            Helper.UnsafeNativeMethods.NtfsVolumeDataBuffer ntfsData = Data.Volume.NtfsVolumeData;

            Data.BytesPerCluster = ntfsData.BytesPerCluster;

            Data.MasterFileTableExcludes[0].Start = ntfsData.MasterFileTableStartLogicalClusterNumber;
            Data.MasterFileTableExcludes[0].End = ntfsData.MasterFileTableStartLogicalClusterNumber + (UInt64)(ntfsData.MasterFileTableValidDataLength / ntfsData.BytesPerCluster);

            Data.MasterFileTableExcludes[1].Start = ntfsData.MasterFileTableZoneStart;
            Data.MasterFileTableExcludes[1].End = ntfsData.MasterFileTableZoneEnd;

            Data.MasterFileTableExcludes[2].Start = ntfsData.MasterFileTable2StartLogicalClusterNumber;
            Data.MasterFileTableExcludes[2].End = ntfsData.MasterFileTable2StartLogicalClusterNumber + (UInt64)(ntfsData.MasterFileTableValidDataLength / ntfsData.BytesPerCluster);

            InitDiskSquareStructures();

            ParseDiskBitmap();

            AnalyzeVolume();

            // Close volume
            Data.Volume.Close();
        }

        Double clustersPerSquare = 1;

        private class SquareCluster
        {
            public Dictionary<eClusterState, Int64> NumClusterStates;

            public eClusterState currentState = eClusterState.Free;

            public SquareCluster(Int64 NumClusters)
            {
                NumClusterStates = new Dictionary<eClusterState, long>();

                NumClusterStates.Add(eClusterState.Allocated, 0);
                NumClusterStates.Add(eClusterState.Busy, 0);
                NumClusterStates.Add(eClusterState.Error, 0);
                NumClusterStates.Add(eClusterState.Fragmented, 0);
                NumClusterStates.Add(eClusterState.Free, NumClusters);
                NumClusterStates.Add(eClusterState.Mft, 0);
                NumClusterStates.Add(eClusterState.SpaceHog, 0);
                NumClusterStates.Add(eClusterState.Unfragmented, 0);
                NumClusterStates.Add(eClusterState.Unmovable, 0);
            }

            public eClusterState GetMaxState()
            {
                if (NumClusterStates[eClusterState.Busy] > 0)
                    return eClusterState.Busy;

                if (NumClusterStates[eClusterState.Error] > 0)
                    return eClusterState.Error;

                if (NumClusterStates[eClusterState.Mft] > 0)
                    return eClusterState.Mft;

                if (NumClusterStates[eClusterState.Unmovable] > 0)
                    return eClusterState.Unmovable;

                if (NumClusterStates[eClusterState.Fragmented] > 0)
                    return eClusterState.Fragmented;

                if (NumClusterStates[eClusterState.SpaceHog] > 0)
                    return eClusterState.SpaceHog;

                if (NumClusterStates[eClusterState.Unfragmented] > 0)
                    return eClusterState.Unfragmented;

                if (NumClusterStates[eClusterState.Allocated] > 0)
                    return eClusterState.Allocated;

                return eClusterState.Free;
            }
        }

        Dictionary<Int64, SquareCluster> SquareClusterStates;

        private void InitDiskSquareStructures()
        {
            clustersPerSquare = (Double)((Double)Data.NumberOfClusters / (Double)NumSquares);

            SquareClusterStates = new Dictionary<long, SquareCluster>();

            for (Int32 ii = 0; ii < NumSquares; ii++)
            {
                SquareClusterStates.Add(ii, new SquareCluster((Int64)clustersPerSquare));
            }
        }

        private void AnalyzeVolume()
        {
            ScanNtfs.AnalyzeVolume();
        }

        private Collection<ItemStruct> _ItemCollection;

        public Collection<ItemStruct> ItemCollection
        { get { return _ItemCollection; } }

        private Dictionary<UInt64, Fragment> FragmentCollection
        { set; get; }

        /* Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number). */
        public void AddItemToList(ItemStruct newItem)
        {
            if (newItem == null)
            {
                return;
            }

            if (_ItemCollection == null)
            {
                _ItemCollection = new Collection<ItemStruct>();
            }

            if (FragmentCollection == null)
            {
                FragmentCollection = new Dictionary<ulong,Fragment>();
            }

            lock (_ItemCollection)
            {
                _ItemCollection.Add(newItem);

                lock (FragmentCollection)
                {
                    List<Fragment> frList =
                        (from fr in newItem.FragmentList
                         where fr.IsLogical
                         select fr).ToList();

                    foreach (Fragment fr in frList)
                    {
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

                    //SetClusterState(fragment, isFileError ? eClusterState.Error : ClusterState);
                    SetClusterState(item.LogicalClusterNumber + SegmentBegin, item.LogicalClusterNumber + SegmentEnd, isFileError ? eClusterState.Error : ClusterState);
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

            UInt64 totalClusters = Data.NumberOfClusters;

            //Clusters = new Dictionary<Int32, ClusterState>();

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
                //UInt64 clusterBegin = (UInt64)currentClusterIndex;
                //UInt64 clusterEnd = clusterBegin;

                //eClusterState currentState = eClusterState.Free;

                //Boolean Allocated = bitmapData.Buffer[(Int32)clusterBegin];

                //while ((clusterEnd < totalClusters - 1) && (Allocated == bitmapData.Buffer[(Int32)clusterEnd + 1]))
                //{
                //    clusterEnd++;
                //}

                //if (Allocated)
                //{
                //    currentState = eClusterState.Allocated;
                //}

                //SetClusterState(clusterBegin, clusterEnd, currentState);

                //currentClusterIndex = clusterEnd + 1;
            }

            // Show the MFT zones

            for (int i = 0; i < 3; i++)
            {
                if (Data.MasterFileTableExcludes[i].Start <= 0)
                    continue;

                SetClusterState(Data.MasterFileTableExcludes[i].Start, Data.MasterFileTableExcludes[i].End, eClusterState.Mft);
            }
        }

        private void SetClusterState(Fragment fragment, eClusterState clusterState)
        {
            if (fragment == null)
            {
                return;
            }

            if (fragment.IsVirtual)
            {
                return;
            }

            fragment.ClusterState = clusterState;

            for (UInt64 clusterIndex = fragment.LogicalClusterNumber; clusterIndex < fragment.LogicalClusterNumber + fragment.Length; clusterIndex++)
            {
                Int64 squareIndex = (Int64)((double)fragment.LogicalClusterNumber / clustersPerSquare);
                SquareCluster squareCluster = SquareClusterStates[squareIndex];

                squareCluster.NumClusterStates[fragment.ClusterState] = Math.Max(squareCluster.NumClusterStates[fragment.ClusterState] - 1, 0);
                squareCluster.NumClusterStates[clusterState]++;
            }

            UInt32 squareIndexBegin = (UInt32)((double)fragment.LogicalClusterNumber / clustersPerSquare);
            UInt32 squareIndexEnd = (UInt32)(((double)fragment.LogicalClusterNumber + fragment.Length) / clustersPerSquare);

            for (UInt32 squareIndex = squareIndexBegin; squareIndex <= squareIndexEnd; squareIndex++)
            {
                SquareCluster squareCluster = SquareClusterStates[squareIndex];
                eClusterState newState = squareCluster.GetMaxState();

                if (squareCluster.currentState != newState)
                {
                    MainForm.SetClusterState((UInt32)squareIndex, squareCluster.GetMaxState());
                    squareCluster.currentState = newState;
                }
            }
            //            MainForm.SetClusterState((UInt32)clusterBegin, (UInt32)clusterNext, (UInt32)Data.NumberOfClusters, clusterState);
        }

        private void SetClusterState(UInt64 clusterBegin, UInt64 clusterEnd, eClusterState clusterState)
        {
            for (UInt64 clusterIndex = clusterBegin; clusterIndex <= clusterEnd; clusterIndex++)
            {
                Int64 squareIndex = (Int64)((double)clusterIndex / clustersPerSquare);

                SquareCluster squareCluster = SquareClusterStates[squareIndex];

                if (clusterState != eClusterState.Free)
                {
                    squareCluster.NumClusterStates[eClusterState.Free] = Math.Max(squareCluster.NumClusterStates[eClusterState.Free] - 1, 0);
                }

                squareCluster.NumClusterStates[clusterState]++;
            }

            UInt32 squareIndexBegin = (UInt32)((double)clusterBegin / clustersPerSquare);
            UInt32 squareIndexEnd = (UInt32)((double)clusterEnd / clustersPerSquare);

            for (UInt32 squareIndex = squareIndexBegin; squareIndex <= squareIndexEnd; squareIndex++)
            {
                SquareCluster squareCluster = SquareClusterStates[squareIndex];
                eClusterState newState = squareCluster.GetMaxState();

                if (squareCluster.currentState != newState)
                {
                    MainForm.SetClusterState((UInt32)squareIndex, squareCluster.GetMaxState());
                    squareCluster.currentState = newState;
                }
            }
            //MainForm.SetClusterState((UInt32)clusterBegin, (UInt32)clusterEnd, (UInt32)Data.NumberOfClusters, clusterState);
        }

        private void SetClusterState1(UInt64 clusterBegin, UInt64 clusterEnd, eClusterState clusterState)
        {
            for (UInt64 clusterIndex = clusterBegin; clusterIndex <= clusterEnd; clusterIndex++)
            {
                Int64 squareIndex = (Int64)((double)clusterIndex / clustersPerSquare);

                SquareCluster squareCluster = SquareClusterStates[squareIndex];
                Fragment fr = null;

                if (FragmentCollection != null && FragmentCollection.TryGetValue(clusterIndex, out fr))
                {
                    if (fr.ClusterState != clusterState)
                    {
                        squareCluster.NumClusterStates[fr.ClusterState] = Math.Max(squareCluster.NumClusterStates[fr.ClusterState] - 1, 0);

                        fr.ClusterState = clusterState;
                    }
                }
                else
                {
                    if (clusterState != eClusterState.Free)
                    {
                        squareCluster.NumClusterStates[eClusterState.Free] = Math.Max(squareCluster.NumClusterStates[eClusterState.Free] - 1, 0);
                    }
                }

                squareCluster.NumClusterStates[clusterState]++;
            }
            
            UInt32 squareIndexBegin = (UInt32)((double)clusterBegin / clustersPerSquare);
            UInt32 squareIndexEnd = (UInt32)((double)clusterEnd / clustersPerSquare);

            for (UInt32 squareIndex = squareIndexBegin; squareIndex <= squareIndexEnd; squareIndex++)
            {
                SquareCluster squareCluster = SquareClusterStates[squareIndex];
                eClusterState newState = squareCluster.GetMaxState();

                if (squareCluster.currentState != newState)
                {
                    MainForm.SetClusterState((UInt32)squareIndex, squareCluster.GetMaxState());
                    squareCluster.currentState = newState;
                }
            }
//            MainForm.SetClusterState((UInt32)clusterBegin, (UInt32)clusterNext, (UInt32)Data.NumberOfClusters, clusterState);
        }

        public Information Data
        { set; get; }

        Ntfs.Scan ScanNtfs;

        private MainWindow MainForm;
    }
}
