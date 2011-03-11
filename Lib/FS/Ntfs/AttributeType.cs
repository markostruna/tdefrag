using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDefragLib.FileSystem.Ntfs
{
    /// <summary>
    /// Enumerator containing all atribute types
    /// </summary>
    public enum AttributeEnumType
    {
        Invalid = 0x00,                /* Not defined by Windows */
        StandardInformation = 0x10,
        AttributeList = 0x20,
        FileName = 0x30,
        ObjectId = 0x40,
        SecurityDescriptor = 0x50,
        VolumeName = 0x60,
        VolumeInformation = 0x70,
        Data = 0x80,
        IndexRoot = 0x90,
        IndexAllocation = 0xA0,
        Bitmap = 0xB0,
        ReparsePoint = 0xC0,           /* Reparse Point = Symbolic link */
        EAInformation = 0xD0,
        EA = 0xE0,
        PropertySet = 0xF0,
        LoggedUtilityStream = 0x100,
        All = 0xFF,
        EndOfList = -1
    };

    [DebuggerDisplay("{Type}")]
    public class AttributeType
    {
        private AttributeEnumType m_attributeType;

        public AttributeType()
            : this(AttributeEnumType.Invalid)
        {
        }

        private AttributeType(AttributeEnumType type)
        {
            m_attributeType = type;
        }

        public static implicit operator AttributeType(AttributeEnumType type)
        {
            return new AttributeType(type);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public AttributeEnumType Type
        {
            get
            {
                return m_attributeType;
            }
        }

        public Boolean IsInvalid
        { private set { } get { return m_attributeType == AttributeEnumType.Invalid; } }

        public Boolean IsStandardInformation
        { private set { } get { return m_attributeType == AttributeEnumType.StandardInformation; } }

        public Boolean IsAttributeList
        { private set { } get { return m_attributeType == AttributeEnumType.AttributeList; } }

        public Boolean IsFileName
        { private set { } get { return m_attributeType == AttributeEnumType.FileName; } }

        public Boolean IsObjectId
        { private set { } get { return m_attributeType == AttributeEnumType.ObjectId; } }

        public Boolean IsSecurityDescriptor
        { private set { } get { return m_attributeType == AttributeEnumType.SecurityDescriptor; } }

        public Boolean IsVolumeName
        { private set { } get { return m_attributeType == AttributeEnumType.VolumeName; } }

        public Boolean IsVolumeInformation
        { private set { } get { return m_attributeType == AttributeEnumType.VolumeInformation; } }

        public Boolean IsData
        { private set { } get { return m_attributeType == AttributeEnumType.Data; } }

        public Boolean IsIndexRoot
        { private set { } get { return m_attributeType == AttributeEnumType.IndexRoot; } }

        public Boolean IsIndexAllocation
        { private set { } get { return m_attributeType == AttributeEnumType.IndexAllocation; } }

        public Boolean IsBitmap
        { private set { } get { return m_attributeType == AttributeEnumType.Bitmap; } }

        public Boolean IsReparsePoint
        { private set { } get { return m_attributeType == AttributeEnumType.ReparsePoint; } }

        public Boolean IsEAInformation
        { private set { } get { return m_attributeType == AttributeEnumType.EAInformation; } }

        public Boolean IsEA
        { private set { } get { return m_attributeType == AttributeEnumType.EA; } }

        public Boolean IsPropertySet
        { private set { } get { return m_attributeType == AttributeEnumType.PropertySet; } }

        public Boolean IsLoggedUtilityStream
        { private set { } get { return m_attributeType == AttributeEnumType.LoggedUtilityStream; } }

        public Boolean IsEndOfList
        { private set { } get { return m_attributeType == AttributeEnumType.EndOfList; } }

        public override string ToString()
        {
            return m_attributeType.ToString();
        }

        public static AttributeType Parse(BinaryReader reader)
        {
            AttributeEnumType retValue = AttributeEnumType.Invalid;

            // http://msdn.microsoft.com/en-us/library/bb470038%28VS.85%29.aspx
            // It is a DWORD containing enumerated values
            UInt32 val = reader.ReadUInt32();

            // the attribute type code may contain a special value -1 (or 0xFFFFFFFF) which 
            // may be present as a filler to mark the end of an attribute list. In that case,
            // the rest of the attribute should be ignored, and the attribute list should not
            // be scanned further.
            switch (val)
            {
                case 0xFFFFFFFF:
                    retValue = AttributeEnumType.EndOfList;
                    break;
                case 0x00: 
                    retValue = AttributeEnumType.Invalid;
                    break;
                case 0x10: 
                    retValue = AttributeEnumType.StandardInformation;
                    break;
                case 0x20: 
                    retValue = AttributeEnumType.AttributeList;
                    break;
                case 0x30: 
                    retValue = AttributeEnumType.FileName;
                    break;
                case 0x40: 
                    retValue = AttributeEnumType.ObjectId;
                    break;
                case 0x50: 
                    retValue = AttributeEnumType.SecurityDescriptor;
                    break;
                case 0x60: 
                    retValue = AttributeEnumType.VolumeName;
                    break;
                case 0x70: 
                    retValue = AttributeEnumType.VolumeInformation;
                    break;
                case 0x80: 
                    retValue = AttributeEnumType.Data;
                    break;
                case 0x90: 
                    retValue = AttributeEnumType.IndexRoot;
                    break;
                case 0xA0: 
                    retValue = AttributeEnumType.IndexAllocation;
                    break;
                case 0xB0: 
                    retValue = AttributeEnumType.Bitmap;
                    break;
                case 0xC0: 
                    retValue = AttributeEnumType.ReparsePoint;
                    break;
                case 0xD0: 
                    retValue = AttributeEnumType.EAInformation;
                    break;
                case 0xE0: 
                    retValue = AttributeEnumType.EA;
                    break;
                case 0xF0: 
                    retValue = AttributeEnumType.PropertySet;
                    break;
                case 0x100: 
                    retValue = AttributeEnumType.LoggedUtilityStream;
                    break;
                case 0xFF: 
                    retValue = AttributeEnumType.All;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new AttributeType(retValue);
        }

        public String StreamName
        {
            set { }
            get
            {
                switch (m_attributeType)
                {
                    case AttributeEnumType.StandardInformation:
                        return ("$STANDARD_INFORMATION");
                    case AttributeEnumType.AttributeList:
                        return ("$ATTRIBUTE_LIST");
                    case AttributeEnumType.FileName:
                        return ("$FILE_NAME");
                    case AttributeEnumType.ObjectId:
                        return ("$OBJECT_ID");
                    case AttributeEnumType.SecurityDescriptor:
                        return ("$SECURITY_DESCRIPTOR");
                    case AttributeEnumType.VolumeName:
                        return ("$VOLUME_NAME");
                    case AttributeEnumType.VolumeInformation:
                        return ("$VOLUME_INFORMATION");
                    case AttributeEnumType.Data:
                        return ("$DATA");
                    case AttributeEnumType.IndexRoot:
                        return ("$INDEX_ROOT");
                    case AttributeEnumType.IndexAllocation:
                        return ("$INDEX_ALLOCATION");
                    case AttributeEnumType.Bitmap:
                        return ("$BITMAP");
                    case AttributeEnumType.ReparsePoint:
                        return ("$REPARSE_POINT");
                    case AttributeEnumType.EAInformation:
                        return ("$EA_INFORMATION");
                    case AttributeEnumType.EA:
                        return ("$EA");
                    case AttributeEnumType.PropertySet:
                        return ("$PROPERTY_SET");               /* guess, not documented */
                    case AttributeEnumType.LoggedUtilityStream:
                        return ("$LOGGED_UTILITY_STREAM");
                    case AttributeEnumType.Invalid:
                        return String.Empty;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public static Boolean operator ==(AttributeType at, AttributeEnumType ate)
        {
            return at.m_attributeType == ate;
        }

        public static Boolean operator !=(AttributeType at, AttributeEnumType ate)
        {
            return at.m_attributeType != ate;
        }

        public static Boolean operator ==(AttributeType at, AttributeType at2)
        {
            return at.m_attributeType == at2.m_attributeType;
        }

        public static Boolean operator !=(AttributeType at, AttributeType at2)
        {
            return at.m_attributeType != at2.m_attributeType;
        }

        public override int GetHashCode()
        {
            return m_attributeType.GetHashCode();
        }

    }
}
