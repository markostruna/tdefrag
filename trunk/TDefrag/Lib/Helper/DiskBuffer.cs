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

        public Int64 ReaderPosition
        {
            set { reader.BaseStream.Seek(value, SeekOrigin.Begin); }
            get { return reader.BaseStream.Position; }
        }

        public String GetString(Int64 position, Int32 length)
        {
            ReaderPosition = position;
            return new String(reader.ReadChars(length));
        }

        public RecordHeader GetRecordHeader(Int64 position)
        {
            ReaderPosition = position;
            return RecordHeader.Parse(reader);
        }

        public FileRecordHeader GetFileRecordHeader(Int64 position)
        {
            ReaderPosition = position;
            return FileRecordHeader.Parse(reader);
        }

        public FileNameAttribute GetFileNameAttribute(Int64 position)
        {
            ReaderPosition = position;
            return FileNameAttribute.Parse(reader);
        }

        public TDefragLib.FileSystem.Ntfs.Attribute GetAttribute(Int64 position)
        {
            ReaderPosition = position;
            return TDefragLib.FileSystem.Ntfs.Attribute.Parse(reader);
        }

        public NonResidentAttribute GetNonResidentAttribute(Int64 position)
        {
            ReaderPosition = position;
            return NonResidentAttribute.Parse(reader);
        }

        public ResidentAttribute GetResidentAttribute(Int64 position)
        {
            ReaderPosition = position;
            return ResidentAttribute.Parse(reader);
        }

        public StandardInformation GetStandardInformation(Int64 position)
        {
            ReaderPosition = position;
            return StandardInformation.Parse(reader);
        }

        public AttributeList GetAttributeList(Int64 position)
        {
            ReaderPosition = position;
            return AttributeList.Parse(reader);
        }

        public UInt16 GetUInt16(Int64 position)
        {
            ReaderPosition = position;
            return reader.ReadUInt16();
        }

        public Byte[] GetBytes(Int64 position, Int32 count)
        {
            ReaderPosition = position;
            return reader.ReadBytes(count);
        }

        public Byte[] Buffer;
        BinaryReader reader;
    }
}
