using System;

namespace TDefragWpf.Library.Structures
{
    public class Excludes
    {
        public Excludes(UInt64 sLcn, UInt64 eLcn)
        {
            StartLcn = sLcn;
            EndLcn = eLcn;
        }

        public UInt64 StartLcn { get; set; }

        public UInt64 EndLcn { get; set; }
    }
}
