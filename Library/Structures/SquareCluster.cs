using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragWpf.Library.Common;

namespace TDefragWpf.Library.Structures
{
    public class SquareCluster
    {
        //public Dictionary<eClusterState, Int64> NumClusterStates;

        public Int64[] states { get; set; }

        public Int64 Allocated { get; set; }
        public Int64 Busy { get; set; }
        public Int64 Error { get; set; }
        public Int64 Fragmented { get; set; }
        public Int64 Free { get; set; }
        public Int64 Mft { get; set; }
        public Int64 SpaceHog { get; set; }
        public Int64 Unfragmented { get; set; }
        public Int64 Unmovable { get; set; }



        public eClusterState currentState = eClusterState.Free;

        public SquareCluster()
        {
            states = new Int64[9];

            states[0] = 0;
            states[1] = 0;
            states[2] = 0;
            states[3] = 0;
            states[4] = 0;
            states[5] = 0;
            states[6] = 0;
            states[7] = 0;
            states[8] = 0;

            //NumClusterStates = new Dictionary<eClusterState, long>();

            //NumClusterStates.Add(eClusterState.Allocated, 0);
            //NumClusterStates.Add(eClusterState.Busy, 0);
            //NumClusterStates.Add(eClusterState.Error, 0);
            //NumClusterStates.Add(eClusterState.Fragmented, 0);
            //NumClusterStates.Add(eClusterState.Free, NumClusters);
            //NumClusterStates.Add(eClusterState.Mft, 0);
            //NumClusterStates.Add(eClusterState.SpaceHog, 0);
            //NumClusterStates.Add(eClusterState.Unfragmented, 0);
            //NumClusterStates.Add(eClusterState.Unmovable, 0);

            currentState = eClusterState.Free;

            //Allocated = 0;
            //Busy = 0;
            //Error = 0;
            //Fragmented = 0;
            //Free = NumClusters;
            //Mft = 0;
            //SpaceHog = 0;
            //Unfragmented = 0;
            //Unmovable = 0;

        }

        public eClusterState GetMaxState()
        {
            return currentState;

            ////if (NumClusterStates[eClusterState.Busy] > 0)
            ////    return eClusterState.Busy;

            ////if (NumClusterStates[eClusterState.Error] > 0)
            ////    return eClusterState.Error;

            ////if (NumClusterStates[eClusterState.Mft] > 0)
            ////    return eClusterState.Mft;

            ////if (NumClusterStates[eClusterState.Unmovable] > 0)
            ////    return eClusterState.Unmovable;

            ////if (NumClusterStates[eClusterState.Fragmented] > 0)
            ////    return eClusterState.Fragmented;

            ////if (NumClusterStates[eClusterState.SpaceHog] > 0)
            ////    return eClusterState.SpaceHog;

            ////if (NumClusterStates[eClusterState.Unfragmented] > 0)
            ////    return eClusterState.Unfragmented;

            ////if (NumClusterStates[eClusterState.Allocated] > 0)
            ////    return eClusterState.Allocated;

            //if (Busy > 0)
            //    return eClusterState.Busy;

            //if (Error > 0)
            //    return eClusterState.Error;

            //if (Mft > 0)
            //    return eClusterState.Mft;

            //if (Unmovable > 0)
            //    return eClusterState.Unmovable;

            //if (Fragmented > 0)
            //    return eClusterState.Fragmented;

            //if (SpaceHog > 0)
            //    return eClusterState.SpaceHog;

            //if (Unfragmented > 0)
            //    return eClusterState.Unfragmented;

            //if (Allocated > 0)
            //    return eClusterState.Allocated;


            //return eClusterState.Free;
        }

        public bool IsDirty { get; set; }

        public void ChangeState(eClusterState state, int increment)
        {
            states[(Int32)state]++;

            switch (state)
            {
                case eClusterState.Allocated:
                    if (currentState == eClusterState.Free)
                    {
                        if (states[(Int32)currentState] > 0)
                            states[(Int32)currentState]--;
                        currentState = state;
                        IsDirty = true;
                    }
                    break;
                case eClusterState.Error:
                case eClusterState.Mft:
                    {
                        if (states[(Int32)currentState] > 0)
                            states[(Int32)currentState]--;
                        currentState = state;
                        IsDirty = true;
                    }
                    break;
                case eClusterState.Fragmented:
                    if (currentState == eClusterState.Allocated || currentState == eClusterState.Free || currentState == eClusterState.Unfragmented)
                    {
                        if (states[(Int32)currentState] > 0)
                            states[(Int32)currentState]--;
                        currentState = state;
                        IsDirty = true;
                    }
                    break;
                case eClusterState.Unfragmented:
                    if (currentState == eClusterState.Allocated)
                    {
                        if (states[(Int32)currentState] > 0)
                            states[(Int32)currentState]--;
                        currentState = state;
                        IsDirty = true;
                    }
                    break;
            }
        }

        //public void ChangeState(eClusterState state, int increment)
        //{
        //    if (state == eClusterState.Fragmented)
        //        Fragmented += increment;

        //    else if (state == eClusterState.Free)
        //        Free += increment * 2;

        //    else if (state == eClusterState.Unfragmented)
        //        Unfragmented += increment;

        //    else if (state == eClusterState.Allocated)
        //        Allocated += increment;

        //    else if (state == eClusterState.Busy)
        //        Busy += increment;

        //    else if (state == eClusterState.Error)
        //        Error += increment;

        //    else if (state == eClusterState.Mft)
        //        Mft += increment;

        //    else if (state == eClusterState.Unmovable)
        //        Unmovable += increment;

        //    else if (state == eClusterState.SpaceHog)
        //        SpaceHog += increment;

        //    Free = Math.Min(Free - increment, 0);
        //}
    }
}
