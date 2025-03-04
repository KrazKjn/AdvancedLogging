using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using System;
using System.IO;

namespace AdvancedLogging.Utilities
{
    public class DirectoryManager : IDirectoryManager
    {
        public DirectoryManager()
        {

        }
        #region IO Methods
        public DirectoryInfo GetDirectoryInfo(string path)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { path }))
            {
                try
                {
                    return new System.IO.DirectoryInfo(path);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { path }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public FileInfo[] GetDirFiles(DirectoryInfo dir, string searchPattern)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dir, searchPattern }))
            {
                try
                {
                    return dir.GetFiles(searchPattern);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dir, searchPattern }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
    }

}
