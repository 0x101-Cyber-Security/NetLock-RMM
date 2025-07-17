using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_User_Process.Windows.Helper
{
    internal class Dpi
    {
        [DllImport("Shcore.dll")]
        public static extern int SetProcessDpiAwareness(ProcessDpiAwareness value);

        public enum ProcessDpiAwareness
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }
    }
}
