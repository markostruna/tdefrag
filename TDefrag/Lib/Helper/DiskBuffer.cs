using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TDefragLib.FileSystem.Ntfs;
using TDefragLib.FS.Ntfs;

namespace TDefragLib.Helper
{
    class DiskBuffer
    {
        public DiskBuffer(UInt64 capacity)
        {
            Buffer = new Byte[capacity];
            Int32 count = (Int32)capacity;

            System.IO.Stream stream = new MemoryStream(Buffer, 0, count);
            reader = new BinaryReader(stream);
        }

        public String GetRecordType(Int64 position, Int32 length)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            return new String(reader.ReadChars(length));
        }

        public RecordHeader GetRecordHeader(Int64 position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            return RecordHeader.Parse(reader);
        }

        public FileRecordHeader GetFileRecordHeader(Int64 position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            return FileRecordHeader.Parse(reader);
        }

        public TDefragLib.FileSystem.Ntfs.Attribute GetAttribute(Int64 position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return TDefragLib.FileSystem.Ntfs.Attribute.Parse(reader);
        }

        public Byte []Buffer;
        BinaryReader reader;
    }
}
