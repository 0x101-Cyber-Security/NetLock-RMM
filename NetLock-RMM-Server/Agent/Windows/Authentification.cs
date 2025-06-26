using MySqlConnector;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Concurrent;
using NetLock_RMM_Server.SignalR;
using Microsoft.Extensions.Primitives;
using Org.BouncyCastle.Asn1.Icao;

namespace NetLock_RMM_Server.Agent.Windows
{
    public class Authentification
    {
        public class Device_Identity
        {
            public string? agent_version { get; set; }
            public string? package_guid { get; set; }
            public string? device_name { get; set; }
            public string? location_guid { get; set; }
            public string? tenant_guid { get; set; }
            public string? access_key { get; set; }
            public string? hwid { get; set; }
            public string? platform { get; set; } = "Windows"; // Default because of version 2.0.0.0 and below. Needs to be removed in version 3.x and above
            public string? ip_address_internal { get; set; }
            public string? operating_system { get; set; }
            public string? domain { get; set; }
            public string? antivirus_solution { get; set; }
            public string? firewall_status { get; set; }
            public string? architecture { get; set; }
            public string? last_boot { get; set; }
            public string? timezone { get; set; }
            public string? cpu { get; set; }
            public string? cpu_usage { get; set; }
            public string? mainboard { get; set; }
            public string? gpu { get; set; }
            public string? ram { get; set; }
            public string? ram_usage { get; set; }
            public string? tpm { get; set; }
            public string? environment_variables { get; set; }
            public string? last_active_user { get; set; }
        }

        public class Admin_Identity
        {
            public string? token { get; set; }
        }

        public class Root_Entity
        {
            public Device_Identity? device_identity { get; set; }
            public Admin_Identity? admin_identity { get; set; }
            public Admin_Identity? remote_control { get; set; }
        }

        public static async Task<string> Verify_Device(string json, string ip_address_external, bool update)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                Logging.Handler.Debug("Agent.Windows.Authentification.Verify_Device", "json", json);

                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);

                Device_Identity device_identity = rootData.device_identity;

                // Get the tenant id & location id with tenant_guid & location_guid
                (int tenant_id, int location_id) = await Helper.Get_Tenant_Location_Id(device_identity.tenant_guid, device_identity.location_guid);

                await conn.OpenAsync();

                string reader_query = "SELECT * FROM `devices` WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id;";
                Logging.Handler.Debug("Modules.Authentification.Verify_Device", "MySQL_Query", reader_query);

                MySqlCommand command = new MySqlCommand(reader_query, conn);
                command.Parameters.AddWithValue("@tenant_id", tenant_id);
                command.Parameters.AddWithValue("@location_id", location_id);
                command.Parameters.AddWithValue("@device_name", device_identity.device_name);

                DbDataReader reader = await command.ExecuteReaderAsync();

