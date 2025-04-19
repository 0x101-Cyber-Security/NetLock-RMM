using MySqlConnector;
using NetLock_RMM_Server;
using NetLock_RMM_Server.MySQL;
using System;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Principal;
using System.IO.Compression;
using NetLock_RMM_Server.Files.Compiler;
using System.Diagnostics.Eventing.Reader;
using NetLock_RMM_Server.Configuration;

namespace NetLock_RMM_Server.Files
{
    public class Command_Entity
    {
        public string? command { get; set; }
        public string? path { get; set; }
        public string? name { get; set; }
        public string? guid { get; set; }
    }

    public class Download_JSON
    {
        public string? guid{ get; set; }
    }

    public class Handler
    {
        // Verify_Api_Key method
        public static async Task<bool> Verify_Api_Key(string files_api_key)
        {
            try
            {
                bool api_key_exists = false;

                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM settings WHERE files_api_key = @files_api_key;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@files_api_key", files_api_key);

                    Logging.Handler.Debug("Files.Verify_Api_Key", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                            api_key_exists = true;
                        else
                            api_key_exists = false;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Verify_Api_Key", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    conn.Close();
                }

                return api_key_exists;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Verify_Api_Key", "general_error", ex.ToString());
                return false;
            }
        }

        public static async Task<string> Register_File(string file_path, string tenant_guid, string location_guid, string device_name)
        {
            try
            {
                // Bereinigt `file_path`
                string fullFilePath = Path.GetFullPath(file_path);
                string relativeFilePath = Path.GetRelativePath(Application_Paths._private_files, fullFilePath);

                // Entfernt den Dateinamen, um nur den Verzeichnispfad zu erhalten
                string relativeDirectoryPath = Path.GetDirectoryName(relativeFilePath);

                // Extract file information
                string name = Path.GetFileName(file_path);
                string path = relativeDirectoryPath; // Use the cleaned relative directory path

                // If the path equals base directory, set it to an empty string
                if (string.IsNullOrEmpty(path) || path == ".")
                {
                    path = string.Empty;
                }

                string sha512 = await Helper.IO.Get_SHA512(file_path);
                string guid = Guid.NewGuid().ToString();
                string password = Guid.NewGuid().ToString();
                string access = "Private";

                Logging.Handler.Debug("Files.Register_File", "name", name);
                Logging.Handler.Debug("Files.Register_File", "path", path);
                Logging.Handler.Debug("Files.Register_File", "sha512", sha512);
                Logging.Handler.Debug("Files.Register_File", "guid", guid);
                Logging.Handler.Debug("Files.Register_File", "access", access);
                Logging.Handler.Debug("Files.Register_File", "date", DateTime.Now.ToString());
                Logging.Handler.Debug("Files.Register_File", "tenant_guid", tenant_guid);
                Logging.Handler.Debug("Files.Register_File", "location_guid", location_guid);
                Logging.Handler.Debug("Files.Register_File", "device_name", device_name);

                // Get the tenant id & location id with tenant_guid & location_guid
                (int tenant_id, int location_id) = await NetLock_RMM_Server.Agent.Windows.Helper.Get_Tenant_Location_Id(tenant_guid, location_guid);

                // Get device ID
                int device_id = await NetLock_RMM_Server.Agent.Windows.Helper.Get_Device_Id(device_name, tenant_id, location_id);

                //  Create the JSON object
                var jsonObject = new
                {
                    guid = guid,
                };

                // Convert the object into a JSON string
                string register_json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Handler.Debug("Files.Register_File", "info_json ", register_json);

                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    // Check if the file already exists
                    string query = "SELECT * FROM files WHERE name = @name AND path = @path;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@path", path);

                    Logging.Handler.Debug("Files.Register_File", "MySQL_Prepared_Query", query);

                    bool fileExists = false;

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        fileExists = reader.HasRows; // Check if file exists
                    }

