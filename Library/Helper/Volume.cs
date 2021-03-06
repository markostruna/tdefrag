﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TDefragLib.FS.Ntfs;
using System.Collections;
using TDefragWpf.Library.Helper;

namespace TDefragLib.Helper
{
    public class Volume : IDisposable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path"></param>
        public Volume(String path)
        {
            MountPoint = path;
            VolumeHandle = IntPtr.Zero;
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Disk: " + _mountPoint;
        }

        /// <summary>
        /// VolumeHandle
        /// </summary>
        private IntPtr VolumeHandle { get; set; }

        /// <summary>
        /// Open
        /// </summary>
        public void Open()
        {
            Close();
            VolumeHandle = UnsafeNativeMethods.OpenVolume(_mountPoint);
        }

        /// <summary>
        /// IsOpen
        /// </summary>
        public Boolean IsOpen { get { return VolumeHandle != IntPtr.Zero; } }

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
        //public FS.FileSystemType FileSystemType
        //{
        //    get { return BootSector.FileSystemType; }
        //}

        ///// <summary>
        ///// Number of clusters at begin of MFT that cannot be moved.
        ///// </summary>
        //public UInt64 MftLockedClusters;

        /// <summary>
        /// Read data from this disk starting at the given LCN
        /// </summary>
        /// <param name="logicalClusterNumber"></param>
        /// <param name="buffer">Buffer to copy the data into</param>
        /// <param name="start">Start index inside buffer</param>
        /// <param name="count">Number of bytes to read</param>
        public Boolean ReadFromCluster(UInt64 logicalClusterNumber, Byte[] buffer, int start, int count)
        {
            //Trace.WriteLine(this, String.Format("Reading: LCN={0:X8}, {1} bytes", logicalClusterNumber, count));
            Debug.Assert(buffer.Length >= count);
            Overlapped overlapped = Helper.OverlappedBuilder.Get(logicalClusterNumber);

            int bytesRead = Helper.UnsafeNativeMethods.Read(VolumeHandle, buffer, start, count, overlapped);

            if (bytesRead != count)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Load
        /// </summary>
        /// <param name="diskInfo"></param>
        /// <param name="fragments"></param>
        /// <returns></returns>
        public BitArray Load(DiskInformation diskInfo, FragmentCollection fragments)
        {
            UInt64 totalSize = fragments.TotalLength;

            // transform clusters into bytes
            totalSize *= diskInfo.BytesPerCluster;

            Byte[] bytes = new Byte[totalSize];

            foreach (Fragment fragment in fragments)
            {
                if (fragment.IsLogical)
                {
                    UInt64 lcnPosition = diskInfo.ClusterToBytes(fragment.LogicalClusterNumber);

                    UInt64 numClusters = fragment.Length;
                    Int32 numBytes = (Int32)diskInfo.ClusterToBytes(numClusters);
                    Int32 startIndex = (Int32)diskInfo.ClusterToBytes(fragment.VirtualClusterNumber);

                    ReadFromCluster(lcnPosition, bytes, startIndex, numBytes);
                }
            }

            return new BitArray(bytes);
        }

        private FS.IBootSector _bootSector;

        public FS.IBootSector BootSector
        {
            get
            {
                if (_bootSector == null)
                {
                    FS.Volume volume = new FS.Volume(VolumeHandle);

                    if (volume != null)
                    {
                        _bootSector = volume.BootSector;
                    }
                }

                return _bootSector;
            }
        }

        /// <summary>
        /// VolumeBitmap
        /// </summary>
        public UnsafeNativeMethods.BitmapData VolumeBitmap { get { return UnsafeNativeMethods.GetVolumeMap(VolumeHandle); } }

        /// <summary>
        /// NtfsVolumeData
        /// </summary>
        public NtfsVolumeDataBuffer NtfsVolumeData { get { return UnsafeNativeMethods.GetNtfsInfo(VolumeHandle); } }

        /// <summary>
        /// Close
        /// </summary>
        public void Close()
        {
            if (IsOpen)
                UnsafeNativeMethods.CloseHandle(VolumeHandle);

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
