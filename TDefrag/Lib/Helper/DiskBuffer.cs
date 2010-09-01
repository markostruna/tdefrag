using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.Helper
{
    class DiskBuffer
    {
        public DiskBuffer(UInt64 capacity)
        {
            Buffer = new Byte[capacity];
        }

        public Byte []Buffer;
    }
}
