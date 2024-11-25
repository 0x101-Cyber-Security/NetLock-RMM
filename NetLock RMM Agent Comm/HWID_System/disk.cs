using System.IO;
using System.Management;

namespace _x101.HWID_System
{
    internal class DiskId
    {
        [System.Reflection.ObfuscationAttribute(Feature = "Virtualization", Exclude = false)]

        public static string GetDiskId()
        {
            return GetDiskId("");
        }
        private static string GetDiskId(string diskLetter)
        {
            //Find first drive
            if (string.IsNullOrEmpty(diskLetter))
            {
                foreach (var compDrive in DriveInfo.GetDrives())
                {
                    if (compDrive.IsReady)
                    {
                        diskLetter = compDrive.RootDirectory.ToString();
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(diskLetter) && diskLetter.EndsWith(":\\"))
            {
                //C:\ -> C
                diskLetter = diskLetter.Substring(0, diskLetter.Length - 2);
            }
            var disk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + diskLetter + @":""");
            disk.Get();

            var volumeSerial = disk["VolumeSerialNumber"].ToString();
            disk.Dispose();

            return volumeSerial;
        }
    }
}