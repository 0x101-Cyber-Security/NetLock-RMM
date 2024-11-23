using System.Text.Json;

namespace NetLock_RMM_Server.MySQL
{
    public class Config
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string SslMode { get; set; }
        public string AdditionalConnectionParameters { get; set; }

        public class MySQL
        {
            public string Server { get; set; }
            public int Port { get; set; }
            public string Database { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string SslMode { get; set; }
            public string AdditionalConnectionParameters { get; set; }

        }

        public class RootData
        {
            public MySQL MySQL { get; set; }
        }

        // Read appsettings.json file and get mysql database name
        public static async Task<string> Get_Database_Name()
        {
            try
            {
                // Read appssettings.json file
                string json = await File.ReadAllTextAsync("appsettings.json");

                //Logging.Handler.Debug("Classes.MySQL.MySQLConfig.Get_Database_Name", "json", json);

                // Deserialize JSON string
                RootData rootData = JsonSerializer.Deserialize<RootData>(json);

                // Get MySQL configuration
                string database = rootData.MySQL.Database;

                // Return MySQL configuration
                //Logging.Handler.Debug("Classes.MySQL.MySQLConfig.Get_Database_Name", "database", database);
                return database;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.MySQLConfig.Get_Database_Name", "General error", ex.ToString());
                return String.Empty;
            }
        }
    }
}
