using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragLib.Helper;

namespace TDefragLib.FS.Ntfs
{
    class Helper
    {
        /// <summary>
        /// BinaryReader
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static BinaryReader BinaryReader(DiskBuffer buffer)
        {
            return BinaryReader(buffer, 0);
        }

        /// <summary>
        /// BinaryReader
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static BinaryReader BinaryReader(DiskBuffer buffer, Int64 offset)
        {
            Int64 count = buffer.Buffer.Length - offset;
            Debug.Assert(count > 0);

            System.IO.Stream stream = new MemoryStream(buffer.Buffer, (int)offset, (int)count);
            BinaryReader reader = new BinaryReader(stream);

            return reader;
        }

        /// <summary>
        /// ParseString
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static String ParseString(BinaryReader reader, int length)
        {
            StringBuilder sb = new StringBuilder();
            
            for (int ii = 0; ii < length; ii++)
            {
                UInt16 i = reader.ReadUInt16();
                
                sb.Append((Char)i);
            }
            
            return sb.ToString();
        }
    }
}
