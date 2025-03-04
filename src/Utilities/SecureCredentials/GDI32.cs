using System;
using System.Runtime.InteropServices;

namespace AdvancedLogging.SecureCredentials
{
    public sealed class GDI32
    {
        private GDI32() { }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
