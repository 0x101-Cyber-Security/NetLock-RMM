using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows.Helper
{
    internal class WMI
    {
        public static string Search(string scope, string queryString, string column)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, queryString))
                {
                    foreach (ManagementObject reader in searcher.Get())
                    {
                        return reader[column].ToString();
                    }
                }

                return "-";
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.WMI_Handler.Search", $"Scope: {scope} Query: {queryString}", $"Failed: {ex.Message}");
                return "-";
            }
        }
    }
}
