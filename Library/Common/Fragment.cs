using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragWpf.Library.Common;

namespace TDefragLib
{
    /// <summary>
    /// List in memory of the fragments of a file.
    /// Add the size of the fragment to the total number of clusters.
    /// There are two kinds of fragments: real and virtual.
    /// The latter do not occupy clusters on disk, but are information
    /// used by compressed and sparse files. 
    /// </summary>
    [DebuggerDisplay("Lcn={LogicalClusterNumber}, Vcn={LogicalClusterNumber}, Len={Length}")]
    public class Fragment
    {
        public const UInt64 VirtualFragment = UInt64.MaxValue;

        public Fragment(UInt64 logicalClusterNumber, UInt64 virtualClusterNumber, UInt64 length, Boolean isVirtual)
        {
            Length = length;
            VirtualClusterNumber = virtualClusterNumber;

            if (isVirtual)
                LogicalClusterNumber = VirtualFragment;
            else
                LogicalClusterNumber = logicalClusterNumber;

            ClusterState = eClusterState.Allocated;
        }

        /// <summary>
        /// Is this a logical fragment or a virtual one
        /// </summary>
        public Boolean IsLogical
        { get { return LogicalClusterNumber != VirtualFragment; } }
        public Boolean IsVirtual
        { get { return LogicalClusterNumber == VirtualFragment; } }

        /// <summary>
        /// Logical cluster number, location on disk.
        /// </summary>
        public UInt64 LogicalClusterNumber
        { get; private set; }

        /// <summary>
        /// Virtual cluster number, offset from beginning of file.
        /// When representing the data runs of a file, the clusters are given
        /// virtual cluster numbers. Cluster zero refers to the first cluster
        /// of the file. The data runs map the VCNs to LCNs so that the file
        /// can be located on the volume. 
        /// </summary>
        public UInt64 VirtualClusterNumber
        { get; private set; }

        /// <summary>
        /// Length of this fragment in clusters
        /// </summary>
        public UInt64 Length
        { get; private set; }

        /// <summary>
        /// Virtual cluster number of next fragment.
        /// </summary>
        public UInt64 NextVirtualClusterNumber
        { get { return VirtualClusterNumber + Length; } }

        /// <summary>
        /// Logical cluster number of next fragment.
        /// </summary>
        public UInt64 NextLogicalClusterNumber
        { get { return LogicalClusterNumber + Length; } }

        public ItemStruct Item
        { get; set; }

        public eClusterState ClusterState;
    };
}
