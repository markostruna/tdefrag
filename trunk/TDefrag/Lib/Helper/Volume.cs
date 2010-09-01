using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace TDefragLib.Helper
{
    public class Volume : IDisposable
    {
        public Volume(String Path)
        {
            MountPoint = Path;
            VolumeHandle = IntPtr.Zero;
        }

        public override string ToString()
        {
            return "Disk: " + _mountPoint;
        }

        private IntPtr VolumeHandle
        {
            get;
            set;
        }

        public void Open()
        {
            Close();
            VolumeHandle = Wrapper.OpenVolume(_mountPoint);
        }

        public Boolean IsOpen
        {
            get
            {
                return VolumeHandle != IntPtr.Zero;
            }
        }

        private String _mountPoint;

        /* Example: "c:" */
        public String MountPoint
        {
            get { return _mountPoint; }
            set
            {
                String root = Path.GetPathRoot(value);
                _mountPoint = root.Replace(@"\", String.Empty);
            }
        }

        //public String MountPointSlash
        //{
        //    // Example: "c:\"
        //    get { return MountPoint + @"\"; }
        //}

        ///// <summary>
        ///// Returns the filesystem of this volume
        ///// </summary>
        //public FS.Filesystem Filesystem
        //{
        //    get { return BootSector.Filesystem; }
        //}

        ///// <summary>
        ///// Number of clusters at begin of MFT that cannot be moved.
        ///// </summary>
        //public UInt64 MftLockedClusters;

        /// <summary>
        /// Read data from this disk starting at the given LCN
        /// </summary>
        /// <param name="lcn"></param>
        /// <param name="buffer">Buffer to copy the data into</param>
        /// <param name="start">Start index inside buffer</param>
        /// <param name="count">Number of bytes to read</param>
        public Boolean ReadFromCluster(UInt64 lcn, Byte[] buffer, int start, int count)
        {
            //Trace.WriteLine(this, String.Format("Reading: LCN={0:X8}, {1} bytes", lcn, count));
            Debug.Assert(buffer.Length >= count);
            Overlapped overlapped = Helper.OverlappedBuilder.Get(lcn);

            int bytesRead = Helper.Wrapper.Read(VolumeHandle, buffer, start, count, overlapped);

            if (bytesRead != count)
            {
                return false;
            }

            return true;
        }

        //public byte[] Load(FileSystem.Ntfs.DiskInformation diskInfo, FragmentList fragments)
        //{
        //    UInt64 totalSize = fragments.TotalLength;

        //    // transform clusters into bytes
        //    totalSize *= diskInfo.BytesPerCluster;

        //    Byte[] bytes = new Byte[totalSize];

        //    foreach (Fragment fragment in fragments)
        //    {
        //        if (fragment.IsLogical)
        //        {
        //            UInt64 lcnPosition = diskInfo.ClusterToBytes(fragment.Lcn);

        //            UInt64 numClusters = fragment.Length;
        //            Int32 numBytes = (Int32)diskInfo.ClusterToBytes(numClusters);
        //            Int32 startIndex = (Int32)diskInfo.ClusterToBytes(fragment.Vcn);

        //            ReadFromCluster(lcnPosition, bytes, startIndex, numBytes);
        //        }
        //    }
        //    return bytes;
        //}

        private FS.IBootSector _bootSector;

        public FS.IBootSector BootSector
        {
            get
            {
                if (_bootSector == null)
                {
                    FS.Volume volume = new FS.Volume(VolumeHandle);
                    _bootSector = volume.BootSector;
                }
                return _bootSector;
            }
        }

        public Wrapper.BitmapData VolumeBitmap
        {
            get
            {
                return Wrapper.GetVolumeMap(VolumeHandle);
            }
        }

        public Wrapper.NTFS_VOLUME_DATA_BUFFER NtfsVolumeData
        {
            get
            {
                return Wrapper.GetNtfsInfo(VolumeHandle);
            }
        }



        public void Close()
        {
            if (IsOpen)
                Wrapper.CloseHandle(VolumeHandle);

            VolumeHandle = IntPtr.Zero;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
