using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Global.Helper
{
    internal class IO
    {
        public class File_Or_Directory_Info
        {
            public string name { get; set; }
            public string path { get; set; }
            public string type { get; set; }
            public string size { get; set; }
            public DateTime last_modified { get; set; }
        }


        // Get drives
        public static string Get_Drives()
        {
            var driveLetters = new List<string>();

            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady)
                    {
                        driveLetters.Add(d.Name);
                    }
                }

                return string.Join(",", driveLetters);
            }
            catch (Exception ex)
            {
                Logging.Error("IO.GetDriveLetters", "General error", ex.ToString());
                return string.Empty;
            }
        }

        // Get directories from path
        public static async Task<List<File_Or_Directory_Info>> Get_Directory_Index(string path)
        {
            var directoryDetails = new List<File_Or_Directory_Info>();

            try
            {
                DirectoryInfo rootDirInfo = new DirectoryInfo(path);

                // Directories
                foreach (var directory in rootDirInfo.GetDirectories())
                {
                    var dirDetail = new File_Or_Directory_Info
                    {
                        name = directory.Name,
                        path = directory.FullName,
                        last_modified = directory.LastWriteTime,
                        size = await Get_Directory_Size(directory),
                        type = "0", // 0 = Directory
                    };

                    directoryDetails.Add(dirDetail);
                }

                // Files
                foreach (var file in rootDirInfo.GetFiles())
                {
                    var fileDetail = new File_Or_Directory_Info
                    {
                        name = file.Name,
                        path = file.FullName,
                        last_modified = file.LastWriteTime,
                        size = await Get_File_Size(file.FullName),
                        type = file.Extension,
                    };

                    directoryDetails.Add(fileDetail);
                }

                return directoryDetails;
            }
            catch (Exception ex)
            {
                Logging.Error("IO.GetDirectoryDetails", "General error", ex.ToString());
                return directoryDetails;
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
                Logging.Error("IO.Get_Directory_Size", "General error", ex.ToString());
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
                Logging.Error("IO.Get_File_Size", "General error", ex.ToString());
                return "0.00 Bytes";
            }
        }

        // Create directory and return true if successful
        public static string Create_Directory(string path)
        {
            try
            {
                // Check if the directory exists
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Create_Directory", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Delete directory and files and return true if successful
        public static string Delete_Directory(string path)
        {
            StringBuilder deletedItems = new StringBuilder();

            try
            {
                // Check if the directory exists
                if (Directory.Exists(path))
                {
                    // Recursively delete all files and subdirectories
                    Delete_Directory_Recursive(new DirectoryInfo(path), deletedItems);

                    // Delete the root directory itself
                    Directory.Delete(path, true);

                    // Append the root directory to the list of deleted items
                    deletedItems.AppendLine(path);
                }

                return deletedItems.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("IO.DeleteDirectoryAndListContents", "General error", ex.ToString());
                return ex.Message + Environment.NewLine + Environment.NewLine + deletedItems.ToString();
            }
        }

        private static void Delete_Directory_Recursive(DirectoryInfo directoryInfo, StringBuilder deletedItems)
        {
            try
            {
                // Delete all files in the directory
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    try
                    {
                        file.Delete();
                        deletedItems.AppendLine(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("IO.DeleteDirectoryRecursive", "Error deleting file", ex.ToString());
                    }
                }

                // Recursively delete all subdirectories
                foreach (DirectoryInfo subdirectory in directoryInfo.GetDirectories())
                {
                    try
                    {
                        Delete_Directory_Recursive(subdirectory, deletedItems);
                        subdirectory.Delete();
                        deletedItems.AppendLine(subdirectory.FullName);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("IO.DeleteDirectoryRecursive", "Error deleting directory", ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Delete_Directory_Recursive", "General error", ex.ToString());
            }
        }

        // Move directory and return true if successful
        public static string Move_Directory(string source_path, string destination_path)
        {
            Logging.Debug("IO.Move_Directory", "", $"Source: {source_path}, Destination: {destination_path}");

            var movedItems = new List<string>();

            try
            {
                // Check whether the source directory exists
                if (Directory.Exists(source_path))
                {
                    // Check whether the target directory exists and create if necessary
                    if (!Directory.Exists(destination_path))
                        Directory.CreateDirectory(destination_path);

                    // Move all files from the source directory to the target directory
                    var files = Directory.GetFiles(source_path);
                    foreach (var file in files)
                    {
                        var destFile = Path.Combine(destination_path, Path.GetFileName(file));
                        if (File.Exists(destFile))
                        {
                            // Dateinamenskonflikt behandeln
                            var newFileName = Path.GetFileNameWithoutExtension(destFile) + "_copy" + Path.GetExtension(destFile);
                            destFile = Path.Combine(destination_path, newFileName);
                        }
                        File.Move(file, destFile);
                        movedItems.Add(destFile);
                    }

                    // Move all subdirectories from the source directory to the target directory
                    var directories = Directory.GetDirectories(source_path);
                    foreach (var directory in directories)
                    {
                        var destDir = Path.Combine(destination_path, Path.GetFileName(directory));
                        if (Directory.Exists(destDir))
                        {
                            // Verzeichnisnamenskonflikt behandeln
                            var newDirName = Path.GetFileName(directory) + "_copy";
                            destDir = Path.Combine(destination_path, newDirName);
                        }
                        Directory.Move(directory, destDir);
                        movedItems.Add(destDir);
                    }

                    // At the end, do not move the source directory itself, as all contents have already been moved
                }
                else
                {
                    return $"Source path does not exist: {source_path}";
                }

                return string.Join(Environment.NewLine, movedItems);
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Move_Directory", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Rename directory or file and return true if successful
        public static string Rename_Directory(string sourceDirectoryPath, string newDirectoryName)
        {
            try
            {
                if (Directory.Exists(sourceDirectoryPath))
                {
                    string parentDirectory = Path.GetDirectoryName(sourceDirectoryPath);
                    string destDirectoryPath = Path.Combine(parentDirectory, newDirectoryName);

                    // Überprüfen, ob der neue Ordnername bereits existiert
                    if (Directory.Exists(destDirectoryPath))
                    {
                        throw new IOException("A directory with the same name already exists.");
                    }

                    Directory.Move(sourceDirectoryPath, destDirectoryPath);

                    return destDirectoryPath;
                }
                else
                {
                    return "The source directory does not exist.";
                }
            }
            catch (Exception ex)
            {
                Logging.Error("IO.RenameDirectory", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Create file and return true if successful
        public static async Task<string> Create_File(string path, string content)
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(path))
                    File.WriteAllText(path, await Base64.Decode(content));

                return path;
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Create_File", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Delete file and return true if successful
        public static string Delete_File(string path)
        {
            try
            {
                // Check if the file exists
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return path;
                }

                return true.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Delete_File", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Move file and return true if successful
        public static string Move_File(string source_path, string destination_path)
        {
            try
            {
                // Check if the source file exists
                if (File.Exists(source_path))
                {
                    // Check if the destination directory exists
                    if (!Directory.Exists(Path.GetDirectoryName(destination_path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(destination_path));

                    // Move the file
                    File.Move(source_path, destination_path);

                    return destination_path;
                }

                return "The source file does not exist.";
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Move_File", "General error", ex.ToString());
                return ex.Message;
            }
        }

        // Rename file and return true if successful
        public static string Rename_File(string sourceFilePath, string newFileName)
        {
            try
            {
                if (File.Exists(sourceFilePath))
                {
                    string parentDirectory = Path.GetDirectoryName(sourceFilePath);
                    string destFilePath = Path.Combine(parentDirectory, newFileName);

                    // Überprüfen, ob der neue Dateiname bereits existiert
                    if (File.Exists(destFilePath))
                    {
                        return "A file with the same name already exists.";
                    }

                    File.Move(sourceFilePath, destFilePath);

                    return destFilePath;
                }
                else
                {
                    return "The source file does not exist.";
                }
            }
            catch (Exception ex)
            {
                Logging.Error("IO.Rename_File", "General error", ex.ToString());
                return ex.Message;
            }
        }

    }
}
