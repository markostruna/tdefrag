
namespace TDefragLib.FS
{
    /// <summary>
    /// Knwon disk types
    /// </summary>
    public enum FileSystemType
    {
        UnknownType = 0,
        Mbr = -1,
        Ntfs = 1,
        Fat12 = 12,
        Fat16 = 16,
        Fat32 = 32
    }

}
