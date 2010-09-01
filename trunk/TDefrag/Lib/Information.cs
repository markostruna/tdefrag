using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib
{
    public class ExcludesStruct
    {
        public UInt64 Start;
        public UInt64 End;
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
            MftExcludes = new List<ExcludesStruct>(3);

            MftExcludes.Add(new ExcludesStruct());
            MftExcludes.Add(new ExcludesStruct());
            MftExcludes.Add(new ExcludesStruct());

            MftExcludes[0].Start = 0;
            MftExcludes[0].End = 0;
            MftExcludes[1].Start = 0;
            MftExcludes[1].End = 0;
            MftExcludes[2].Start = 0;
            MftExcludes[2].End = 0;

            NumClusters = 0;
            BytesPerCluster = 0;
        }

        public UInt64 NumClusters
        {
            get;
            set;
        }

        public UInt64 BytesPerCluster
        {
            get;
            set;
        }

        public UInt64 MftLockedClusters
        {
            get;
            set;
        }

        // List of clusters reserved for the MFT.
        public List<ExcludesStruct> MftExcludes;

        public Helper.Volume volume;
    }
}