                string authentification_result = String.Empty;
                string authorized = "0";
                bool deauthorize = false;
                bool device_exists = true;

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        if (device_identity.access_key == reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString() && reader["authorized"].ToString() == "1") //access key & hwid correct
                        {
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device authorized", "Device is authorized.");

                            if (reader["synced"].ToString() == "1")
                            {
                                authentification_result = "synced";
                            }
                            else if (reader["synced"].ToString() == "0")
                            {
                                authentification_result = "not_synced";
                            }

                            authorized = "1";
                        }
                        else if (device_identity.access_key == reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString() && reader["authorized"].ToString() == "0") //access key & hwid correct, but not authorized
                        {
                            // log state with additional details
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device not authorized", "Device is not authorized. //access key & hwid correct, but not authorized");
                            authentification_result = "unauthorized";
                        }
                        else if (device_identity.access_key != reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString()) //access key is not correct, but hwid is. Deauthorize the device, set new access key & set not synced
                        {
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device unauthorized", "Device is unauthorized. //access key is not correct, but hwid is. Deauthorize the device, set new access key & set not synced. Access key: " + device_identity.access_key + " DB access_key: " + reader["access_key"].ToString() + " Device hwid: " + device_identity.hwid + " DB hwid: " + reader["hwid"].ToString());
                            authentification_result = "unauthorized";
                            deauthorize = true;
                        }
                        else if (device_identity.access_key == reader["access_key"].ToString() && device_identity.hwid != reader["hwid"].ToString()) //access key is correct, but hwid is not. Deauthorize the device, set new hwid & set not synced
                        {
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device unauthorized", "Device is unauthorized. //access key is correct, but hwid is not. Deauthorize the device, set new access key & set not synced. Access key: " + device_identity.access_key + " DB access_key: " + reader["access_key"].ToString() + " Device hwid: " + device_identity.hwid + " DB hwid: " + reader["hwid"].ToString());
                            authentification_result = "unauthorized";
                            deauthorize = true;
                        }
                        else if (device_identity.access_key != reader["access_key"].ToString() && device_identity.hwid != reader["hwid"].ToString()) //access key is not correct & hwid is not. Deauthorize the device, set new access key & set not synced
                        {
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device unauthorized", "Device is unauthorized. //access key is not correct, but hwid is not. Deauthorize the device, set new access key & set not synced. Access key: " + device_identity.access_key + " DB access_key: " + reader["access_key"].ToString() + " Device hwid: " + device_identity.hwid + " DB hwid: " + reader["hwid"].ToString());
                            authentification_result = "unauthorized";
                            deauthorize = true;
                        }
                        else // data not correct. Refuse device
                        {
                            Logging.Handler.Debug("Modules.Authentification.Verify_Device", "Device invalid", "Device is invalid.");
                            authentification_result = "invalid";
                        }
                    }

                    await reader.CloseAsync();

                    // Deauthorize the device if access key is not correct, but hwid is
                    if (deauthorize)
                    {
                        string execute_query = "UPDATE `devices` SET access_key = @access_key, hwid = @hwid, authorized = 0, synced = 0 WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id";

                        MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                        cmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                        cmd.Parameters.AddWithValue("@location_id", location_id);
                        cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                        cmd.Parameters.AddWithValue("@access_key", device_identity.access_key);
                        cmd.Parameters.AddWithValue("@hwid", device_identity.hwid);
                        cmd.ExecuteNonQuery();
                    }
                }
                else //device not existing, create
                {
                    await reader.CloseAsync();

                    (string tenant_name, string location_name) = await Helper.Get_Tenant_Location_Name(tenant_id, location_id);

                    device_exists = false;
                    string execute_query = "INSERT INTO `devices` " +
                        "(`agent_version`, " +
                        "`tenant_id`, " +
                        "`tenant_name`, " +
                        "`location_id`, " +
                        "`location_name`, " +
                        "`device_name`, " +
                        "`access_key`, " +
                        "`hwid`, " +
                        "`platform`, " +
                        "`last_access`, " +
                        "`ip_address_internal`, " +
                        "`ip_address_external`, " +
                        "`operating_system`, " +
                        "`domain`, " +
                        "`antivirus_solution`, " +
                        "`firewall_status`, " +
                        "`architecture`, " +
                        "`last_boot`, " +
                        "`timezone`, " +
                        "`cpu`, " +
                        "`cpu_usage`, " +
                        "`mainboard`, " +
                        "`gpu`, " +
                        "`ram`, " +
                        "`ram_usage`, " +
                        "`tpm`, " +
                        "`last_active_user`, " +
                        "`environment_variables`) " +
                        "VALUES " +
                        "(@agent_version, " +
                        "@tenant_id, " +
                        "@tenant_name, " +
                        "@location_id, " +
                        "@location_name, " +
                        "@device_name, " +
                        "@access_key, " +
                        "@hwid, " +
                        "@platform, " +
                        "@last_access, " +
                        "@ip_address_internal, " +
                        "@ip_address_external, " +
                        "@operating_system, " +
                        "@domain, " +
                        "@antivirus_solution, " +
                        "@firewall_status, " +
                        "@architecture, " +
                        "@last_boot, " +
                        "@timezone, " +
                        "@cpu, " +
                        "@cpu_usage, " +
                        "@mainboard, " +
                        "@gpu, " +
                        "@ram, " +
                        "@ram_usage, " +
                        "@tpm, " +
                        "@last_active_user, " +
                        "@environment_variables);";

                    MySqlCommand cmd = new MySqlCommand(execute_query, conn);

                    cmd.Parameters.AddWithValue("@agent_version", device_identity.agent_version);
                    cmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                    cmd.Parameters.AddWithValue("@tenant_name", tenant_name);
                    cmd.Parameters.AddWithValue("@location_id", location_id);
                    cmd.Parameters.AddWithValue("@location_name", location_name);
                    cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                    cmd.Parameters.AddWithValue("@access_key", device_identity.access_key);
                    cmd.Parameters.AddWithValue("@hwid", device_identity.hwid);
                    cmd.Parameters.AddWithValue("@platform", device_identity.platform);
                    cmd.Parameters.AddWithValue("@last_access", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@ip_address_internal", device_identity.ip_address_internal);
                    cmd.Parameters.AddWithValue("@ip_address_external", ip_address_external);
                    cmd.Parameters.AddWithValue("@operating_system", device_identity.operating_system);
                    cmd.Parameters.AddWithValue("@domain", device_identity.domain);
                    cmd.Parameters.AddWithValue("@antivirus_solution", device_identity.antivirus_solution);
                    cmd.Parameters.AddWithValue("@firewall_status", device_identity.firewall_status);
                    cmd.Parameters.AddWithValue("@architecture", device_identity.architecture);
                    cmd.Parameters.AddWithValue("@last_boot", Helper.Truncate(device_identity.last_boot));
                    cmd.Parameters.AddWithValue("@timezone", device_identity.timezone);
                    cmd.Parameters.AddWithValue("@cpu", Helper.Truncate(device_identity.cpu));
                    cmd.Parameters.AddWithValue("@cpu_usage", device_identity.cpu_usage);
                    cmd.Parameters.AddWithValue("@mainboard", Helper.Truncate(device_identity.mainboard));
                    cmd.Parameters.AddWithValue("@gpu", Helper.Truncate(device_identity.gpu));
                    cmd.Parameters.AddWithValue("@ram", Helper.Truncate(device_identity.ram));
                    cmd.Parameters.AddWithValue("@ram_usage", device_identity.ram_usage);
                    cmd.Parameters.AddWithValue("@tpm", Helper.Truncate(device_identity.tpm));
                    cmd.Parameters.AddWithValue("@environment_variables", device_identity.environment_variables);
                    cmd.Parameters.AddWithValue("@last_active_user", Helper.Truncate(device_identity.last_active_user));

                    cmd.ExecuteNonQuery();

                    authentification_result = "unauthorized";
                    device_exists = false;
                }

                //Update device data if authorized, not synced or synced, and device exists, and update is true
                if ((authentification_result == "authorized" || authentification_result == "synced" || authentification_result == "not_synced") && device_exists && update)
                {
                    string synced = "0";

                    if (authentification_result == "authorized" && authentification_result == "not_synced")
                    {
                        synced = "0";
                    }
                    else if (authentification_result == "synced")
                    {
                        synced = "1";
                    }

                    string execute_query = "UPDATE `devices` SET " +
                        "`agent_version` = @agent_version, " +
                        "`tenant_id` = @tenant_id, " +
                        "`location_id` = @location_id, " +
                        "`device_name` = @device_name, " +
                        "`access_key` = @access_key, " +
                        "`platform` = @platform, " +
                        "`authorized` = @authorized, " +
                        "`last_access` = @last_access, " +
                        "`ip_address_internal` = @ip_address_internal, " +
                        "`ip_address_external` = @ip_address_external, " +
                        "`operating_system` = @operating_system, " +
                        "`domain` = @domain, " +
                        "`antivirus_solution` = @antivirus_solution, " +
                        "`firewall_status` = @firewall_status, " +
                        "`architecture` = @architecture, " +
                        "`last_boot` = @last_boot, " +
                        "`timezone` = @timezone, " +
                        "`cpu` = @cpu, " +
                        "`cpu_usage` = @cpu_usage, " +
                        "`mainboard` = @mainboard, " +
                        "`gpu` = @gpu, " +
                        "`ram` = @ram, " +
                        "`ram_usage` = @ram_usage, " +
                        "`tpm` = @tpm, " +
                        "`environment_variables` = @environment_variables, " +
                        "`synced` = @synced, " +
                        "`last_active_user` = @last_active_user " +
                        "WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id";

                    MySqlCommand cmd = new MySqlCommand(execute_query, conn);

                    cmd.Parameters.AddWithValue("@agent_version", device_identity.agent_version);
                    cmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                    cmd.Parameters.AddWithValue("@location_id", location_id);
                    cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                    cmd.Parameters.AddWithValue("@access_key", device_identity.access_key);
                    cmd.Parameters.AddWithValue("@platform", device_identity.platform);
                    cmd.Parameters.AddWithValue("@authorized", authorized);
                    cmd.Parameters.AddWithValue("@last_access", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@ip_address_internal", device_identity.ip_address_internal);
                    cmd.Parameters.AddWithValue("@ip_address_external", ip_address_external);
                    cmd.Parameters.AddWithValue("@operating_system", device_identity.operating_system);
                    cmd.Parameters.AddWithValue("@domain", device_identity.domain);
                    cmd.Parameters.AddWithValue("@antivirus_solution", device_identity.antivirus_solution);
                    cmd.Parameters.AddWithValue("@firewall_status", device_identity.firewall_status);
                    cmd.Parameters.AddWithValue("@architecture", device_identity.architecture);
                    cmd.Parameters.AddWithValue("@last_boot", Helper.Truncate(device_identity.last_boot));
                    cmd.Parameters.AddWithValue("@timezone", device_identity.timezone);
                    cmd.Parameters.AddWithValue("@cpu", Helper.Truncate(device_identity.cpu));
                    cmd.Parameters.AddWithValue("@cpu_usage", device_identity.cpu_usage);
                    cmd.Parameters.AddWithValue("@mainboard", Helper.Truncate(device_identity.mainboard));
                    cmd.Parameters.AddWithValue("@gpu", Helper.Truncate(device_identity.gpu));
                    cmd.Parameters.AddWithValue("@ram", Helper.Truncate(device_identity.ram));
                    cmd.Parameters.AddWithValue("@ram_usage", device_identity.ram_usage);
                    cmd.Parameters.AddWithValue("@tpm", Helper.Truncate(device_identity.tpm));
                    cmd.Parameters.AddWithValue("@environment_variables", "");
                    cmd.Parameters.AddWithValue("@synced", synced);
                    cmd.Parameters.AddWithValue("@last_active_user", Helper.Truncate(device_identity.last_active_user));

                    cmd.ExecuteNonQuery();
                }
                else if (authentification_result == "unauthorized") // if unauthorized, update last access
                {
                    string execute_query = "UPDATE `devices` SET last_access = @last_access WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id";

                    MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                    cmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                    cmd.Parameters.AddWithValue("@location_id", location_id);
                    cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                    cmd.Parameters.AddWithValue("@last_access", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }

                return authentification_result; //returns final result
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Authentification.Verify_Device", "General error", ex.ToString());
                return "invalid";
            }
            finally
            {
                conn.Close();
            }
        }

        public static async Task<bool> Verify_Admin(string username, string password)
        {
            bool isPasswordCorrect = false;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand("SELECT * FROM accounts WHERE username = @username;", conn);
                cmd.Parameters.AddWithValue("@username", username);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                        isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, reader["password"].ToString());
                }
                await reader.CloseAsync();

                Logging.Handler.Debug("NetLock_RMM_Server.Modules.Authentification.Verify_Admin", "isPasswordCorrect", isPasswordCorrect.ToString());

                return isPasswordCorrect;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        public static async Task<bool> Verify_NetLock_Package_Configurations_Guid(string guid)
        {
            bool isCorrect = false;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM agent_package_configurations WHERE guid = @guid;", conn);
                cmd.Parameters.AddWithValue("@guid", guid);

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                Logging.Handler.Debug("NetLock_RMM_Server.Modules.Authentification.Verify_NetLock_Package_Configurations_Guid", "count", count.ToString());

                if (count > 0)
                    isCorrect = true;
                
                Logging.Handler.Debug("NetLock_RMM_Server.Modules.Authentification.Verify_NetLock_Package_Configurations_Guid", "isCorrect", isCorrect.ToString());

                return isCorrect;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Authentification.Verify_NetLock_Package_Configurations_Guid", "General error", ex.ToString());
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        public class JsonAuthMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly ConcurrentDictionary<string, string> _clientConnections;


            public JsonAuthMiddleware(RequestDelegate next)
            {
                _next = next;
                _clientConnections = ConnectionManager.Instance.ClientConnections;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                if (!await Helper.Get_Role_Status("Remote"))
                {
                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "Trust role", "Trust role is not enabled.");
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }

                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    // Attempt to retrieve the header values
                    bool hasDeviceIdentity = context.Request.Headers.TryGetValue("Device-Identity", out StringValues deviceIdentityEncoded);
                    bool hasAdminIdentity = context.Request.Headers.TryGetValue("Admin-Identity", out StringValues adminIdentityEncoded);

                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "hasDeviceIdentity", hasDeviceIdentity.ToString());
                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "hasAdminIdentity", hasAdminIdentity.ToString());

                    if (!hasDeviceIdentity && !hasAdminIdentity)
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "identityJson", "Device (" + hasDeviceIdentity + ") or admin (" + hasAdminIdentity + ") identity was not provided.");
                        await context.Response.WriteAsync("Device or admin identity was not provided.");
                        return;
                    }

                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "deviceIdentityEncoded", deviceIdentityEncoded.ToString());
                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "adminIdentityEncoded", adminIdentityEncoded.ToString());

                    // Decode the received JSON
                    string deviceIdentityJson = String.Empty;
                    string adminIdentityJson = String.Empty;

                    if (hasDeviceIdentity)
                        deviceIdentityJson = Uri.UnescapeDataString(deviceIdentityEncoded);
                    else if (hasAdminIdentity)
                        adminIdentityJson = Uri.UnescapeDataString(adminIdentityEncoded);

                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "deviceIdentityJson", deviceIdentityJson);
                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "adminIdentityJson", adminIdentityJson);

                    // Deserialize the JSON
                    Root_Entity rootData = new Root_Entity();

                    if (hasDeviceIdentity)
                        rootData = JsonSerializer.Deserialize<Root_Entity>(deviceIdentityJson);
                    else if (hasAdminIdentity)
                        rootData = JsonSerializer.Deserialize<Root_Entity>(adminIdentityJson);

                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "rootData", "extracted");

                    Device_Identity device_identity = new Device_Identity();
                    Admin_Identity admin_identity = new Admin_Identity();
    
                    string authentification_result = String.Empty;

                    if (hasDeviceIdentity)
                    {
                        device_identity = rootData.device_identity;

                        // Verify package guid
                        bool package_guid_status = await Verify_NetLock_Package_Configurations_Guid(device_identity.package_guid);

                        if (package_guid_status == false)
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Unauthorized.");
                            return;
                        }

                        // Verarbeiten Sie die Device-Identity
                        Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "device_identity", $"Device name: {device_identity.device_name}");

                        // Get the tenant id & location id with tenant_guid & location_guid
                        (int tenant_id, int location_id) = await Helper.Get_Tenant_Location_Id(device_identity.tenant_guid, device_identity.location_guid);

                        await conn.OpenAsync();

                        string reader_query = "SELECT * FROM `devices` WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id;";
                        Logging.Handler.Debug("Modules.Authentification.InvokeAsync", "MySQL_Query", reader_query);

                        MySqlCommand command = new MySqlCommand(reader_query, conn);
                        command.Parameters.AddWithValue("@device_name", device_identity.device_name);
                        command.Parameters.AddWithValue("@location_id", location_id);
                        command.Parameters.AddWithValue("@tenant_id", tenant_id);

                        DbDataReader reader = await command.ExecuteReaderAsync();

                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                if (device_identity.access_key == reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString() && reader["authorized"].ToString() == "1") //access key & hwid correct
                                {
                                    if (reader["synced"].ToString() == "1")
                                    {
                                        authentification_result = "synced";
                                    }
                                    else if (reader["synced"].ToString() == "0")
                                    {
                                        authentification_result = "not_synced";
                                    }
                                }
                                else if (device_identity.access_key == reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString() && reader["authorized"].ToString() == "0") //access key & hwid correct, but not authorized
                                {
                                    authentification_result = "unauthorized";
                                }
                                else if (device_identity.access_key != reader["access_key"].ToString() && device_identity.hwid == reader["hwid"].ToString()) //access key is not correct, but hwid is. Deauthorize the device, set new access key & set not synced
                                {
                                    authentification_result = "authorized";
                                }
                                else // data not correct. Refuse device
                                {
                                    authentification_result = "invalid";
                                }
                            }

                            await reader.CloseAsync();
                        }
                        else //device not existing, create
                            authentification_result = "unauthorized";
                    }
                    else if (hasAdminIdentity)
                    {
                        admin_identity = rootData.admin_identity;

                        // Verarbeiten Sie die Admin-Identity
                        Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "admin_identity", $"Admin token: {admin_identity.token}");
                        
                        bool isTokenCorrect = await SignalR.Webconsole.Handler.Verify_Remote_Session_Token(admin_identity.token);

                        // Check if the password is correct
                        if (isTokenCorrect)
                        {
                            Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "isTokenCorrect", "Token is correct.");
                            authentification_result = "authorized";
                        }
                        else
                        {
                            Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "isPasswordCorrect", "Token is incorrect.");
                            authentification_result = "unauthorized";
                        }
                    }
                    else
                    {   
                        context.Response.StatusCode = 400; // Bad Request
                        Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "Invalid JSON", "Neither device identity nor admin identity provided.");
                        await context.Response.WriteAsync("Invalid JSON. Neither device identity nor admin identity provided.");
                        return;
                    }

                    Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "authentification_result", authentification_result);

                    // Device is not authorized or invalid, remove from client connections
                    if (authentification_result == "unauthorized" || authentification_result == "invalid")
                    {
                        Logging.Handler.Debug("Agent.Windows.Authentification.InvokeAsync", "authentification_result", "Unauthorized device or admin.");

                        var clientId = context.Connection.Id;

                        if (_clientConnections.ContainsKey(clientId))
                            _clientConnections.TryRemove(clientId, out _);

                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsync("Unauthorized device.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("NetLock_RMM_Server.Modules.Authentification.InvokeAsync", "General error", ex.ToString());

                    var clientId = context.Connection.Id;

                    if (_clientConnections.ContainsKey(clientId))
                        _clientConnections.TryRemove(clientId, out _);

                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Unauthorized device.");
                    return;
                }
                finally
                {
                    conn.Close();
                }

                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
        }
    }
}
