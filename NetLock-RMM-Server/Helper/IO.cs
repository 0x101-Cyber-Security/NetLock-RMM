using MySqlConnector;
using NetLock_RMM_Server;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Helper
{
    public class IO
    {
        public class File_Or_Directory_Info
        {
            public string name { get; set; }
            public string path { get; set; }
            public string type { get; set; }
            public string size { get; set; }
            public DateTime last_modified { get; set; }
            public string sha512 { get; set; }
            public string guid { get; set; }
            public string password { get; set; }
            public string access { get; set; }
        }

        // Get directories from path
        public static async Task<List<File_Or_Directory_Info>> Get_Directory_Index(string path)
        {
            MySqlConnection conn = new MySqlConnection(NetLock_RMM_Server.Configuration.MySQL.Connection_String);
            var directoryDetails = new List<File_Or_Directory_Info>();

            try
            {
                await conn.OpenAsync();

                // Define the base directory (_private_files_admin)
                string baseDirectory = Application_Paths._private_files.Replace('\\', '/').TrimEnd('/');

                DirectoryInfo rootDirInfo = new DirectoryInfo(path);

                // Directories
                foreach (var directory in rootDirInfo.GetDirectories())
                {
                    // Remove the base path to get the relative path
                    string relativeDirectoryPath = directory.FullName.Replace('\\', '/').Replace(baseDirectory, "").TrimStart('/');

                    var dirDetail = new File_Or_Directory_Info
                    {
                        name = directory.Name,
                        path = relativeDirectoryPath,
                        last_modified = directory.LastWriteTime,
                        size = await Get_Directory_Size(directory),
                        type = "0", // 0 = Directory
                    };

                    directoryDetails.Add(dirDetail);
                }

                // Files
                foreach (var file in rootDirInfo.GetFiles())
                {
                    // Remove the base path to get the relative path
                    string relativeFilePath = file.FullName.Replace('\\', '/').Replace(baseDirectory, "").TrimStart('/');

                    // Extract just the directory part of the path
                    string relativeDirectoryPath = Path.GetDirectoryName(relativeFilePath);

                    // Prepare the query
                    string query = "SELECT * FROM files WHERE name = @name AND path = @path;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", file.Name);
                    cmd.Parameters.AddWithValue("@path", relativeDirectoryPath);

                    string sha512 = String.Empty;
                    string guid = String.Empty;
                    string password = String.Empty;
                    string access = String.Empty;

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                sha512 = reader["sha512"].ToString();
                                guid = reader["guid"].ToString();
                                password = reader["password"].ToString();
                                access = reader["access"].ToString();
                            }
                        }
                    }

                    var fileDetail = new File_Or_Directory_Info
                    {
                        name = file.Name,
                        path = relativeDirectoryPath,
                        last_modified = file.LastWriteTime,
                        size = await Get_File_Size(file.FullName),
                        type = file.Extension,
                        sha512 = sha512,
                        guid = guid,
                        password = password,
                        access = access
                    };

                    directoryDetails.Add(fileDetail);
                }

                return directoryDetails;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("IO.GetDirectoryDetails", "General error", ex.ToString());
                return directoryDetails;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task<string> GetSizeFormatted(long sizeInBytes)
        {
            return await Task.Run(() =>
            {
                if (sizeInBytes >= 1024 * 1024 * 1024) // Check for GB
                {
                    double sizeInGB = sizeInBytes / (1024.0 * 1024.0 * 1024.0);
                    return sizeInGB.ToString("F2") + " GB";
                }
                else if (sizeInBytes >= 1024 * 1024) // Check for MB
                {
                    double sizeInMB = sizeInBytes / (1024.0 * 1024.0);
                    return sizeInMB.ToString("F2") + " MB";
                }
                else if (sizeInBytes >= 1024) // Check for KB
                {
                    double sizeInKB = sizeInBytes / 1024.0;
                    return sizeInKB.ToString("F2") + " KB";
                }
                else // Bytes
                {
                    return sizeInBytes.ToString() + " Bytes";
                }
            });
        }

        public static async Task<string> Get_Directory_Size(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                // Add file sizes.
                FileInfo[] fis = directory.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }

                // Convert and format size.
                return await GetSizeFormatted(size);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("IO.Get_Directory_Size", "General error", ex.ToString());
                return "0.00 Bytes";
            }
        }

        public static async Task<string> Get_File_Size(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                long size = fileInfo.Length;

                // Convert and format size.
                return await GetSizeFormatted(size);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("IO.Get_File_Size", "General error", ex.ToString());
                return "0.00 Bytes";
            }
        }

        public static async Task <string> Get_SHA512(string FilePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(FilePath))
                {
                    SHA512Managed sha512 = new SHA512Managed();
                    byte[] checksum_sha512 = sha512.ComputeHash(stream);
                    string hash = BitConverter.ToString(checksum_sha512).Replace("-", String.Empty);

                    Logging.Handler.Debug("Helper.IO.Get_SHA512", "hash", hash);

                    return hash;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.IO.Get_SHA512", "General error", ex.ToString());
                return ex.ToString();
            }
        }
    }
}