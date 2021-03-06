﻿using System;

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

        public override FileSystemType Filesystem
        {
            get { return FileSystemType.Fat32; }
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
