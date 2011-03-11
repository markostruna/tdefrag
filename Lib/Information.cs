using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TDefragLib
{
    public class ExcludesStruct
    {
        private UInt64 startLcn;

        public UInt64 Start
        {
            set { startLcn = value; }
            get { return startLcn; }
        }

        private UInt64 endLcn;

        public UInt64 End
        {
            set { endLcn = value; }
            get { return endLcn; }
        }
    };

    public enum RunningState
    {
        Stopped = 0,
        Running,
        Stopping
    }

    public class Information
    {
        public Information()
        {
            _MasterFileTableExcludes = new Collection<ExcludesStruct>();

            _MasterFileTableExcludes.Add(new ExcludesStruct());
            _MasterFileTableExcludes.Add(new ExcludesStruct());
            _MasterFileTableExcludes.Add(new ExcludesStruct());

            _MasterFileTableExcludes[0].Start = 0;
            _MasterFileTableExcludes[0].End = 0;
            _MasterFileTableExcludes[1].Start = 0;
            _MasterFileTableExcludes[1].End = 0;
            _MasterFileTableExcludes[2].Start = 0;
            _MasterFileTableExcludes[2].End = 0;

            NumberOfClusters = 0;
            BytesPerCluster = 0;
        }

        public UInt64 NumberOfClusters
        {
            get;
            set;
        }

        public UInt64 BytesPerCluster
        {
            get;
            set;
        }

        public UInt64 MasterFileTableLockedClusters
        {
            get;
            set;
        }

        public UInt64 CountDirectories
        {
            get;
            set;
        }

        public UInt64 CountAllFiles
        {
            get;
            set;
        }

        public UInt64 CountAllClusters
        {
            get;
            set;
        }

        public UInt64 CountAllBytes
        {
            get;
            set;
        }

        public UInt64 CountFragmentedItems
        {
            get;
            set;
        }

        public UInt64 CountFragmentedBytes
        {
            get;
            set;
        }

        public UInt64 CountFragmentedClusters
        {
            get;
            set;
        }

        public UInt64 TasksCompleted
        {
            get;
            set;
        }

        public UInt64 TasksCount
        {
            get;
            set;
        }

        // List of clusters reserved for the MFT.
        private Collection<ExcludesStruct> _MasterFileTableExcludes;

        public Collection<ExcludesStruct> MasterFileTableExcludes
        {
            get { return _MasterFileTableExcludes; }
        }

        public Helper.Volume Volume
        { set; get; }
    }
}
