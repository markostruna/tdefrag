using System;
using System.Diagnostics;
using System.Text;

namespace TDefragLib.FS.KnownBootSector
{
    /// <summary>
    /// Class for describing boot sector
    /// http://www.ntfs.com/ntfs-partition-boot-sector.htm
    /// </summary>
    class NtfsBootSector : BaseBootSector
    {
        private const String NtfsBootSectorSignature = "NTFS    ";
        
        public NtfsBootSector(byte[] buffer) : base(buffer)
        {
            AssertValid();
        }

        protected override void AssertValid()
        {
            base.AssertValid();

            Debug.Assert(OemIdString.Equals(NtfsBootSectorSignature));
            //Debug.Assert(OemId == 0x202020205346544E);
        }

        #region IBootSector Members

        public override FileSystemType Filesystem { get { return FileSystemType.Ntfs; } }

        public override ushort BytesPerSector { get { return BitConverter.ToUInt16(Data, 11); } }

        public override ulong SectorsPerCluster { get { return BitConverter.ToUInt64(Data, 13); } }

        public override ulong TotalSectors { get { return BitConverter.ToUInt64(Data, 40); } }

        public override ulong MasterFileTable1StartLogicalClusterNumber { get { return BitConverter.ToUInt64(Data, 48); } }

        public override ulong MasterFileTable2StartLogicalClusterNumber { get { return BitConverter.ToUInt64(Data, 56); } }

        public override ushort SectorsPerTrack { get { return BitConverter.ToUInt16(Data, 24); } }

        public override ushort NumberOfHeads { get { return BitConverter.ToUInt16(Data, 26); } }

        public override uint ClustersPerIndexRecord { get { return BitConverter.ToUInt32(Data, 68); } }

        public override uint ClustersPerMftRecord { get { return BitConverter.ToUInt32(Data, 64); } }

        #endregion

        public override UInt64 OemId { get { return BitConverter.ToUInt64(Data, 0x03); } }
        
        public String OemIdString { get { return ASCIIEncoding.ASCII.GetString(Data, 0x03, 8); } }

        public override ulong Serial { get { return BitConverter.ToUInt64(Data, 72); } }

        public override byte MediaType { get { return Data[21]; } }
    }
}
