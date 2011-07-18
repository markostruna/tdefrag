﻿using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    public class ResidentAttribute : Attribute
    {
        private ResidentAttribute()
        {
        }

        public UInt32 ValueLength
        { get; private set; }

        public UInt16 ValueOffset
        { get; private set; }

        // 0x0001 = Indexed
        public UInt16 Flags2
        { get; private set; }

        public static new ResidentAttribute Parse(BinaryReader reader)
        {
            ResidentAttribute a = new ResidentAttribute();
            a.InternalParse(reader);
            a.ValueLength = reader.ReadUInt32();
            a.ValueOffset = reader.ReadUInt16();
            a.Flags2 = reader.ReadUInt16();
            return a;
        }
    }
}
