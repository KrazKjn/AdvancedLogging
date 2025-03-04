using System.IO;

namespace AdvancedLogging.Interfaces
{
    public interface IDirectoryManager
    {
        DirectoryInfo GetDirectoryInfo(string path);
        FileInfo[] GetDirFiles(DirectoryInfo dir, string searchPattern);
    }
}
