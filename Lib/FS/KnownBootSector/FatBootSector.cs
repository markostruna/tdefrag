using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FS.KnownBootSector
{
    class FatBootSector : BaseBootSector
    {
        public FatBootSector(byte[] buffer)
            : base(buffer)
        {
            throw new NotImplementedException();
        }

        #region IBootSector Members

        public override Filesystem Filesystem
        {
            get { return Filesystem.FAT32; }
        }

        #endregion

        public override ulong Serial
        {
            get { throw new NotImplementedException(); }
        }

        public override byte MediaType
        {
            get { throw new NotImplementedException(); }
        }
    }
}
