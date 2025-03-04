using System.Text;
namespace AdvancedLogging.Tests.Utilities
{
    public class FileUtilities
    {
        public static void Touch(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Update the file's last access and modification times to the current time
                File.SetLastAccessTime(filePath, DateTime.Now);
                File.SetLastWriteTime(filePath, DateTime.Now);
            }
            else
            {
                // Create the file
                using FileStream fs = File.Create(filePath);
                // Optionally write some content to the file
                byte[] info = new UTF8Encoding(true).GetBytes("This is a newly created file.");
                fs.Write(info, 0, info.Length);
            }
        }
    }
}