using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using static Global.Online_Mode.Handler;
using NetLock_RMM_Agent_Comm;
using System.Text.Json;
using Global.Helper;
using Linux.Helper;
using System.Runtime.InteropServices;

namespace Global.Initialization
{
    internal class Version
    {
        public static async Task<bool> Check_Version()
        {
            try
            {
                //Create JSON
                Device_Identity identity = new Device_Identity
                {
                    agent_version = Application_Settings.version,
                    package_guid = Configuration.Agent.package_guid,
                    device_name = Configuration.Agent.device_name,
                    location_guid = Configuration.Agent.location_guid,
                    tenant_guid = Configuration.Agent.tenant_guid,
                    access_key = Device_Worker.access_key,
                    hwid = Configuration.Agent.hwid,
                    ip_address_internal = string.Empty,
                    operating_system = string.Empty,
                    domain = string.Empty,
                    antivirus_solution = string.Empty,
                    firewall_status = string.Empty,
                    architecture = string.Empty,
                    last_boot = string.Empty,
                    timezone = string.Empty,
                    cpu = string.Empty,
                    mainboard = string.Empty,
                    gpu = string.Empty,
                    ram = string.Empty,
                    tpm = string.Empty,
                };

                // Create the object that contains the device_identity object
                var jsonObject = new { device_identity = identity };

                // Serialize the object to a JSON string
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Debug("Initialization.Version_Handler.Check_Version", "json", json);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package-Guid", Configuration.Agent.package_guid);

                    Logging.Debug("Initialization.Version_Handler.Check_Version", "communication_server", Device_Worker.communication_server + "/Agent/Windows/Verify_Device");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Configuration.Agent.http_https + Device_Worker.communication_server + "/Agent/Windows/Check_Version", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Debug("Initialization.Version_Handler.Check_Version", "result", result);

                        // Parse the JSON response
                        if (result == "identical")
                        {
                            Logging.Debug("Initialization.Version_Handler.Check_Version", "up2date?", "true");
                            return true;
                        }
                        else if (result == "different")
                        {
                            Logging.Debug("Initialization.Version_Handler.Check_Version", "up2date?", "false");
                            return false;
                        }
                        else //something mostelikely went wrong. Returning true to avoid any issues
                        {
                            Logging.Debug("Initialization.Version_Handler.Check_Version", "Something mostelikely went wrong. Returning true to avoid any issues", "true");
                            return true;
                        }
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Debug("Initialization.Version_Handler.Check_Version", "request", "Request failed: " + response.Content);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Version_Handler.Check_Version", "General error", ex.ToString());
                return true;
            }
        }

        public static async Task Update()
        {
            try
            {
                // Delete the temp directory
                if (Directory.Exists(Application_Paths.c_temp_netlock_dir))
                    Directory.Delete(Application_Paths.c_temp_netlock_dir, true);

                // Create the temp directory
                if (!Directory.Exists(Application_Paths.c_temp_netlock_dir))
                    Directory.CreateDirectory(Application_Paths.c_temp_netlock_dir);

                // Download the new version
                // Check OS & Architecture
                string installer_package_url = string.Empty;

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Detecting OS & Architecture.");
                Logging.Debug("Main", "Detecting OS & Architecture", "");

                var arch = RuntimeInformation.ProcessArchitecture;

                if (OperatingSystem.IsWindows())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows x64 detected.");
                        Logging.Debug("Main", "Windows x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_winx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows ARM64 detected.");
                        Logging.Debug("Main", "Windows ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_winarm64;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux x64 detected.");
                        Logging.Debug("Main", "Linux x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_linuxx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux ARM64 detected.");
                        Logging.Debug("Main", "Linux ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_linuxarm64;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS x64 detected.");
                        Logging.Debug("Main", "MacOS x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_osx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS ARM64 detected.");
                        Logging.Debug("Main", "MacOS ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_osxarm64;
                    }
                }
                else
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Unsupported OS & Architecture.");
                    Logging.Error("Main", "Unsupported OS & Architecture", "");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                Logging.Debug("Initialization.Version_Handler.Update", "Downloading new version", "true");
                await Http.DownloadFileAsync(Configuration.Agent.ssl, Device_Worker.update_server + installer_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path), Configuration.Agent.package_guid);

                // Get the hash of the new version
                Logging.Debug("Initialization.Version_Handler.Update", "Getting hash of new version", "true");
                string hash = await Http.GetHashAsync(Configuration.Agent.ssl, Device_Worker.update_server + installer_package_url + ".sha512", Configuration.Agent.package_guid);

                // Get the hash of the downloaded file
                Logging.Debug("Initialization.Version_Handler.Update", "Getting hash of downloaded file", "true");
                string downloaded_hash = IO.Get_SHA512(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path));

                // Compare the hashes
                if (hash != downloaded_hash)
                {
                    Logging.Debug("Initialization.Version_Handler.Update", "Hash check", "Bad. Canceling update.");
                    return;
                }
                else
                    Logging.Debug("Initialization.Version_Handler.Update", "Hash check", "Good");

                // Extract the new version
                Logging.Debug("Initialization.Version_Handler.Update", "Extracting new version", "true");
                ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path), Application_Paths.c_temp_installer_dir, true);

                // Start the installer
                Logging.Debug("Initialization.Version_Handler.Update", "Starting installer", "true");

                // Set permissions on linux & macos
                if (OperatingSystem.IsLinux())
                    Bash.Execute_Script("Installer Permissions", false, $"chmod +x \"{Application_Paths.c_temp_installer_path}\"");
                else if (OperatingSystem.IsMacOS())
                    Bash.Execute_Script("Installer Permissions", false, $"chmod +x \"{Application_Paths.c_temp_installer_path}\"");

                // Run the installer
                if (OperatingSystem.IsWindows())
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: Windows");
                    Logging.Debug("Main", "Run installer", "Windows");
                    Process installer = new Process();
                    installer.StartInfo.FileName = Application_Paths.c_temp_installer_path;
                    installer.StartInfo.ArgumentList.Add("fix");
                    installer.StartInfo.ArgumentList.Add(Application_Paths.program_data_server_config_json);
                    installer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    installer.Start();
                    await installer.WaitForExitAsync();
                }
                else if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: Linux");
                    Logging.Debug("Main", "Run installer", "Linux");

                    // Create installer
                    Linux.Helper.Linux.CreateInstallerService();

                    Process installer = new Process();
                    installer.StartInfo.FileName = "/bin/bash";
                    installer.StartInfo.ArgumentList.Add("-c");
                    installer.StartInfo.ArgumentList.Add($"systemctl start netlock-rmm-agent-installer");
                    installer.StartInfo.RedirectStandardOutput = true;
                    installer.StartInfo.RedirectStandardError = true;
                    installer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    installer.Start();
                    await installer.WaitForExitAsync();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: MacOS");
                    Logging.Debug("Main", "Run installer", "MacOS");

                    // Create installer service
                    MacOS.Helper.MacOS.CreateInstallerService();

                    Process installer = new Process();
                    installer.StartInfo.FileName = "zsh";
                    installer.StartInfo.ArgumentList.Add("-c");
                    installer.StartInfo.ArgumentList.Add($"sudo launchctl load -w /Library/LaunchDaemons/com.netlock.rmm.installer.plist");
                    installer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    installer.StartInfo.RedirectStandardOutput = true;
                    installer.StartInfo.RedirectStandardError = true;
                    installer.Start();
                    await installer.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Version_Handler.Update", "General error", ex.ToString());
            }
        }
    }
}
