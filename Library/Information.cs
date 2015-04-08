using System;
using System.Collections.ObjectModel;
using TDefragWpf.Library.Structures;

namespace TDefragLib
{
    public class Information
    {
        public Information()
        {
            MasterFileTableExcludes = new Collection<Excludes>();

            NumberOfClusters = 0;
            BytesPerCluster = 0;
        }

        public UInt64 NumberOfClusters { get; set; }

        public UInt64 BytesPerCluster { get; set; }

        public UInt64 MasterFileTableLockedClusters { get; set; }

        public UInt64 CountDirectories { get; set; }

        public UInt64 CountAllFiles { get; set; }

        public UInt64 CountAllClusters { get; set; }

        public UInt64 CountAllBytes { get; set; }

        public UInt64 CountFragmentedItems { get; set; }

        public UInt64 CountFragmentedBytes { get; set; }

        public UInt64 CountFragmentedClusters { get; set; }

        public UInt64 TasksCompleted { get; set; }

        public UInt64 TasksCount { get; set; }


        /// <summary>
        /// List of clusters reserved for the MFT.
        /// </summary>
        public Collection<Excludes> MasterFileTableExcludes { get; set; }

        public Helper.Volume Volume { get; set; }
    }
}
