using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragWpf.Library.FS.Ntfs;

namespace TDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("{Type}")]
    public class AttributeType
    {
        public AttributeType() : this(AttributeEnumType.Invalid)
        {
        }

        private AttributeType(AttributeEnumType type)
        {
            Type = type;
        }

        public static implicit operator AttributeType(AttributeEnumType type)
        {
            return new AttributeType(type);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public AttributeEnumType Type { get; set; }
        
        public UInt32 TypeCode { get; set; }

        public Boolean IsInvalid { get { return Type == AttributeEnumType.Invalid; } }

        public Boolean IsStandardInformation { get { return Type == AttributeEnumType.StandardInformation; } }

        public Boolean IsAttributeList { get { return Type == AttributeEnumType.AttributeList; } }

        public Boolean IsFileName { get { return Type == AttributeEnumType.FileName; } }

        public Boolean IsObjectId { get { return Type == AttributeEnumType.ObjectId; } }

        public Boolean IsSecurityDescriptor { get { return Type == AttributeEnumType.SecurityDescriptor; } }

        public Boolean IsVolumeName { get { return Type == AttributeEnumType.VolumeName; } }

        public Boolean IsVolumeInformation { get { return Type == AttributeEnumType.VolumeInformation; } }

        public Boolean IsData { get { return Type == AttributeEnumType.Data; } }

        public Boolean IsIndexRoot { get { return Type == AttributeEnumType.IndexRoot; } }

        public Boolean IsIndexAllocation { get { return Type == AttributeEnumType.IndexAllocation; } }

        public Boolean IsBitmap { get { return Type == AttributeEnumType.Bitmap; } }

        public Boolean IsReparsePoint { get { return Type == AttributeEnumType.ReparsePoint; } }

        public Boolean IsEAInformation { get { return Type == AttributeEnumType.EAInformation; } }

        public Boolean IsEA { get { return Type == AttributeEnumType.EA; } }

        public Boolean IsPropertySet { get { return Type == AttributeEnumType.PropertySet; } }

        public Boolean IsLoggedUtilityStream { get { return Type == AttributeEnumType.LoggedUtilityStream; } }

        public Boolean IsEndOfList { get { return Type == AttributeEnumType.EndOfList; } }

        public override string ToString()
        {
            return Type.ToString();
        }

        public static AttributeType Parse(BinaryReader reader)
        {
            if (reader == null)
                return null;

            AttributeType attributeType = new AttributeType();

            UInt32 val = reader.ReadUInt32();

            attributeType.TypeCode = val;

            return attributeType;
        }

        public String StreamName { get; set; }

        public static Boolean operator ==(AttributeType at, AttributeEnumType ate)
        {
            return at.Type == ate;
        }

        public static Boolean operator !=(AttributeType at, AttributeEnumType ate)
        {
            return at.Type != ate;
        }

        public static Boolean operator ==(AttributeType at, AttributeType at2)
        {
            return at.Type == at2.Type;
        }

        public static Boolean operator !=(AttributeType at, AttributeType at2)
        {
            return at.Type != at2.Type;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }
}
