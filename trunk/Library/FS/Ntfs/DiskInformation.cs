using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FS.Ntfs
{
    public class DiskInformation
    {
        public DiskInformation(FS.IBootSector bootSector)
        {
            if (bootSector == null)
            {
                return;
            }

            /* Extract data from the bootblock. */
            BytesPerSector = bootSector.BytesPerSector;
            SectorsPerCluster = bootSector.SectorsPerCluster;

            TotalSectors = bootSector.TotalSectors;
            MasterFileTableStartLogicalClusterNumber = bootSector.MasterFileTable1StartLogicalClusterNumber;
            MasterFileTable2StartLogicalClusterNumber = bootSector.MasterFileTable2StartLogicalClusterNumber;

            UInt64 clustersPerMftRecord = bootSector.ClustersPerMftRecord;
            ClustersPerIndexRecord = bootSector.ClustersPerIndexRecord;

            if (clustersPerMftRecord >= 128)
            {
                BytesPerMasterFileTableRecord = (UInt64)(1 << (256 - (Int16)clustersPerMftRecord));
            }
            else
            {
                BytesPerMasterFileTableRecord = clustersPerMftRecord * BytesPerCluster;
            }
        }

        public UInt64 ClusterToInode(UInt64 cluster)
        {
            return cluster * BytesPerCluster / BytesPerMasterFileTableRecord;
        }

        public UInt64 InodeToCluster(UInt64 inode)
        {
            return inode * BytesPerMasterFileTableRecord / BytesPerCluster;
        }

        public UInt64 ClusterToBytes(UInt64 cluster)
        {
            return cluster * BytesPerCluster;
        }

        public UInt64 BytesToCluster(UInt64 bytes)
        {
            return bytes / BytesPerCluster;
        }

        public UInt64 InodeToBytes(UInt64 inode)
        {
            return inode * BytesPerMasterFileTableRecord;
        }

        public UInt64 BytesToInode(UInt64 bytes)
        {
            return bytes / BytesPerMasterFileTableRecord;
        }

        public UInt64 BytesPerCluster
        {
            get
            {
                return BytesPerSector * SectorsPerCluster;
            }
        }

        public UInt64 BytesPerSector
        { get; private set; }

        public UInt64 SectorsPerCluster
        { get; private set; }

        public UInt64 TotalSectors
        { get; private set; }

        public UInt64 MasterFileTableStartLogicalClusterNumber
        { get; private set; }

        public UInt64 MasterFileTable2StartLogicalClusterNumber
        { get; private set; }

        public UInt64 BytesPerMasterFileTableRecord
        { get; private set; }

        public UInt64 ClustersPerIndexRecord
        { get; private set; }
    }
}
