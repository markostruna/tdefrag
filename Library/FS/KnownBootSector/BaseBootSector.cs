using System;
using System.Diagnostics;

namespace TDefragLib.FS.KnownBootSector
{
    /// <summary>
    /// Includes the common functionality for all boot sectors
    /// </summary>
    public abstract class BaseBootSector : IBootSector
    {
        private const UInt16 BOOT_SECTOR_SIGNATURE = 0xAA55;

        protected BaseBootSector(byte[] buffer)
        {
            Data = buffer;
            AssertValid();
        }

        [Conditional("DEBUG")]
        protected virtual void AssertValid()
        {
            Debug.Assert(EndOfSector == BOOT_SECTOR_SIGNATURE);
        }

        #region IBootSector Members

        /// <summary>
        /// The end of sector signature, shall always be 0xAA55
        /// </summary>
        public UInt16 EndOfSector { get { return BitConverter.ToUInt16(Data, 0x1FE); } }

        public abstract FileSystemType Filesystem { get; }

        public byte[] Data { get; set; }

        public virtual ushort BytesPerSector
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong SectorsPerCluster
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong TotalSectors
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong OemId
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong MasterFileTable1StartLogicalClusterNumber
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ulong MasterFileTable2StartLogicalClusterNumber
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ushort SectorsPerTrack
        {
            get { throw new NotImplementedException(); }
        }

        public virtual ushort NumberOfHeads
        {
            get { throw new NotImplementedException(); }
        }

        public virtual uint ClustersPerMftRecord
        {
            get { throw new NotImplementedException(); }
        }

        public virtual uint ClustersPerIndexRecord
        {
            get { throw new NotImplementedException(); }
        }

        public abstract ulong Serial { get; }

        public abstract byte MediaType { get; }

        #endregion
    }
}
