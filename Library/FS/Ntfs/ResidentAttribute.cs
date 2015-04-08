using System;
using System.IO;

namespace TDefragLib.FileSystem.Ntfs
{
    public class ResidentAttribute : Attribute
    {
        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static new ResidentAttribute Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            ResidentAttribute a = new ResidentAttribute();

            a.InternalParse(reader);

            a.ValueLength = reader.ReadUInt32();
            a.ValueOffset = reader.ReadUInt16();
            a.Flags2 = reader.ReadUInt16();
            
            return a;
        }

        /// <summary>
        /// ValueLength
        /// </summary>
        public UInt32 ValueLength { get; set; }

        /// <summary>
        /// ValueOffset
        /// </summary>
        public UInt16 ValueOffset { get; set; }

        /// <summary>
        /// 0x0001 = Indexed
        /// </summary>
        public UInt16 Flags2 { get; set; }

    }
}
