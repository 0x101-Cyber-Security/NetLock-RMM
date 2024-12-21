using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using Global.Helper;

namespace Windows.Helper
{
    internal class Registry
    {
        public static bool HKLM_Key_Exists(string path)
        {
            try
            {
                RegistryKey regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true);
                if (regkey != null)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Registry_Handler.HKLM_Key_Exists", "Path: " + path, "Failed: " + ex.Message);
                return false;
            }
        }

        public static void HKLM_Create_Key(string path)
        {
            try
            {
                RegistryKey regkey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path, true);
                regkey.Close();
                regkey.Dispose();
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Registry_Handler.HKLM_Create_Key", "Path: " + path, "Failed: " + ex.Message);
            }
        }

        public static string HKLM_Read_Value(string path, string value)
        {
            try
            {
                RegistryKey localKey;
                if (Environment.Is64BitOperatingSystem)
                    localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                Logging.Registry("Helper.Registry_Handler.HKLM_Read_Value", "Request", "Path: " + path + " Value: " + value);
                Logging.Registry("Helper.Registry_Handler.HKLM_Read_Value", "Result", localKey.OpenSubKey(path).GetValue(value).ToString());

                return localKey.OpenSubKey(path).GetValue(value).ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Registry_Handler.HKLM_Read_Value", "Path: " + path + " Value: " + value, "Failed: " + ex.Message);
                return null;
            }
        }

        public static bool HKLM_Write_Value(string path, string value, string content)
        {
            try
            {
                RegistryKey regkey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path, true);
                {
                    regkey.SetValue(value, content, RegistryValueKind.String);
                    regkey.Close();
                    regkey.Dispose();

                    Logging.Registry("Helper.Registry_Handler.HKLM_Write_Value", "Path: " + path + " Value: " + value + " Content: " + content, "Done.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Registry_Handler.HKLM_Write_Value", "Path: " + path + " Value: " + value + " Content: " + content, "Failed: " + ex.Message);
                return false;
            }
        }

        public static bool HKLM_Delete_Value(string path, string value)
        {
            try
            {
                RegistryKey regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true);
                {
                    if (regkey == null)
                    {
                        Logging.Registry("Helper.Registry_Handler.HKLM_Delete_Value", "Path: " + path + " Value: " + value, "Done.");
                        return true;
                    }
                    else
                    {
                        regkey.DeleteValue(value);
                        Logging.Registry("Helper.Registry_Handler.HKLM_Delete_Value", "Path: " + path + " Value: " + value, "Done.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Registry_Handler.HKLM_Delete_Value", "Path: " + path + " Value: " + value, "Failed: " + ex.Message);
                return false;
            }

        }
    }
}
