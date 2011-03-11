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

namespace TDefragLib
{
    public class MainLib
    {
        public MainLib(/*MainWindow mainWindow*/)
        {
            //MainForm = mainWindow;

            Data = new Information();

            ScanNtfs = new Ntfs.Scan(this);
        }

        public static void ShowMessage(String message)
        {
            //mainForm.AddLine(message);
            System.Console.WriteLine(message);
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

        public Information Data
        { set; get; }

        Ntfs.Scan ScanNtfs;

        //private MainWindow MainForm;
    }
}