                    if (fileExists)
                    {
                        // Update file
                        query = "UPDATE files SET device_id = @device_id, name = @name, path = @path, sha512 = @sha512, guid = @guid, password = @password, access = @access, date = @date WHERE name = @name AND path = @path;";
                    }
                    else
                    {
                        // Insert file
                        query = "INSERT INTO files (device_id, name, path, sha512, guid, password, access, date) VALUES (@device_id, @name, @path, @sha512, @guid, @password, @access, @date);";
                    }

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.Parameters.AddWithValue("@sha512", sha512);
                    cmd.Parameters.AddWithValue("@guid", guid);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@access", access);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@device_id", device_id);

                    Logging.Handler.Debug("Files.Register_File", "MySQL_Prepared_Query", query);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Register_File", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }

                return register_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Register_File", "general_error", ex.ToString());
                return string.Empty;
            }
        }

        public static async Task Unregister_File(string guid)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "DELETE FROM files WHERE guid = @guid;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@guid", guid);

                    Logging.Handler.Debug("Files.Unregister_File", "MySQL_Prepared_Query", query);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Unregister_File", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Unregister_File", "general_error", ex.ToString());
            }
        }

        // Command method
        public static async Task Command(string json)
        {
            try
            {
                Logging.Handler.Debug("Files.Command", "json", json);

                Command_Entity command = JsonSerializer.Deserialize<Command_Entity>(json);

                Logging.Handler.Debug("Files.Command", "command", command.command);
                Logging.Handler.Debug("Files.Command", "path", command.path);
                Logging.Handler.Debug("Files.Command", "name", command.name);
                Logging.Handler.Debug("Files.Command", "guid", command.guid);

                // Normalize the path based on the base path
                string normalizedPath = command.path;

                // Replace "base1337" with the actual base path if needed
                if (normalizedPath.Contains("base1337"))
                {
                    normalizedPath = normalizedPath.Replace("base1337", Application_Paths._private_files);
                }

                // Sanitize and get the full path
                string safePath = Path.GetFullPath(Path.Combine(Application_Paths._private_files, normalizedPath))
                    .Replace('\\', '/').TrimEnd('/');

                // Remove the base path for storage or processing
                string relativePath = safePath.Replace(Application_Paths._private_files.Replace('\\', '/'), string.Empty).TrimStart('/');

                if (command.command == "create_directory")
                {
                    if (!Directory.Exists(safePath))
                        Directory.CreateDirectory(safePath);
                }
                else if (command.command == "delete_directory")
                {
                    DirectoryInfo di = new DirectoryInfo(safePath);

                    // Recursively delete files and directories if the directory exists
                    if (di.Exists)
                    {
                        await DeleteDirectoryRecursively(di);
                    }
                }
                else if (command.command == "delete_file")
                {
                    if (File.Exists(Path.Combine(safePath, command.name)))
                    {
                        await Unregister_File(command.guid); // Remove the file from the DB
                        File.Delete(Path.Combine(safePath, command.name));
                    }
                }
                else if (command.command == "rename")
                {
                    string oldPath = Path.GetDirectoryName(safePath);
                    string newPath = Path.Combine(oldPath, command.name);

                    if (File.Exists(safePath))
                    {
                        // Rename file
                        File.Move(safePath, newPath);

                        // Update the path and name in the DB
                        using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                        {
                            try
                            {
                                await conn.OpenAsync();

                                string query = "UPDATE files SET name = @name, path = @path WHERE guid = @guid;";
                                MySqlCommand cmd = new MySqlCommand(query, conn);
                                cmd.Parameters.AddWithValue("@guid", command.guid);
                                cmd.Parameters.AddWithValue("@name", command.name);
                                cmd.Parameters.AddWithValue("@path", relativePath);

                                Logging.Handler.Debug("Files.Command", "MySQL_Prepared_Query", query);

                                await cmd.ExecuteNonQueryAsync();
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("Files.Command", "MySQL_Query", ex.ToString());
                            }
                        }
                    }
                    else if (Directory.Exists(safePath))
                    {
                        // Rename directory
                        Directory.Move(safePath, newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Command", "general_error", ex.ToString());
            }
        }


        // Recursive method for deleting files and directories
        private static async Task DeleteDirectoryRecursively(DirectoryInfo directory)
        {
            try
            {
                // First delete all files in the current directory
                foreach (FileInfo file in directory.GetFiles())
                {
                    await Unregister_File(file.FullName); // Remove the file from the DB
                    file.Delete(); // Delete the file from the file system
                }

                // Recursively run through and delete all subdirectories
                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    await DeleteDirectoryRecursively(subDirectory);
                }

                // If the directory is empty, delete it yourself
                directory.Delete();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.DeleteDirectoryRecursively", "general_error", ex.ToString());
            }
        }

        // Verify device file access
        public static async Task<bool> Verify_Device_File_Access(string tenant_guid, string location_guid, string device_name, string guid)
        {
            try
            {
                // Get the tenant id & location id with tenant_guid & location_guid
                (int tenant_id, int location_id) = await NetLock_RMM_Server.Agent.Windows.Helper.Get_Tenant_Location_Id(tenant_guid, location_guid);

                // Get device ID
                int device_id = await NetLock_RMM_Server.Agent.Windows.Helper.Get_Device_Id(device_name, tenant_id, location_id);

                // Check if the device id matches with the file guid
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM files WHERE device_id = @device_id AND guid = @guid;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@device_id", device_id);
                    cmd.Parameters.AddWithValue("@guid", guid);

                    Logging.Handler.Debug("Files.Verify_Device_File_Access", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Verify_Device_File_Access", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    conn.Close();
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Verify_Device_File_Access", "general_error", ex.ToString());
                return false;
            }
        }

        // Download file
        public static async Task<bool> Verify_File_Access(string guid, string password, string api_key)
        {
            bool access_granted = false;

            try
            {
                Logging.Handler.Debug("Files.Verify_File_Access", "guid", guid);

                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM files WHERE guid = @guid;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@guid", guid);

                    Logging.Handler.Debug("Files.Verify_File_Access", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            Logging.Handler.Debug("Files.Verify_File_Access", "File exists in DB", "true");

                            await reader.ReadAsync();

                            string access = reader.GetString(reader.GetOrdinal("access"));

                            // Check if the file is public or private
                            if (access == "Public")
                                access_granted = true;
                            else if (access == "Private")
                            {
                                // Check if the password is valid
                                if (password == reader.GetString(reader.GetOrdinal("password")))
                                {
                                    access_granted = true;
                                }
                                else
                                    access_granted = false;

                                // Check if the API key is valid, if the password is invalid
                                if (!access_granted)
                                {
                                    // Check if the API key is valid
                                    if (await Verify_Api_Key(api_key))
                                        access_granted = true;
                                    else
                                        access_granted = false;
                                }
                            }
                        }
                        else
                        {
                            Logging.Handler.Debug("Files.Verify_File_Access", "File exists in DB", "false");
                            access_granted = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Verify_File_Access", "MySQL_Query", ex.ToString());
                    access_granted = false;
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Command", "general_error", ex.ToString());
                access_granted = false;
            }

            return access_granted;
        }

        // Get file path by GUID
        public static async Task<string> Get_File_Path_By_GUID(string guid)
        {
            string file_path = String.Empty;

            try
            {
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM files WHERE guid = @guid;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@guid", guid);

                    Logging.Handler.Debug("Files.Get_File_Path_By_GUID", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();

                            string path = reader.GetString(reader.GetOrdinal("path"));
                            string name = reader.GetString(reader.GetOrdinal("name"));

                            file_path = Path.Combine(path, name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Get_File_Path_By_GUID", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Get_File_Path_By_GUID", "general_error", ex.ToString());
            }

            return file_path;
        }

        // Get file name by GUID
        public static async Task<string> Get_File_Name_By_GUID(string json)
        {
            string guid = String.Empty;

            // Deserialize JSON
            try
            {
                Download_JSON download = JsonSerializer.Deserialize<Download_JSON>(json);
                guid = download.guid;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Get_File_Name_By_GUID", "json_deserialize", ex.ToString());
            }

            string file_name = String.Empty;

            try
            {
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM files WHERE guid = @guid;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@guid", guid);

                    Logging.Handler.Debug("Files.Get_File_Name_By_GUID", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();

                            file_name = reader.GetString(reader.GetOrdinal("name"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Get_File_Name_By_GUID", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Get_File_Name_By_GUID", "general_error", ex.ToString());
            }

            return file_name;
        }

        // Get file guid by path
        public static async Task<string> Get_File_GUID_By_Path(string path)
        {
            string guid = String.Empty;

            try
            {
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM files WHERE path = @path;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@path", path);

                    Logging.Handler.Debug("Files.Get_File_GUID_By_Path", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();

                            guid = reader.GetString(reader.GetOrdinal("guid"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Files.Get_File_GUID_By_Path", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Get_File_GUID_By_Path", "general_error", ex.ToString());
            }

            return guid;
        }

        public static async Task<string> Create_Custom_Installer(string name, string json)
        {
            try
            {
                // Remove .zip from the name
                name = name.Replace(".zip", "");
                string architecture = String.Empty;

                string installer_archive = String.Empty;
                string installer_extracted_dir = Path.Combine(Application_Paths.internal_temp_dir, Guid.NewGuid().ToString());
                string installer_packed_dir = Path.Combine(Application_Paths.internal_temp_dir, Guid.NewGuid().ToString());
                string installer_executable = String.Empty;

                byte[] installer_package_bytes = null;

                // Deserialize JSON
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement architecture_element = document.RootElement.GetProperty("architecture");
                    architecture = architecture_element.ToString();
                }

                // Check the architecture
                if (architecture == "win-x64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_win_x64_zip_bytestream;
                else if (architecture == "win-arm64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_win_arm64_zip_bytestream;
                else if (architecture == "linux-x64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_linux_x64_zip_bytestream;
                else if (architecture == "linux-arm64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_linux_arm64_zip_bytestream;
                else if (architecture == "osx-x64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_osx_x64_zip_bytestream;
                else if (architecture == "osx-arm64")
                    installer_package_bytes = Configuration.Members_Portal.installer_package_osx_arm64_zip_bytestream;
                else
                    return string.Empty;

                // Extract installer.package to temp folder 
                if (installer_package_bytes == null)
                    return string.Empty;

                // Entpacken aus MemoryStream
                Directory.CreateDirectory(installer_extracted_dir);
                using (MemoryStream zipStream = new MemoryStream(installer_package_bytes))
                using (ZipArchive archive = new ZipArchive(zipStream))
                {
                    archive.ExtractToDirectory(installer_extracted_dir);
                }
                
                //Thread.Sleep(20000);

                // Check the architecture
                if (architecture == "win-x64" || architecture == "win-arm64")
                    installer_executable = Path.Combine(installer_extracted_dir, "NetLock_RMM_Agent_Installer.exe");
                else if (architecture == "linux-x64" || architecture == "linux-arm64")
                    installer_executable = Path.Combine(installer_extracted_dir, "NetLock_RMM_Agent_Installer");
                else if (architecture == "osx-x64" || architecture == "osx-arm64")
                    installer_executable = Path.Combine(installer_extracted_dir, "NetLock_RMM_Agent_Installer");

                // Write ressource to the installer
                await Assembly_Manipulation.Embedd_Server_Config(installer_executable, json);

                string new_installer_package = Path.Combine(installer_packed_dir, name + ".zip");

                // Check if the directory exists
                if (!Directory.Exists(installer_packed_dir))
                    Directory.CreateDirectory(installer_packed_dir);

                // Package the installer
                ZipFile.CreateFromDirectory(installer_extracted_dir, new_installer_package);

                string installer_download_location = Path.Combine(Application_Paths._private_files_netlock_temp, name + "-" + architecture + ".zip");

                // Copy the packed installer to the private files directory
                File.Copy(new_installer_package, installer_download_location, true);

                // Delete the temp directories
                Directory.Delete(installer_extracted_dir, true);
                Directory.Delete(installer_packed_dir, true);

                string register_result = await Register_File(installer_download_location, String.Empty, String.Empty, String.Empty);

                return register_result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Files.Create_Custom_Installer", "general_error", ex.ToString());
                return string.Empty;
            }
        }
    }
}
