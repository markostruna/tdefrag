using System;

namespace TDefragWpf.Library.FS.Ntfs
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

    public class AttributeEnumTypeEntry
    {
        public AttributeEnumTypeEntry (UInt32 v, AttributeEnumType t, String n)
        {
            TypeCode = v;
            Type = t;
            StreamName = n;
        }
       
        public UInt32 TypeCode { get; set; }
        
        public AttributeEnumType Type { get; set; }
        
        public String StreamName { get; set; }
    }
}
