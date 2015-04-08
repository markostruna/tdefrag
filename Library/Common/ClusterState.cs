using System;

namespace TDefragWpf.Library.Common
{
    public enum eClusterState
    {
        Free = 0,
        Allocated,
        Unfragmented,
        Unmovable,
        Fragmented,
        Busy,
        Mft,
        SpaceHog,
        Error,

        MaxValue
    }

    class ClusterState
    {
        public ClusterState(Int32 clusterIndex, eClusterState newState)
        {
            Index = clusterIndex;
            state = newState;
            IsDirty = true;
        }

        public Int32 Index { get; set; }

        private eClusterState state;

        public eClusterState State
        {
            get { return state; }
            set
            {
                if (state != value)
                    IsDirty = true;

                state = value;
            }
        }

        public Boolean IsDirty { get; set; }

        public Boolean MasterFileTable { get; set; }

        public Boolean CurrentlyUsed { get; set; }

        public Boolean SpaceHog { get; set; }
    }
}
