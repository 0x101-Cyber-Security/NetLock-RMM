﻿using Global.Helper;
using Linux.Helper;
using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacOS.Helper
{
    internal class MacOS
    {
        public static double Disks_Convert_Size_To_GB(string size)
        {
            try
            {
                double sizeInGB = 0;

                // Check for size specifications in GB
                if (size.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("G", "").Trim());
                }
                // Check for size specifications in MB (conversion to GB)
                else if (size.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("M", "").Trim()) / 1024; // Umrechnung von MB in GB
                }
                // Check for size specifications in TB (conversion to GB)
                else if (size.EndsWith("T", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("T", "").Trim()) * 1024; // Umrechnung von TB in GB
                }
                // Check for size specifications in bytes (conversion to GB)
                else if (size.EndsWith("B", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("B", "").Trim()) / (1024 * 1024 * 1024); // Umrechnung von Bytes in GB
                }
                // Case without unit, default assumption: size in GB
                else if (double.TryParse(size, out double sizeValue))
                {
                    sizeInGB = sizeValue;
                }
                else
                {
                    throw new ArgumentException("Invalid size format.");
                }

                // Rounding to 2 decimal places for a better display
                return Math.Round(sizeInGB, 2);
            }
            catch (Exception ex)
            { 
                Logging.Error("MacOS.Helper.MacOS.Disks_Convert_Size_To_GB", "Error converting size to GB", ex.ToString());
                return 0;
            }
        }

        public static double Disks_Convert_Size_To_GB_Two(long sizeInBytes)
        {
            try
            {
                // Conversion from bytes to GB
                double sizeInGB = (double)sizeInBytes / (1024 * 1024 * 1024);

                // Rounding to 2 decimal places for a better display
                return Math.Round(sizeInGB, 2);
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.MacOS.Disks_Convert_Size_To_GB_Two", "Error converting size to GB", ex.ToString());
                return 0;
            }
        }

        public static void CreateInstallerService()
        {
            string serviceScript = "IyEvYmluL2Jhc2gNCiMgTmV0TG9jayBSTU0gQWdlbnQgSW5zdGFsbGVyIG1hY09TIExhdW5jaERhZW1vbiBTZXR1cA0KDQpQTElTVF9QQVRIPSIvTGlicmFyeS9MYXVuY2hEYWVtb25zL2NvbS5uZXRsb2NrLnJtbS5pbnN0YWxsZXIucGxpc3QiDQpFWEVDVVRBQkxFPSIvdG1wL25ldGxvY2sgcm1tL2luc3RhbGxlci9OZXRMb2NrX1JNTV9BZ2VudF9JbnN0YWxsZXIiDQpBUkcxPSJmaXgiDQpBUkcyPSIvdmFyLzB4MTAxIEN5YmVyIFNlY3VyaXR5L05ldExvY2sgUk1NL0NvbW0gQWdlbnQvc2VydmVyX2NvbmZpZy5qc29uIg0KTE9HX1BBVEg9Ii92YXIvbG9nL25ldGxvY2tfcm1tX2luc3RhbGxlci5sb2ciDQpFUlJPUl9MT0dfUEFUSD0iL3Zhci9sb2cvbmV0bG9ja19ybW1faW5zdGFsbGVyX2Vycm9yLmxvZyINCg0KIyBQcsO8ZmVuLCBvYiBkYXMgU2tyaXB0IG1pdCBSb290LVJlY2h0ZW4gYXVzZ2Vmw7xocnQgd2lyZA0KaWYgW1sgJEVVSUQgLW5lIDAgXV07IHRoZW4NCiAgIGVjaG8gIlRoaXMgc2NyaXB0IG11c3QgYmUgZXhlY3V0ZWQgYXMgcm9vdCEgKHN1ZG8gJDApIg0KICAgZXhpdCAxDQpmaQ0KDQojIDEuIFBsaXN0LURhdGVpIGVyc3RlbGxlbg0KZWNobyAiQ3JlYXRpbmcgTGF1bmNoRGFlbW9uLVBsaXN0IHVuZGVyICRQTElTVF9QQVRIIC4uLiINCmNhdCA8PEVPRiA+ICIkUExJU1RfUEFUSCINCjw/eG1sIHZlcnNpb249IjEuMCIgZW5jb2Rpbmc9IlVURi04Ij8+DQo8IURPQ1RZUEUgcGxpc3QgUFVCTElDICItLy9BcHBsZS8vRFREIFBMSVNUIDEuMC8vRU4iICJodHRwOi8vd3d3LmFwcGxlLmNvbS9EVERzL1Byb3BlcnR5TGlzdC0xLjAuZHRkIj4NCjxwbGlzdCB2ZXJzaW9uPSIxLjAiPg0KICA8ZGljdD4NCiAgICA8a2V5PkxhYmVsPC9rZXk+DQogICAgPHN0cmluZz5jb20ubmV0bG9jay5ybW0uaW5zdGFsbGVyPC9zdHJpbmc+DQoNCiAgICA8a2V5PlByb2dyYW1Bcmd1bWVudHM8L2tleT4NCiAgICA8YXJyYXk+DQogICAgICA8c3RyaW5nPiRFWEVDVVRBQkxFPC9zdHJpbmc+DQogICAgICA8c3RyaW5nPiRBUkcxPC9zdHJpbmc+DQogICAgICA8c3RyaW5nPiRBUkcyPC9zdHJpbmc+DQogICAgPC9hcnJheT4NCg0KICAgIDxrZXk+UnVuQXRMb2FkPC9rZXk+DQogICAgPHRydWUvPg0KDQogICAgPGtleT5LZWVwQWxpdmU8L2tleT4NCiAgICA8ZmFsc2UvPg0KDQogICAgPGtleT5TdGFuZGFyZE91dFBhdGg8L2tleT4NCiAgICA8c3RyaW5nPiRMT0dfUEFUSDwvc3RyaW5nPg0KDQogICAgPGtleT5TdGFuZGFyZEVycm9yUGF0aDwva2V5Pg0KICAgIDxzdHJpbmc+JEVSUk9SX0xPR19QQVRIPC9zdHJpbmc+DQogIDwvZGljdD4NCjwvcGxpc3Q+DQpFT0YNCg0KIyAyLiBTZXQgdGhlIGNvcnJlY3QgYXV0aG9yaXNhdGlvbnMNCmVjaG8gIlNldCBhdXRob3Jpc2F0aW9ucyBmb3IgdGhlIHBsaXN0IGZpbGUgLi4uIg0KY2htb2QgNjQ0ICIkUExJU1RfUEFUSCINCmNob3duIHJvb3Q6d2hlZWwgIiRQTElTVF9QQVRIIg0KDQojIDMuIEVuc3VyZSB0aGF0IHRoZSBleGVjdXRhYmxlIGZpbGUgZXhpc3RzIGFuZCBpcyBleGVjdXRhYmxlDQppZiBbWyAtZiAiJEVYRUNVVEFCTEUiIF1dOyB0aGVuDQogICAgZWNobyAiU2V0IHBlcm1pc3Npb25zIGZvciB0aGUgZXhlY3V0YWJsZSBmaWxlIC4uLiINCiAgICBjaG1vZCAreCAiJEVYRUNVVEFCTEUiDQplbHNlDQogICAgZWNobyAiV0FSTklORzogVGhlIGV4ZWN1dGFibGUgZmlsZSAkRVhFQ1VUQUJMRSBkb2VzIG5vdCBleGlzdCEiDQpmaQ==";

            // Execute the scripts
            string serviceScriptOutput = Zsh.Execute_Script("InstallerService", true, serviceScript);
            Logging.Debug("InstallerService", "CommAgent script output", serviceScript);
        }
    }
}
