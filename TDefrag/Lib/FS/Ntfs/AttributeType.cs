﻿using System;
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
    public enum AttributeTypeEnum
    {
        AttributeInvalid = 0x00,                /* Not defined by Windows */
        AttributeStandardInformation = 0x10,
        AttributeAttributeList = 0x20,
        AttributeFileName = 0x30,
        AttributeObjectId = 0x40,
        AttributeSecurityDescriptor = 0x50,
        AttributeVolumeName = 0x60,
        AttributeVolumeInformation = 0x70,
        AttributeData = 0x80,
        AttributeIndexRoot = 0x90,
        AttributeIndexAllocation = 0xA0,
        AttributeBitmap = 0xB0,
        AttributeReparsePoint = 0xC0,           /* Reparse Point = Symbolic link */
        AttributeEAInformation = 0xD0,
        AttributeEA = 0xE0,
        AttributePropertySet = 0xF0,
        AttributeLoggedUtilityStream = 0x100,
        AttributeAll = 0xFF,
        AttributeEndOfList = -1
    };

    [DebuggerDisplay("{Type}")]
    public class AttributeType
    {
        private AttributeTypeEnum m_attributeType;

        public AttributeType()
            : this(AttributeTypeEnum.AttributeInvalid)
        {
        }

        private AttributeType(AttributeTypeEnum type)
        {
            m_attributeType = type;
        }

        public static implicit operator AttributeType(AttributeTypeEnum type)
        {
            return new AttributeType(type);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public AttributeTypeEnum Type
        {
            get
            {
                return m_attributeType;
            }
        }

        public Boolean IsInvalid
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeInvalid; } }

        public Boolean IsStandardInformation
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeStandardInformation; } }

        public Boolean IsAttributeList
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeAttributeList; } }

        public Boolean IsFileName
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeFileName; } }

        public Boolean IsObjectId
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeObjectId; } }

        public Boolean IsSecurityDescriptor
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeSecurityDescriptor; } }

        public Boolean IsVolumeName
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeVolumeName; } }

        public Boolean IsVolumeInformation
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeVolumeInformation; } }

        public Boolean IsData
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeData; } }

        public Boolean IsIndexRoot
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeIndexRoot; } }

        public Boolean IsIndexAllocation
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeIndexAllocation; } }

        public Boolean IsBitmap
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeBitmap; } }

        public Boolean IsReparsePoint
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeReparsePoint; } }

        public Boolean IsEAInformation
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeEAInformation; } }

        public Boolean IsEA
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeEA; } }

        public Boolean IsPropertySet
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributePropertySet; } }

        public Boolean IsLoggedUtilityStream
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeLoggedUtilityStream; } }

        public Boolean IsEndOfList
        { private set { } get { return m_attributeType == AttributeTypeEnum.AttributeEndOfList; } }

        public override string ToString()
        {
            return m_attributeType.ToString();
        }

        public static AttributeType Parse(BinaryReader reader)
        {
            AttributeTypeEnum retValue = AttributeTypeEnum.AttributeInvalid;

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
                    retValue = AttributeTypeEnum.AttributeEndOfList;
                    break;
                case 0x00: 
                    retValue = AttributeTypeEnum.AttributeInvalid;
                    break;
                case 0x10: 
                    retValue = AttributeTypeEnum.AttributeStandardInformation;
                    break;
                case 0x20: 
                    retValue = AttributeTypeEnum.AttributeAttributeList;
                    break;
                case 0x30: 
                    retValue = AttributeTypeEnum.AttributeFileName;
                    break;
                case 0x40: 
                    retValue = AttributeTypeEnum.AttributeObjectId;
                    break;
                case 0x50: 
                    retValue = AttributeTypeEnum.AttributeSecurityDescriptor;
                    break;
                case 0x60: 
                    retValue = AttributeTypeEnum.AttributeVolumeName;
                    break;
                case 0x70: 
                    retValue = AttributeTypeEnum.AttributeVolumeInformation;
                    break;
                case 0x80: 
                    retValue = AttributeTypeEnum.AttributeData;
                    break;
                case 0x90: 
                    retValue = AttributeTypeEnum.AttributeIndexRoot;
                    break;
                case 0xA0: 
                    retValue = AttributeTypeEnum.AttributeIndexAllocation;
                    break;
                case 0xB0: 
                    retValue = AttributeTypeEnum.AttributeBitmap;
                    break;
                case 0xC0: 
                    retValue = AttributeTypeEnum.AttributeReparsePoint;
                    break;
                case 0xD0: 
                    retValue = AttributeTypeEnum.AttributeEAInformation;
                    break;
                case 0xE0: 
                    retValue = AttributeTypeEnum.AttributeEA;
                    break;
                case 0xF0: 
                    retValue = AttributeTypeEnum.AttributePropertySet;
                    break;
                case 0x100: 
                    retValue = AttributeTypeEnum.AttributeLoggedUtilityStream;
                    break;
                case 0xFF: 
                    retValue = AttributeTypeEnum.AttributeAll;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new AttributeType(retValue);
        }

        public String GetStreamTypeName()
        {
            switch (m_attributeType)
            {
                case AttributeTypeEnum.AttributeStandardInformation:
                    return ("$STANDARD_INFORMATION");
                case AttributeTypeEnum.AttributeAttributeList:
                    return ("$ATTRIBUTE_LIST");
                case AttributeTypeEnum.AttributeFileName:
                    return ("$FILE_NAME");
                case AttributeTypeEnum.AttributeObjectId:
                    return ("$OBJECT_ID");
                case AttributeTypeEnum.AttributeSecurityDescriptor:
                    return ("$SECURITY_DESCRIPTOR");
                case AttributeTypeEnum.AttributeVolumeName:
                    return ("$VOLUME_NAME");
                case AttributeTypeEnum.AttributeVolumeInformation:
                    return ("$VOLUME_INFORMATION");
                case AttributeTypeEnum.AttributeData:
                    return ("$DATA");
                case AttributeTypeEnum.AttributeIndexRoot:
                    return ("$INDEX_ROOT");
                case AttributeTypeEnum.AttributeIndexAllocation:
                    return ("$INDEX_ALLOCATION");
                case AttributeTypeEnum.AttributeBitmap:
                    return ("$BITMAP");
                case AttributeTypeEnum.AttributeReparsePoint:
                    return ("$REPARSE_POINT");
                case AttributeTypeEnum.AttributeEAInformation:
                    return ("$EA_INFORMATION");
                case AttributeTypeEnum.AttributeEA:
                    return ("$EA");
                case AttributeTypeEnum.AttributePropertySet:
                    return ("$PROPERTY_SET");               /* guess, not documented */
                case AttributeTypeEnum.AttributeLoggedUtilityStream:
                    return ("$LOGGED_UTILITY_STREAM");
                case AttributeTypeEnum.AttributeInvalid:
                    return String.Empty;
                default:
                    throw new NotSupportedException();
            }
        }

        public static Boolean operator ==(AttributeType at, AttributeTypeEnum ate)
        {
            return at.m_attributeType == ate;
        }

        public static Boolean operator !=(AttributeType at, AttributeTypeEnum ate)
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
