using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FS
{
    /// <summary>
    /// Abstract representation of a boot sector. This interface shall contain
    /// only the common elements between all filesystems. It is quite bloated
    /// now.
    /// TODO: reduce the interface to a common subset of properties
    /// </summary>
    public interface IBootSector
    {
        /// <summary>
        /// FileSystemType of the volume
        /// </summary>
        FileSystemType Filesystem { get; }

        /// <summary>
        /// The data buffer of this sector
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// The serial number of this volume
        /// </summary>
        UInt64 Serial { get; }


        //TODO: is this really common stuff?

        Byte MediaType { get; }

        UInt64 OemId { get; }

        UInt16 NumberOfHeads { get; }
        UInt16 BytesPerSector { get; }
        UInt16 SectorsPerTrack { get; }
        UInt64 SectorsPerCluster { get; }

        UInt64 TotalSectors { get; }

        //TODO: this is really NTFS specific, remove later
        UInt64 MasterFileTable1StartLogicalClusterNumber { get; }
        UInt64 MasterFileTable2StartLogicalClusterNumber { get; }

        UInt32 ClustersPerMftRecord { get; }
        UInt32 ClustersPerIndexRecord { get; }
    }
}
