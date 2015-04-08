using System;

namespace TDefragWpf.Library.Helper
{
    public class NtfsVolumeDataBuffer
    {
        public UInt64 VolumeSerialNumber { get; set; }

        public UInt64 NumberSectors { get; set; }

        public UInt64 TotalClusters { get; set; }

        public UInt64 FreeClusters { get; set; }

        public UInt64 TotalReserved { get; set; }

        public UInt32 BytesPerSector { get; set; }

        public UInt32 BytesPerCluster { get; set; }

        public UInt32 BytesPerFileRecordSegment { get; set; }

        public UInt32 ClustersPerFileRecordSegment { get; set; }

        public UInt64 MasterFileTableValidDataLength { get; set; }

        public UInt64 MasterFileTableStartLogicalClusterNumber { get; set; }

        public UInt64 MasterFileTableEndLogicalClusterNumber
        {
            get { return (MasterFileTableStartLogicalClusterNumber + MasterFileTableSizeInClusters); }
        }

        public UInt64 MasterFileTable2StartLogicalClusterNumber { get; set; }

        public UInt64 MasterFileTable2EndLogicalClusterNumber
        {
            get { return (MasterFileTable2StartLogicalClusterNumber + MasterFileTableSizeInClusters); }
        }

        public UInt64 MasterFileTableSizeInClusters
        {
            get
            {
                if (BytesPerCluster == 0)
                    return 0;

                return (UInt64)(MasterFileTableValidDataLength / BytesPerCluster);
            }
        }

        public UInt64 MasterFileTableZoneStart { get; set; }

        public UInt64 MasterFileTableZoneEnd { get; set; }
    };
}
