﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.Helper;
using System.Collections;
using TDefrag;
using TDefragWPF;

namespace TDefragLib
{
    public class MainLib
    {
        public MainLib(MainWindow m)
        {
            mainForm = m;

            Data = new Information();

            ScanNtfs = new Ntfs.Scan(this);
        }

        public void ShowMessage(String message)
        {
            //mainForm.AddLine(message);
            System.Console.WriteLine(message);
        }

        public void StartDefrag(String Path)
        {
            //ShowMessage("Defragmentation started!");

            if (!String.IsNullOrEmpty(Path))
            {
                DefragPath(Path);
            }
        }

        private void DefragPath(String Path)
        {
            if (Helper.Wrapper.ElevatePermissions() != 0)
            {
                ShowMessage("Elevate permissions failed!!!");
            }

            Data.volume = new Volume(Path);

            // Open volume for reading
            Data.volume.Open(); 

            // Check if volume was opened for reading
            if (!Data.volume.IsOpen)
            {
                ShowMessage("Unable to open volume!");
            }

            // Get Disk Information
            BitArray bitmap = Data.volume.VolumeBitmap.Buffer;

            Data.NumClusters = (UInt64)bitmap.Count;

            Helper.Wrapper.NTFS_VOLUME_DATA_BUFFER ntfsData = Data.volume.NtfsVolumeData;

            Data.BytesPerCluster = ntfsData.BytesPerCluster;

            Data.MftExcludes[0].Start = ntfsData.MftStartLcn;
            Data.MftExcludes[0].End = ntfsData.MftStartLcn + (UInt64)(ntfsData.MftValidDataLength / ntfsData.BytesPerCluster);

            Data.MftExcludes[1].Start = ntfsData.MftZoneStart;
            Data.MftExcludes[1].End = ntfsData.MftZoneEnd;

            Data.MftExcludes[2].Start = ntfsData.Mft2StartLcn;
            Data.MftExcludes[2].End = ntfsData.Mft2StartLcn + (UInt64)(ntfsData.MftValidDataLength / ntfsData.BytesPerCluster);

            AnalyzeVolume();

            // Close volume
            Data.volume.Close();
        }

        private void AnalyzeVolume()
        {
            ScanNtfs.AnalyzeVolume();
        }

        //SortedList<UInt64, ItemStruct> itemList;
        public List<ItemStruct> itemList;

        /* Insert a record into the tree. The tree is sorted by LCN (Logical Cluster Number). */
        public void AddItemToList(ItemStruct newItem)
        {
            if (itemList == null)
            {
                itemList = new List<ItemStruct>();
            }

            lock (itemList)
            {
                itemList.Add(newItem);
            }
        }

        public Information Data;
        Ntfs.Scan ScanNtfs;

        private MainWindow mainForm;
    }
}