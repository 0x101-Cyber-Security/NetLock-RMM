using System.Data.Common;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using crypto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using MySqlConnector;
using System.Security.Claims;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace NetLock_RMM_Web_Console.Components.Pages.Devices
{
    public partial class Devices
    {

        #region Permissions System

        private string netlock_username = String.Empty;
        private string token = String.Empty;
        private string permissions_json = String.Empty;

        private bool permissions_devices_authorized_enabled = false;
        private bool permissions_devices_general = false;
        private bool permissions_devices_software = false;
        private bool permissions_devices_task_manager = false;
        private bool permissions_devices_antivirus = false;
        private bool permissions_devices_events = false;
        private bool permissions_devices_remote_shell = false;
        private bool permissions_devices_remote_file_browser = false;
        private bool permissions_devices_remote_control = false;
        private bool permissions_devices_deauthorize = false;
        private bool permissions_devices_move = false;
        public static List<string> permissions_tenants_list = new List<string> { };

        private async Task<bool> Permissions()
        {
            try
            {
                bool logout = false;
                bool has_all_tenants_permission = false;

                // Get the current user from the authentication state
                var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;

                // Check if user is authenticated
                if (user?.Identity is not { IsAuthenticated: true })
                    logout = true;

                netlock_username = user.FindFirst(ClaimTypes.Email)?.Value;

                token = await Classes.Authentication.User.Get_Remote_Session_Token(netlock_username);

                permissions_devices_authorized_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_authorized_enabled");
                permissions_devices_general = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_general");
                permissions_devices_software = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_software");
                permissions_devices_task_manager = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_task_manager");
                permissions_devices_antivirus = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_antivirus");
                permissions_devices_events = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_events");
                permissions_devices_remote_shell = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_remote_shell");
                permissions_devices_remote_file_browser = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_remote_file_browser");
                permissions_devices_remote_control = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_remote_control");
                permissions_devices_deauthorize = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_deauthorize");
                permissions_devices_move = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "devices_move");
                permissions_tenants_list = await Classes.Authentication.Permissions.Get_Tenants(netlock_username, true);

                if (!permissions_devices_authorized_enabled)
                    logout = true;

                //Check tenant permissions
                if (tenant_guid == "all")
                {
                    has_all_tenants_permission = await Classes.Authentication.Permissions.Verify_Tenants_Full_Access(netlock_username);
                    Logging.Handler.Debug("/devices -> Permissions", "Tenant permission", "has_all_tenants_permission: " + has_all_tenants_permission);
                    Logging.Handler.Debug("/devices -> Permissions", "Tenant permission", "user has permissions to all tenants");
                }

                if (!has_all_tenants_permission)
                {
                    if (!permissions_devices_authorized_enabled || !permissions_tenants_list.Contains(tenant_guid) || permissions_tenants_list.Count == 0)
                    {
                        //maybe add deleting the tenant name from the browsers storage here
                        Logging.Handler.Debug("/devices -> OnInitializedAsync", "Tenant permission", "false");
                        logout = true;
                    }
                }

                if (logout) // Redirect to the login page
                {
                    NavigationManager.NavigateTo("/logout", true);
                    return false;
                }

                // All fine? Nice.
                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/dashboard -> Permissions", "Error", ex.ToString());
                return false;
            }
        }

        #endregion

        private bool _isDarkMode = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AfterInitializedAsync();
            }
        }

        private async Task AfterInitializedAsync()
        {
            tenant_guid = await localStorage.GetItemAsync<string>("tenant_guid");

            if (!await Permissions())
                return;

            loading_overlay = true;
            StateHasChanged();

            date = Localizer["date"];
            device_information_events_severity_string = Localizer["any"];
            events_type_string = Localizer["any"];

            _isDarkMode = await JSRuntime.InvokeAsync<bool>("isDarkMode");

            disabled = true;

            devices_table_view_port = "70vh";

            await Get_Clients_OverviewAsync();

            string tenant_name = await localStorage.GetItemAsync<string>("tenant_name");
            group_name = await localStorage.GetItemAsync<string>("group_name");

            if (tenant_name == "all")
                group_name = Localizer["all_devices"];

            Update_Chart_Options();

            if (Configuration.Members_Portal.api_enabled)
                await Get_Members_Portal_License_Limit();

            await Remote_Setup_SignalR();

            loading_overlay = false;
            StateHasChanged();
        }

        private bool members_portal_license_limit_reached = false;
        private bool members_portal_licenses_hard_limit = false;
        private int members_portal_license_count = 0;
        private int members_portal_license_limit = 0;

        private async Task Get_Members_Portal_License_Limit()
        {
            try
            {
                members_portal_license_limit_reached = await Classes.Members_Portal.Handler.Check_License_Limit_Reached();

                members_portal_license_count = Convert.ToInt32(await Classes.MySQL.Handler.Quick_Reader("SELECT COUNT(*) FROM devices WHERE authorized = '1';", "COUNT(*)"));
                members_portal_license_limit = Convert.ToInt32(await Classes.MySQL.Handler.Quick_Reader("SELECT members_portal_licenses_max FROM settings;", "members_portal_licenses_max"));
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("System_Logs", "Get_Members_Portal_License_Limit", ex.ToString());
            }
        }

        public string[] drive_labels = { "Freier Speicher", "Belegter Speicher" };

        private bool loading_overlay = false;
        private bool expanded = false;
        private bool disabled = true;
        private string devices_table_view_port = "70vh";
        private string device_table_search_string = "";
        private int events_rows_per_page = 25;
        private static string date = String.Empty;

        private string tenant_id = String.Empty;
        private string location_id = String.Empty;
        private string tenant_guid = String.Empty;
        private string location_guid = String.Empty;
        private string group_id = String.Empty;

        //Device information
        public string agent_version = String.Empty;
        public string operating_system = String.Empty;
        public string architecture = String.Empty;
        public string platform = String.Empty;
        public string last_active_user = String.Empty;
        public string domain = String.Empty;
        public string antivirus_solution = String.Empty;
        public string firewall_status = String.Empty;
        public string last_access = String.Empty;
        public string last_boot = String.Empty;
        public string timezone = String.Empty;
        public string cpu = String.Empty;
        public string cpu_usage = "0";
        public string mainboard = String.Empty;
        public string gpu = String.Empty;
        public string ram = String.Empty;
        public string ram_usage = "0";
        public string tpm = String.Empty;
        public string environment_variables = String.Empty;
        public string ip_address_internal = String.Empty;
        public string ip_address_external = String.Empty;
        public string network_adapters = String.Empty;
        public string network_adapters_display_string = String.Empty;
        public string network_adapters_history_display_string = String.Empty;
        public string disks = String.Empty;
        public string disks_display_string = String.Empty;
        public string applications_installed = String.Empty;
        public string cronjobs = String.Empty;
        public string applications_logon = String.Empty;
        public string applications_scheduled_tasks = String.Empty;
        public string applications_services = String.Empty;
        public string applications_drivers = String.Empty;

        #region Device Table

        private int devicesTableRowsPerPage = 25;

        private bool Devices_Table_Filter_Func(MySQL_Entity row)
        {
            if (string.IsNullOrEmpty(device_table_search_string))
                return true;

            //Search logic for each column
            return row.device_name.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.tenant_name.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.location_name.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.group_name.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.agent_version.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.last_access.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.ip_address.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.operating_system.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.domain.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.antivirus_solution.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.firewall_status.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.last_active_user.Contains(device_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string devices_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private async void Devices_RowClickHandler(MySQL_Entity row)
        {
            loading_overlay = true;

            expanded = true;
            disabled = false;
            devices_table_view_port = "35vh";
            // Hier können Sie den Inhalt der ausgewählten Zeile verarbeiten
            devices_selectedRowContent = row.device_id;

            // notes
            notes_tenant_name = row.tenant_name;
            notes_location_name = row.location_name;
            notes_device_name = row.device_name;
            notes_device_id = row.device_id;
            notes_tenant_id = row.tenant_id;
            notes_location_id = row.location_id;

            await Get_Device_Information_Details(row.tenant_name, row.location_name, row.device_id);
            await CPU_Information_Load();
            await CPU_Usage_History_Graph_Load();
            await RAM_Information_Load();
            await RAM_Usage_History_Graph_Load();
            await Network_Information_Load();
            await Drives_Information_Load();
            await Antivirus_Products_Load();
            await Get_Antivirus_Information();
            await Software_Installed_Load();
            await Application_Logon_Load();
            await Applications_Scheduled_Tasks_Load();
            await Applications_Services_Load();
            await Applications_Drivers_Load();
            await Task_Manager_Load();
            await Device_Information_Remote_Shell_History_Load();
            
            support_history_mysql_data = await Get_Device_Support_History(false);

            (tenant_guid, location_guid) = await Classes.MySQL.Handler.Get_Tenant_Location_Guid(row.tenant_name, row.location_name);

            events_mysql_data = await Events_Load(row.device_id, true);

            if (String.IsNullOrEmpty(notes_string))
                notes_expanded = false;
            else
                notes_expanded = true;

            loading_overlay = false;

            StateHasChanged();
        }

        private async void Devices_Uptime_Monitoring_RowClickHandler(MySQL_Entity row)
        {
            await Devices_Update_Uptime_Monitorung_Enabled(row.device_id, row.uptime_monitoring_enabled);
        }

        private async Task Devices_Update_Uptime_Monitorung_Enabled(string device_id, bool enabled)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            int enabled_converted = 0;

            if (enabled)
                enabled_converted = 1;
            else
                enabled_converted = 0;

            try
            {
                await conn.OpenAsync();

                string execute_query = "UPDATE devices SET uptime_monitoring_enabled = @enabled_converted WHERE id = @device_id;";
                MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);

                cmd.Parameters.AddWithValue("@enabled_converted", enabled_converted);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Devices_Update_Uptime_Monitorung_Enabled", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private string Devices_GetRowClass(MySQL_Entity row)
        {
            return row.device_id == devices_selectedRowContent ? (_isDarkMode ? "selected-row-dark" : "selected-row-light") : String.Empty;
        }

        //Deauthorize device
        private async Task Deauthorize_Device(string device_id)
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
                BackgroundClass = "dialog-blurring",
            };

            bool? dialog_result = await DialogService.ShowMessageBox(Localizer["warning"], "Are you sure you want to withdraw the authorization of this device?", yesText: Localizer["confirm"], cancelText: Localizer["cancel"], options: options);

            bool state = Convert.ToBoolean(dialog_result == null ? "false" : "true");

            if (!state)
                return;

            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string execute_query = "UPDATE devices SET authorized = 0 WHERE id = @device_id;";

                MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);
                cmd.ExecuteNonQuery();

                this.Snackbar.Add(Localizer["device deauthorized"], Severity.Success);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Deauthorize_Device", "Result", ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }

            await AfterInitializedAsync();
        }

        private void Device_Information_Expansion_Status()
        {
            devices_table_view_port = expanded ? "35vh" : "70vh";

            StateHasChanged();
        }

        #endregion

        #region CPU Information

        public string cpu_information_string = String.Empty;

        public List<CPU_Information_Entity> cpu_information_mysql_data;

        public class CPU_Information_Entity
        {
            public string name { get; set; } = String.Empty;
            public string socket_designation { get; set; } = String.Empty;
            public string processor_id { get; set; } = String.Empty;
            public string revision { get; set; } = String.Empty;
            public string usage { get; set; } = String.Empty;
            public string voltage { get; set; } = String.Empty;
            public string currentclockspeed { get; set; } = String.Empty;
            public string processes { get; set; } = String.Empty;
            public string threads { get; set; } = String.Empty;
            public string handles { get; set; } = String.Empty;
            public string maxclockspeed { get; set; } = String.Empty;
            public string sockets { get; set; } = String.Empty;
            public string cores { get; set; } = String.Empty;
            public string logical_processors { get; set; } = String.Empty;
            public string virtualization { get; set; } = String.Empty;
            public string l1_cache { get; set; } = String.Empty;
            public string l2_cache { get; set; } = String.Empty;
            public string l3_cache { get; set; } = String.Empty;
        }

        private async Task CPU_Information_Load()
        {
            cpu_information_mysql_data = new List<CPU_Information_Entity>();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(cpu_information_string))
                {
                    CPU_Information_Entity cpuInfo = new CPU_Information_Entity();

                    // cpu_information_name
                    JsonElement name_element = document.RootElement.GetProperty("name");
                    cpuInfo.name = name_element.ToString();

                    // socket_designation
                    JsonElement socket_designation_element = document.RootElement.GetProperty("socket_designation");
                    cpuInfo.socket_designation = socket_designation_element.ToString();

                    // processor_id
                    JsonElement processor_id_element = document.RootElement.GetProperty("processor_id");
                    cpuInfo.processor_id = processor_id_element.ToString();

                    // revision
                    JsonElement revision_element = document.RootElement.GetProperty("revision");
                    cpuInfo.revision = revision_element.ToString();

                    // usage
                    JsonElement usage_element = document.RootElement.GetProperty("usage");
                    cpuInfo.usage = usage_element.ToString();

                    // voltage
                    JsonElement voltage_element = document.RootElement.GetProperty("voltage");
                    cpuInfo.voltage = voltage_element.ToString();

                    // currentclockspeed
                    JsonElement currentclockspeed_element = document.RootElement.GetProperty("currentclockspeed");
                    cpuInfo.currentclockspeed = currentclockspeed_element.ToString();

                    // processes
                    JsonElement processes_element = document.RootElement.GetProperty("processes");
                    cpuInfo.processes = processes_element.ToString();

                    // threads
                    JsonElement threads_element = document.RootElement.GetProperty("threads");
                    cpuInfo.threads = threads_element.ToString();

                    // handles
                    JsonElement handles_element = document.RootElement.GetProperty("handles");
                    cpuInfo.handles = handles_element.ToString();

                    // maxclockspeed
                    JsonElement maxclockspeed_element = document.RootElement.GetProperty("maxclockspeed");
                    cpuInfo.maxclockspeed = maxclockspeed_element.ToString();

                    // sockets
                    JsonElement sockets_element = document.RootElement.GetProperty("sockets");
                    cpuInfo.sockets = sockets_element.ToString();

                    // cores
                    JsonElement cores_element = document.RootElement.GetProperty("cores");
                    cpuInfo.cores = cores_element.ToString();

                    // logical_processors
                    JsonElement logical_processors_element = document.RootElement.GetProperty("logical_processors");
                    cpuInfo.logical_processors = logical_processors_element.ToString();

                    // virtualization
                    JsonElement virtualization_element = document.RootElement.GetProperty("virtualization");
                    cpuInfo.virtualization = virtualization_element.ToString();

                    // l1_cache
                    JsonElement l1_cache_element = document.RootElement.GetProperty("l1_cache");
                    cpuInfo.l1_cache = l1_cache_element.ToString();

                    // l2_cache
                    JsonElement l2_cache_element = document.RootElement.GetProperty("l2_cache");
                    cpuInfo.l2_cache = l2_cache_element.ToString();

                    // l3_cache
                    JsonElement l3_cache_element = document.RootElement.GetProperty("l3_cache");
                    cpuInfo.l3_cache = l3_cache_element.ToString();

                    // Füge cpuInfo zur Liste hinzu
                    cpu_information_mysql_data.Add(cpuInfo);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> CPU_Information_Load", "Result", ex.Message);
            }
        }

        private Color GetCpuUsageColor()
        {
            if (Convert.ToInt32(cpu_usage) < 50)
            {
                return Color.Info;   // low
            }
            else if (Convert.ToInt32(cpu_usage) < 80)
            {
                return Color.Warning; // moderate
            }
            else
            {
                return Color.Primary; // high
            }
        }

        #endregion

        #region CPU History

        private bool device_information_cpu_history_expanded = false;

        private List<Device_Information_CPU_History_Entity> device_information_cpu_history_mysql_data;

        public class Device_Information_CPU_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string socket_designation { get; set; } = String.Empty;
            public string processor_id { get; set; } = String.Empty;
            public string revision { get; set; } = String.Empty;
            public string usage { get; set; } = String.Empty;
            public string voltage { get; set; } = String.Empty;
            public string currentclockspeed { get; set; } = String.Empty;
            public string processes { get; set; } = String.Empty;
            public string threads { get; set; } = String.Empty;
            public string handles { get; set; } = String.Empty;
            public string maxclockspeed { get; set; } = String.Empty;
            public string sockets { get; set; } = String.Empty;
            public string cores { get; set; } = String.Empty;
            public string logical_processors { get; set; } = String.Empty;
            public string virtualization { get; set; } = String.Empty;
            public string l1_cache { get; set; } = String.Empty;
            public string l2_cache { get; set; } = String.Empty;
            public string l3_cache { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_CPU_History_Entity> device_information_cpu_history_groupDefinition = new TableGroupDefinition<Device_Information_CPU_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_cpu_history_table_view_port = "70vh";
        private string device_information_cpu_history_table_sorted_column;
        private string device_information_cpu_history_table_search_string = "";
        private MudDateRangePicker device_information_cpu_history_table_picker;
        private DateRange device_information_cpu_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_CPU_History_Table_Submit_Picker()
        {
            await device_information_cpu_history_table_picker.CloseAsync();

            device_information_cpu_history_mysql_data = await Device_Information_CPU_History_Load();

            await CPU_Usage_History_Graph_Load();

            StateHasChanged();
        }


        private bool Device_Information_CPU_History_Table_Filter_Func(Device_Information_CPU_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_cpu_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.socket_designation.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.processor_id.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.revision.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.usage.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.voltage.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.currentclockspeed.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.processes.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.threads.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.handles.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.maxclockspeed.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.sockets.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cores.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.logical_processors.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.virtualization.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.l1_cache.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.l2_cache.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.l3_cache.Contains(device_information_cpu_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string cpu_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Device_Information_CPU_History_RowClickHandler(Device_Information_CPU_History_Entity row)
        {
            cpu_history_selectedRowContent = row.date;
        }

        private string Device_Information_CPU_History_GetRowClass(Device_Information_CPU_History_Entity row)
        {
            return row.date == cpu_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Device_Information_CPU_History_Entity>> Device_Information_CPU_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_cpu_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Device_Information_CPU_History_Entity> result = new List<Device_Information_CPU_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_cpu_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_cpu_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> CPU_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> CPU_History_Load", "MySQL_Result", reader["json"].ToString());

                            try
                            {
                                JsonObject cpu_history_object = JsonNode.Parse(reader["json"].ToString()).AsObject();

                                Device_Information_CPU_History_Entity entity = new Device_Information_CPU_History_Entity
                                {
                                    date = reader["date"].ToString(),
                                    name = cpu_history_object["name"].ToString(),
                                    socket_designation = cpu_history_object["socket_designation"].ToString(),
                                    processor_id = cpu_history_object["processor_id"].ToString(),
                                    revision = cpu_history_object["revision"].ToString(),
                                    usage = cpu_history_object["usage"].ToString(),
                                    voltage = cpu_history_object["voltage"].ToString(),
                                    currentclockspeed = cpu_history_object["currentclockspeed"].ToString(),
                                    processes = cpu_history_object["processes"].ToString(),
                                    threads = cpu_history_object["threads"].ToString(),
                                    handles = cpu_history_object["handles"].ToString(),
                                    maxclockspeed = cpu_history_object["maxclockspeed"].ToString(),
                                    sockets = cpu_history_object["sockets"].ToString(),
                                    cores = cpu_history_object["cores"].ToString(),
                                    logical_processors = cpu_history_object["logical_processors"].ToString(),
                                    virtualization = cpu_history_object["virtualization"].ToString(),
                                    l1_cache = cpu_history_object["l1_cache"].ToString(),
                                    l2_cache = cpu_history_object["l2_cache"].ToString(),
                                    l3_cache = cpu_history_object["l3_cache"].ToString(),
                                };

                                result.Add(entity);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/devices -> CPU_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> CPU_History_Load", "MySQL_Query", ex.Message);
                return new List<Device_Information_CPU_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_CPU_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("cpu_history");
        }

        public class Cpu_History_Graph_Json
        {
            public string? usage { get; set; }
        }

        private int cpu_usage_history_graph_index = -1; //default value cannot be 0 -> first selectedindex is 0.
        private ChartOptions cpu_usage_history_graph_options = new ChartOptions
        {
            YAxisLines = false,
            YAxisTicks = 100,
            MaxNumYAxisTicks = 10,
            YAxisRequireZeroPoint = true,
            XAxisLines = true,
            LineStrokeWidth = 2,
            ShowLabels = true,
        };

        private AxisChartOptions cpu_usage_history_graph_axisChartOptions = new AxisChartOptions
        {
            MatchBoundsToSize = true,
            XAxisLabelRotation = 0,
        };

        private List<TimeSeriesChartSeries> cpu_usage_history_graph_series = new();
        private TimeSeriesChartSeries cpu_usage_history_graph_cpuSeries = new();

        private async Task CPU_Usage_History_Graph_Load()
        {
            try
            {
                var values = new List<TimeSeriesChartSeries.TimeValue>();

                await using var connection = new MySqlConnection(Configuration.MySQL.Connection_String);
                await connection.OpenAsync();

                var hasDateRange = device_information_cpu_history_table_dateRange?.Start != null &&
                                   device_information_cpu_history_table_dateRange?.End != null;

                var queryBuilder = new StringBuilder(@"
        SELECT `date`, `json` 
        FROM device_information_cpu_history 
        WHERE device_id = @deviceId");

                if (hasDateRange)
                    queryBuilder.Append(" AND `date` BETWEEN @startDate AND @endDate");

                queryBuilder.Append(" ORDER BY `date` DESC;");

                await using var cmd = new MySqlCommand(queryBuilder.ToString(), connection);
                cmd.Parameters.AddWithValue("@deviceId", notes_device_id);

                if (hasDateRange)
                {
                    cmd.Parameters.AddWithValue("@startDate", device_information_cpu_history_table_dateRange.Start);
                    cmd.Parameters.AddWithValue("@endDate", device_information_cpu_history_table_dateRange.End);
                }

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var timestamp = reader.GetDateTime("date");
                    var json = reader.GetString("json");

                    try
                    {
                        var data = JsonSerializer.Deserialize<Cpu_History_Graph_Json>(json);

                        if (data?.usage != null && double.TryParse(data.usage, NumberStyles.Float, CultureInfo.InvariantCulture, out double usage))
                        {
                            values.Add(new TimeSeriesChartSeries.TimeValue(timestamp, usage));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("/devices -> CPU_Usage_History_Graph_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                    }
                }

                cpu_usage_history_graph_cpuSeries = new TimeSeriesChartSeries
                {
                    Index = 0,
                    Name = "CPU utilisation (%)",
                    Data = values,
                    IsVisible = true,
                    LineDisplayType = LineDisplayType.Line,
                    DataMarkerTooltipTitleFormat = "{{X_VALUE}}",
                    DataMarkerTooltipSubtitleFormat = "{{Y_VALUE}} %",
                    ShowDataMarkers = true,
                };

                cpu_usage_history_graph_series.Clear();
                cpu_usage_history_graph_series.Add(cpu_usage_history_graph_cpuSeries);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> CPU_Usage_History_Graph_Load", "Result", ex.ToString());
            }
        }


        #endregion

        #region RAM Information

        string ram_information_string = String.Empty;

        public List<RAM_Information_Entity> ram_information_mysql_data;

        public class RAM_Information_Entity
        {
            public string name { get; set; } = String.Empty;
            public string available { get; set; } = String.Empty;
            public string assured { get; set; } = String.Empty;
            public string cache { get; set; } = String.Empty;
            public string outsourced_pool { get; set; } = String.Empty;
            public string not_outsourced_pool { get; set; } = String.Empty;
            public string speed { get; set; } = String.Empty;
            public string slots { get; set; } = String.Empty;
            public string slots_used { get; set; } = String.Empty;
            public string form_factor { get; set; } = String.Empty;
            public string hardware_reserved { get; set; } = String.Empty;
        }

        private async Task RAM_Information_Load()
        {
            ram_information_mysql_data = new List<RAM_Information_Entity>();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(ram_information_string))
                {
                    RAM_Information_Entity ramInfo = new RAM_Information_Entity();

                    // name
                    JsonElement name_element = document.RootElement.GetProperty("name");
                    ramInfo.name = name_element.ToString();

                    // available
                    JsonElement available_element = document.RootElement.GetProperty("available");
                    ramInfo.available = available_element.ToString();

                    // assured
                    JsonElement assured_element = document.RootElement.GetProperty("assured");
                    ramInfo.assured = assured_element.ToString();

                    // cache
                    JsonElement cache_element = document.RootElement.GetProperty("cache");
                    ramInfo.cache = cache_element.ToString();

                    // outsourced_pool
                    JsonElement outsourced_pool_element = document.RootElement.GetProperty("outsourced_pool");
                    ramInfo.outsourced_pool = outsourced_pool_element.ToString();

                    // not_outsourced_pool
                    JsonElement not_outsourced_pool_element = document.RootElement.GetProperty("not_outsourced_pool");
                    ramInfo.not_outsourced_pool = not_outsourced_pool_element.ToString();

                    // speed
                    JsonElement speed_element = document.RootElement.GetProperty("speed");
                    ramInfo.speed = speed_element.ToString();

                    // slots
                    JsonElement slots_element = document.RootElement.GetProperty("slots");
                    ramInfo.slots = slots_element.ToString();

                    // slots_used
                    JsonElement slots_used_element = document.RootElement.GetProperty("slots_used");
                    ramInfo.slots_used = slots_used_element.ToString();

                    // form_factor
                    JsonElement form_factor_element = document.RootElement.GetProperty("form_factor");
                    ramInfo.form_factor = form_factor_element.ToString();

                    // hardware_reserved
                    JsonElement hardware_reserved_element = document.RootElement.GetProperty("hardware_reserved");
                    ramInfo.hardware_reserved = hardware_reserved_element.ToString();

                    // Füge ramInfo zur Liste hinzu
                    ram_information_mysql_data.Add(ramInfo);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> RAM_Information_Load", "Result", ex.ToString());
            }
        }

        private Color GetRAMUsageColor()
        {
            if (Convert.ToInt32(ram_usage) < 50)
            {
                return Color.Info;   // low
            }
            else if (Convert.ToInt32(ram_usage) < 80)
            {
                return Color.Warning; // moderate
            }
            else
            {
                return Color.Primary; // high
            }
        }

        #endregion

        #region RAM Information History

        private bool ram_history_history_expanded = false;

        private List<RAM_History_Entity> ram_history_mysql_data;

        public class RAM_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string available { get; set; } = String.Empty;
            public string assured { get; set; } = String.Empty;
            public string cache { get; set; } = String.Empty;
            public string outsourced_pool { get; set; } = String.Empty;
            public string not_outsourced_pool { get; set; } = String.Empty;
            public string speed { get; set; } = String.Empty;
            public string slots { get; set; } = String.Empty;
            public string slots_used { get; set; } = String.Empty;
            public string form_factor { get; set; } = String.Empty;
            public string hardware_reserved { get; set; } = String.Empty;
        }

        private TableGroupDefinition<RAM_History_Entity> ram_history_groupDefinition = new TableGroupDefinition<RAM_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string ram_history_table_view_port = "70vh";
        private string ram_history_table_sorted_column;
        private string ram_history_table_search_string = "";
        private MudDateRangePicker ram_history_table_picker;
        private DateRange ram_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date.AddDays(1));

        private async Task RAM_History_Table_Submit_Picker()
        {
            ram_history_table_picker.CloseAsync();

            ram_history_mysql_data = await Device_Information_RAM_History_Load();
        }

        private bool RAM_History_Table_Filter_Func(RAM_History_Entity row)
        {
            if (string.IsNullOrEmpty(ram_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.available.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.assured.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cache.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.outsourced_pool.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.not_outsourced_pool.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.speed.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.slots.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.slots_used.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.form_factor.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.hardware_reserved.Contains(ram_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string ram_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void RAM_History_RowClickHandler(RAM_History_Entity row)
        {
            ram_history_selectedRowContent = row.date;
        }

        private string RAM_History_GetRowClass(RAM_History_Entity row)
        {
            return row.date == ram_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<RAM_History_Entity>> Device_Information_RAM_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_ram_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<RAM_History_Entity> result = new List<RAM_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", ram_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", ram_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Device_Information_RAM_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Device_Information_RAM_History_Load", "MySQL_Result", reader["json"].ToString());

                            try
                            {
                                JsonObject ram_history_object = JsonNode.Parse(reader["json"].ToString()).AsObject();

                                RAM_History_Entity entity = new RAM_History_Entity
                                {
                                    date = reader["date"].ToString(),
                                    name = ram_history_object["name"].ToString(),
                                    available = ram_history_object["available"].ToString(),
                                    assured = ram_history_object["assured"].ToString(),
                                    cache = ram_history_object["cache"].ToString(),
                                    outsourced_pool = ram_history_object["outsourced_pool"].ToString(),
                                    not_outsourced_pool = ram_history_object["not_outsourced_pool"].ToString(),
                                    speed = ram_history_object["speed"].ToString(),
                                    slots = ram_history_object["slots"].ToString(),
                                    slots_used = ram_history_object["slots_used"].ToString(),
                                    form_factor = ram_history_object["form_factor"].ToString(),
                                    hardware_reserved = ram_history_object["hardware_reserved"].ToString(),
                                };

                                result.Add(entity);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/devices -> Device_Information_RAM_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Device_Information_RAM_History_Load", "MySQL_Query", ex.Message);
                return new List<RAM_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_RAM_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("ram_history");
        }

        public class RAM_History_Graph_Json
        {
            public string? available { get; set; }
        }

        private int ram_usage_history_graph_index = -1; //default value cannot be 0 -> first selectedindex is 0.
        private ChartOptions ram_usage_history_graph_options = new ChartOptions
        {
            YAxisLines = false,
            YAxisTicks = 100,
            MaxNumYAxisTicks = 10,
            YAxisRequireZeroPoint = true,
            XAxisLines = true,
            LineStrokeWidth = 2,
            ShowLabels = true,
        };

        private AxisChartOptions ram_usage_history_graph_axisChartOptions = new AxisChartOptions
        {
            MatchBoundsToSize = true,
            XAxisLabelRotation = 0,
        };

        private List<TimeSeriesChartSeries> ram_usage_history_graph_series = new();
        private TimeSeriesChartSeries ram_usage_history_graph_ramSeries = new();

        private async Task RAM_Usage_History_Graph_Load()
        {
            try
            {
                var values = new List<TimeSeriesChartSeries.TimeValue>();

                await using var connection = new MySqlConnection(Configuration.MySQL.Connection_String);
                await connection.OpenAsync();

                // 1. Gesamten RAM vom Gerät abrufen
                double totalRamGB = 0;
                await using (var ramCmd = new MySqlCommand("SELECT ram FROM devices WHERE id = @deviceId;", connection))
                {
                    ramCmd.Parameters.AddWithValue("@deviceId", notes_device_id);
                    var result = await ramCmd.ExecuteScalarAsync();

                    if (result != null && double.TryParse(result.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedRam))
                    {
                        totalRamGB = parsedRam;
                    }
                    else
                    {
                        Logging.Handler.Error("/devices -> RAM_Usage_History_Graph_Load", "RAM info missing or invalid", $"Device ID: {notes_device_id}");
                        return;
                    }
                }

                double totalRamMB = totalRamGB * 1024;

                // 2. Verlauf abrufen
                var hasDateRange = ram_history_table_dateRange?.Start != null &&
                                   ram_history_table_dateRange?.End != null;

                var queryBuilder = new StringBuilder(@"
SELECT `date`, `json` 
FROM device_information_ram_history 
WHERE device_id = @deviceId");

                if (hasDateRange)
                    queryBuilder.Append(" AND `date` BETWEEN @startDate AND @endDate");

                queryBuilder.Append(" ORDER BY `date` DESC;");

                await using var cmd = new MySqlCommand(queryBuilder.ToString(), connection);
                cmd.Parameters.AddWithValue("@deviceId", notes_device_id);

                if (hasDateRange)
                {
                    cmd.Parameters.AddWithValue("@startDate", ram_history_table_dateRange.Start);
                    cmd.Parameters.AddWithValue("@endDate", ram_history_table_dateRange.End);
                }

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var timestamp = reader.GetDateTime("date");
                    var json = reader.GetString("json");

                    try
                    {
                        var data = JsonSerializer.Deserialize<RAM_History_Graph_Json>(json);

                        if (data != null && double.TryParse(data.available, NumberStyles.Float, CultureInfo.InvariantCulture, out double availableMB))
                        {
                            var usagePercent = 100 - (availableMB / totalRamMB * 100);
                            usagePercent = Math.Round(usagePercent, 1); // auf eine Nachkommastelle

                            values.Add(new TimeSeriesChartSeries.TimeValue(timestamp, usagePercent));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("/devices -> RAM_Usage_History_Graph_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                    }
                }

                ram_usage_history_graph_ramSeries = new TimeSeriesChartSeries
                {
                    Index = 0,
                    Name = "RAM-Auslastung (%)",
                    Data = values,
                    IsVisible = true,
                    LineDisplayType = LineDisplayType.Line,
                    DataMarkerTooltipTitleFormat = "{{X_VALUE}}",
                    DataMarkerTooltipSubtitleFormat = "{{Y_VALUE}} %",
                    ShowDataMarkers = true,
                };

                ram_usage_history_graph_series.Clear();
                ram_usage_history_graph_series.Add(ram_usage_history_graph_ramSeries);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> RAM_Usage_History_Graph_Load", "Result", ex.ToString());
            }
        }

        #endregion

        #region Network Information

        string network_information_string = String.Empty;

        public List<Device_Information_Network_Entity> device_information_network_information_mysql_data;

        public class Device_Information_Network_Entity
        {
            public string name { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string type { get; set; } = String.Empty;
            public string link_speed { get; set; } = String.Empty;
            public string service_name { get; set; } = String.Empty;
            public string dns_domain { get; set; } = String.Empty;
            public string dns_hostname { get; set; } = String.Empty;
            public string dhcp_enabled { get; set; } = String.Empty;
            public string dhcp_server { get; set; } = String.Empty;
            public string ipv4_address { get; set; } = String.Empty;
            public string ipv6_address { get; set; } = String.Empty;
            public string subnet_mask { get; set; } = String.Empty;
            public string mac_address { get; set; } = String.Empty;
            public string sending { get; set; } = String.Empty;
            public string receive { get; set; } = String.Empty;
        }

        private async Task Network_Information_Load()
        {
            try
            {
                device_information_network_information_mysql_data = new List<Device_Information_Network_Entity>();

                JsonArray network_adapters_array = JsonNode.Parse(network_adapters).AsArray();

                foreach (var adapter in network_adapters_array)
                {
                    Device_Information_Network_Entity networkInfo = new Device_Information_Network_Entity();

                    networkInfo.name = adapter["name"]?.ToString() ?? "N/A";
                    networkInfo.description = adapter["description"]?.ToString() ?? "N/A";
                    networkInfo.type = adapter["type"]?.ToString() ?? "N/A";
                    networkInfo.link_speed = adapter["link_speed"]?.ToString() ?? "N/A";
                    networkInfo.service_name = adapter["service_name"]?.ToString() ?? "N/A";
                    networkInfo.dns_domain = adapter["dns_domain"]?.ToString() ?? "N/A";
                    networkInfo.dns_hostname = adapter["dns_hostname"]?.ToString() ?? "N/A";
                    networkInfo.dhcp_enabled = adapter["dhcp_enabled"]?.ToString() ?? "N/A";
                    networkInfo.dhcp_server = adapter["dhcp_server"]?.ToString() ?? "N/A";
                    networkInfo.ipv4_address = adapter["ipv4_address"]?.ToString() ?? "N/A";
                    networkInfo.ipv6_address = adapter["ipv6_address"]?.ToString() ?? "N/A";
                    networkInfo.subnet_mask = adapter["subnet_mask"]?.ToString() ?? "N/A";
                    networkInfo.mac_address = adapter["mac_address"]?.ToString() ?? "N/A";
                    networkInfo.sending = adapter["sending"]?.ToString() ?? "N/A";
                    networkInfo.receive = adapter["receive"]?.ToString() ?? "N/A";

                    // Füge networkInfo zur Liste hinzu
                    device_information_network_information_mysql_data.Add(networkInfo);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Network_Information_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Network Adapters History

        private bool device_information_network_adapters_history_expanded = false;

        private List<Device_Information_Network_Adapters_History_Entity> device_information_network_adapters_history_mysql_data;

        public class Device_Information_Network_Adapters_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string type { get; set; } = String.Empty;
            public string link_speed { get; set; } = String.Empty;
            public string service_name { get; set; } = String.Empty;
            public string dns_domain { get; set; } = String.Empty;
            public string dns_hostname { get; set; } = String.Empty;
            public string dhcp_enabled { get; set; } = String.Empty;
            public string dhcp_server { get; set; } = String.Empty;
            public string ipv4_address { get; set; } = String.Empty;
            public string ipv6_address { get; set; } = String.Empty;
            public string subnet_mask { get; set; } = String.Empty;
            public string mac_address { get; set; } = String.Empty;
            public string sending { get; set; } = String.Empty;
            public string receive { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_Network_Adapters_History_Entity> device_information_network_adapters_history_groupDefinition = new TableGroupDefinition<Device_Information_Network_Adapters_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_network_adapters_history_table_view_port = "70vh";
        private string device_information_network_adapters_history_table_sorted_column;
        private string device_information_network_adapters_history_table_search_string = "";
        private MudDateRangePicker device_information_network_adapters_history_table_picker;
        private DateRange device_information_network_adapters_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Network_Adapters_History_Table_Submit_Picker()
        {
            device_information_network_adapters_history_table_picker.CloseAsync();

            device_information_network_adapters_history_mysql_data = await Device_Information_Network_Adapters_History_Load();
        }

        private bool Device_Information_Network_Adapters_History_Table_Filter_Func(Device_Information_Network_Adapters_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_network_adapters_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.type.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.link_speed.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.service_name.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.dns_domain.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.dns_hostname.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.dhcp_enabled.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.dhcp_server.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ipv4_address.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ipv6_address.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.subnet_mask.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.mac_address.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.sending.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.receive.Contains(device_information_network_adapters_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string network_adapters_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Device_Information_Network_Adapters_History_RowClickHandler(Device_Information_Network_Adapters_History_Entity row)
        {
            network_adapters_history_selectedRowContent = row.date;
        }

        private string Device_Information_Network_Adapters_History_GetRowClass(Device_Information_Network_Adapters_History_Entity row)
        {
            return row.date == network_adapters_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Device_Information_Network_Adapters_History_Entity>> Device_Information_Network_Adapters_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_network_adapters_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Device_Information_Network_Adapters_History_Entity> result = new List<Device_Information_Network_Adapters_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_network_adapters_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_network_adapters_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Network_Adapters_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Network_Adapters_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray network_adapters_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var _object in network_adapters_array)
                            {
                                try
                                {
                                    Device_Information_Network_Adapters_History_Entity entity = new Device_Information_Network_Adapters_History_Entity
                                    {
                                        date = reader["date"].ToString(),

                                        name = _object["name"].ToString(),
                                        description = _object["description"].ToString(),
                                        type = _object["type"].ToString(),
                                        link_speed = _object["link_speed"].ToString(),
                                        service_name = _object["service_name"].ToString(),
                                        dns_domain = _object["dns_domain"].ToString(),
                                        dns_hostname = _object["dns_hostname"].ToString(),
                                        dhcp_enabled = _object["dhcp_enabled"].ToString(),
                                        dhcp_server = _object["dhcp_server"].ToString(),
                                        ipv4_address = _object["ipv4_address"].ToString(),
                                        ipv6_address = _object["ipv6_address"].ToString(),
                                        subnet_mask = _object["subnet_mask"].ToString(),
                                        mac_address = _object["mac_address"].ToString(),
                                        sending = _object["sending"].ToString(),
                                        receive = _object["receive"].ToString()
                                    };

                                    result.Add(entity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Network_Adapters_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Network_Adapters_History_Load", "MySQL_Query", ex.Message);
                return new List<Device_Information_Network_Adapters_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_Network_Adapters_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("network_adapters_history");
        }

        #endregion

        #region Software Installed Table

        public List<Software_Installed_Entity> software_installed_mysql_data;

        public class Software_Installed_Entity
        {
            public string name { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
            public string installation_date { get; set; } = String.Empty;
            public string installation_path { get; set; } = String.Empty;
            public string vendor { get; set; } = String.Empty;
            public string uninstallation_string { get; set; } = String.Empty;
        }

        private string software_installed_table_view_port = "70vh";
        private string software_installed_table_sorted_column;
        private string software_installed_table_search_string = "";

        private bool Software_Installed_Table_Filter_Func(Software_Installed_Entity row)
        {
            if (string.IsNullOrEmpty(software_installed_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_date.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_path.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.vendor.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.uninstallation_string.Contains(software_installed_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string software_installed_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Software_Installed_RowClickHandler(Software_Installed_Entity row)
        {
            software_installed_selectedRowContent = row.name;
        }

        private string Software_Installed_GetRowClass(Software_Installed_Entity row)
        {
            return row.name == software_installed_selectedRowContent ? "selected-row" : "";
        }

        private async Task Software_Installed_Load()
        {
            try
            {
                JsonArray software_installed_array = JsonNode.Parse(applications_installed).AsArray();

                software_installed_mysql_data = new List<Software_Installed_Entity>();

                foreach (var software in software_installed_array)
                {
                    Software_Installed_Entity softwareEntity = new Software_Installed_Entity
                    {
                        name = software["name"].ToString(),
                        version = software["version"].ToString(),
                        installation_date = software["installed_date"].ToString(),
                        installation_path = software["installation_path"].ToString(),
                        vendor = software["vendor"].ToString(),
                        uninstallation_string = software["uninstallation_string"].ToString()
                    };

                    software_installed_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Software_Installed_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Applications Installed Table History

        private bool applications_installed_history_expanded = false;

        private List<Applications_Installed_History_Entity> applications_installed_history_mysql_data;

        public class Applications_Installed_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
            public string installation_date { get; set; } = String.Empty;
            public string installation_path { get; set; } = String.Empty;
            public string vendor { get; set; } = String.Empty;
            public string uninstallation_string { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Applications_Installed_History_Entity> _groupDefinition = new TableGroupDefinition<Applications_Installed_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string applications_installed_history_table_view_port = "70vh";
        private string applications_installed_history_table_sorted_column;
        private string applications_installed_history_table_search_string = "";
        private MudDateRangePicker applications_installed_history_table_picker;
        private DateRange applications_installed_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Applications_Installed_History_Table_Submit_Picker()
        {
            applications_installed_history_table_picker.CloseAsync();

            applications_installed_history_mysql_data = await Applications_Installed_History_Load();
        }

        private bool Applications_Installed_History_Table_Filter_Func(Applications_Installed_History_Entity row)
        {
            if (string.IsNullOrEmpty(applications_installed_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_date.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_path.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.vendor.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.uninstallation_string.Contains(applications_installed_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_installed_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Installed_History_RowClickHandler(Applications_Installed_History_Entity row)
        {
            applications_installed_history_selectedRowContent = row.name;
        }

        private string Applications_Installed_History_GetRowClass(Applications_Installed_History_Entity row)
        {
            return row.name == applications_installed_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Applications_Installed_History_Entity>> Applications_Installed_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM applications_installed_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Applications_Installed_History_Entity> result = new List<Applications_Installed_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", applications_installed_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", applications_installed_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Applications_Installed_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Applications_Installed_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray applications_installed_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in applications_installed_history_array)
                            {
                                try
                                {
                                    Applications_Installed_History_Entity softwareEntity = new Applications_Installed_History_Entity
                                    {
                                        name = software["name"].ToString(),
                                        date = reader["date"].ToString(),
                                        version = software["version"].ToString(),
                                        installation_date = software["installed_date"].ToString(),
                                        installation_path = software["installation_path"].ToString(),
                                        vendor = software["vendor"].ToString(),
                                        uninstallation_string = software["uninstallation_string"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Applications_Installed_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Installed_History_Load", "MySQL_Query", ex.Message);
                return new List<Applications_Installed_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Applications Logon Table

        public List<Application_Logon_Entity> application_logon_mysql_data;

        public class Application_Logon_Entity
        {
            public string name { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string command { get; set; } = String.Empty;
            public string user { get; set; } = String.Empty;
            public string user_sid { get; set; } = String.Empty;
        }

        private string application_logon_table_view_port = "70vh";
        private string application_logon_table_sorted_column;
        private string application_logon_table_search_string = "";

        private bool Application_Logon_Table_Filter_Func(Application_Logon_Entity row)
        {
            if (string.IsNullOrEmpty(application_logon_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(application_logon_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(application_logon_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.command.Contains(application_logon_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user.Contains(application_logon_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user_sid.Contains(application_logon_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string application_logon_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Application_Logon_RowClickHandler(Application_Logon_Entity row)
        {
            application_logon_selectedRowContent = row.name;
        }

        private string Application_Logon_GetRowClass(Application_Logon_Entity row)
        {
            return row.name == application_logon_selectedRowContent ? "selected-row" : "";
        }

        private async Task Application_Logon_Load()
        {
            try
            {
                JsonArray application_logon_array = JsonNode.Parse(applications_logon).AsArray();

                application_logon_mysql_data = new List<Application_Logon_Entity>();

                foreach (var software in application_logon_array)
                {
                    Application_Logon_Entity softwareEntity = new Application_Logon_Entity
                    {
                        name = software["name"].ToString(),
                        path = software["path"].ToString(),
                        command = software["command"].ToString(),
                        user = software["user"].ToString(),
                        user_sid = software["user_sid"].ToString(),
                    };

                    application_logon_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Application_Logon_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Applications Logon History Table

        private bool applications_logon_history_expanded = false;

        private List<Applications_Logon_History_Entity> applications_logon_history_mysql_data;

        public class Applications_Logon_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string command { get; set; } = String.Empty;
            public string user { get; set; } = String.Empty;
            public string user_sid { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Applications_Logon_History_Entity> applications_logon_history_groupDefinition = new TableGroupDefinition<Applications_Logon_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string applications_logon_history_table_view_port = "70vh";
        private string applications_logon_history_table_sorted_column;
        private string applications_logon_history_table_search_string = "";
        private MudDateRangePicker applications_logon_history_table_picker;
        private DateRange applications_logon_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Applications_Logon_History_Table_Submit_Picker()
        {
            applications_logon_history_table_picker.CloseAsync();

            applications_logon_history_mysql_data = await Applications_Logon_History_Load();
        }

        private bool Applications_Logon_History_Table_Filter_Func(Applications_Logon_History_Entity row)
        {
            if (string.IsNullOrEmpty(applications_logon_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.command.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user_sid.Contains(applications_logon_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_logon_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Logon_History_RowClickHandler(Applications_Logon_History_Entity row)
        {
            applications_logon_history_selectedRowContent = row.name;
        }

        private string Applications_Logon_History_GetRowClass(Applications_Logon_History_Entity row)
        {
            return row.name == applications_logon_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Applications_Logon_History_Entity>> Applications_Logon_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM applications_logon_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Applications_Logon_History_Entity> result = new List<Applications_Logon_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", applications_logon_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", applications_logon_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Applications_Logon_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Applications_Logon_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray applications_logon_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in applications_logon_history_array)
                            {
                                try
                                {
                                    Applications_Logon_History_Entity softwareEntity = new Applications_Logon_History_Entity
                                    {
                                        name = software["name"].ToString(),
                                        date = reader["date"].ToString(),
                                        path = software["path"].ToString(),
                                        command = software["command"].ToString(),
                                        user = software["user"].ToString(),
                                        user_sid = software["user_sid"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Applications_Logon_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Logon_History_Load", "MySQL_Query", ex.Message);
                return new List<Applications_Logon_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }
        #endregion
        #region Applications Scheduled Tasks Table

        public List<Applications_Scheduled_Tasks_Entity> applications_scheduled_tasks_mysql_data;

        public class Applications_Scheduled_Tasks_Entity
        {
            public string name { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string folder { get; set; } = String.Empty;
            public string user_sid { get; set; } = String.Empty;
            public string next_execution { get; set; } = String.Empty;
            public string last_execution { get; set; } = String.Empty;
        }

        private string applications_scheduled_tasks_table_view_port = "70vh";
        private string applications_scheduled_tasks_table_sorted_column;
        private string applications_scheduled_tasks_table_search_string = "";

        private bool Applications_Scheduled_Tasks_Table_Filter_Func(Applications_Scheduled_Tasks_Entity row)
        {
            if (string.IsNullOrEmpty(applications_scheduled_tasks_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.author.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.folder.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user_sid.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.next_execution.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.last_execution.Contains(applications_scheduled_tasks_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_scheduled_tasks_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Scheduled_Tasks_RowClickHandler(Applications_Scheduled_Tasks_Entity row)
        {
            applications_scheduled_tasks_selectedRowContent = row.name;
        }

        private string Applications_Scheduled_Tasks_GetRowClass(Applications_Scheduled_Tasks_Entity row)
        {
            return row.name == applications_scheduled_tasks_selectedRowContent ? "selected-row" : "";
        }

        private async Task Applications_Scheduled_Tasks_Load()
        {
            try
            {
                JsonArray applications_scheduled_tasks_array = JsonNode.Parse(applications_scheduled_tasks).AsArray();

                applications_scheduled_tasks_mysql_data = new List<Applications_Scheduled_Tasks_Entity>();

                foreach (var software in applications_scheduled_tasks_array)
                {
                    Applications_Scheduled_Tasks_Entity softwareEntity = new Applications_Scheduled_Tasks_Entity
                    {
                        name = software["name"]?.ToString() ?? "N/A",
                        status = software["status"]?.ToString() ?? "N/A",
                        author = software["author"]?.ToString() ?? "N/A",
                        path = software["path"]?.ToString() ?? "N/A",
                        folder = software["folder"]?.ToString() ?? "N/A",
                        user_sid = software["user_sid"]?.ToString() ?? "N/A",
                        next_execution = software["next_execution"]?.ToString() ?? "N/A",
                        last_execution = software["last_execution"]?.ToString() ?? "N/A",
                    };

                    applications_scheduled_tasks_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Scheduled_Tasks_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Applications Scheduled Tasks History

        private bool applications_scheduled_tasks_history_expanded = false;

        private List<Applications_Scheduled_Tasks_History_Entity> applications_scheduled_tasks_history_mysql_data;

        public class Applications_Scheduled_Tasks_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string folder { get; set; } = String.Empty;
            public string user_sid { get; set; } = String.Empty;
            public string next_execution { get; set; } = String.Empty;
            public string last_execution { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Applications_Scheduled_Tasks_History_Entity> applications_scheduled_tasks_history_groupDefinition = new TableGroupDefinition<Applications_Scheduled_Tasks_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string applications_scheduled_tasks_history_table_view_port = "70vh";
        private string applications_scheduled_tasks_history_table_sorted_column;
        private string applications_scheduled_tasks_history_table_search_string = "";
        private MudDateRangePicker applications_scheduled_tasks_history_table_picker;
        private DateRange applications_scheduled_tasks_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Applications_Scheduled_Tasks_History_Table_Submit_Picker()
        {
            applications_scheduled_tasks_history_table_picker.CloseAsync();

            applications_scheduled_tasks_history_mysql_data = await Applications_Scheduled_Tasks_History_Load();
        }

        private bool Applications_Scheduled_Tasks_History_Table_Filter_Func(Applications_Scheduled_Tasks_History_Entity row)
        {
            if (string.IsNullOrEmpty(applications_scheduled_tasks_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.author.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.folder.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user_sid.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.next_execution.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.last_execution.Contains(applications_scheduled_tasks_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_scheduled_tasks_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Scheduled_Tasks_History_RowClickHandler(Applications_Scheduled_Tasks_History_Entity row)
        {
            applications_scheduled_tasks_history_selectedRowContent = row.name;
        }

        private string Applications_Scheduled_Tasks_History_GetRowClass(Applications_Scheduled_Tasks_History_Entity row)
        {
            return row.name == applications_scheduled_tasks_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Applications_Scheduled_Tasks_History_Entity>> Applications_Scheduled_Tasks_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM applications_scheduled_tasks_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Applications_Scheduled_Tasks_History_Entity> result = new List<Applications_Scheduled_Tasks_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", applications_scheduled_tasks_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", applications_scheduled_tasks_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Applications_Scheduled_Tasks_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Applications_Scheduled_Tasks_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray applications_scheduled_tasks_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in applications_scheduled_tasks_history_array)
                            {
                                try
                                {
                                    Applications_Scheduled_Tasks_History_Entity softwareEntity = new Applications_Scheduled_Tasks_History_Entity
                                    {
                                        name = software["name"].ToString(),
                                        date = reader["date"].ToString(),
                                        status = software["status"].ToString(),
                                        author = software["author"].ToString(),
                                        path = software["path"].ToString(),
                                        folder = software["folder"].ToString(),
                                        user_sid = software["user_sid"].ToString(),
                                        next_execution = software["next_execution"].ToString(),
                                        last_execution = software["last_execution"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Applications_Scheduled_Tasks_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Scheduled_Tasks_History_Load", "MySQL_Query", ex.Message);
                return new List<Applications_Scheduled_Tasks_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Applications Services Table

        public List<Applications_Services_Entity> applications_services_mysql_data;

        public class Applications_Services_Entity
        {
            public string display_name { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string start_type { get; set; } = String.Empty;
            public string login_as { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
        }

        private string applications_services_table_view_port = "70vh";
        private string applications_services_table_sorted_column;
        private string applications_services_table_search_string = "";
        private int applications_services_table_rowsPerPage = 25;
        private int applications_services_table_currentPage = 0;

        private bool Applications_Services_Table_Filter_Func(Applications_Services_Entity row)
        {
            if (string.IsNullOrEmpty(applications_services_table_search_string))
                return true;

            //Search logic for each column
            return row.display_name.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.start_type.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.login_as.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(applications_services_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_services_selectedRowContent = String.Empty; // Hier wird der Inhalt der ausgewählten Zeile gespeichert
        private string applications_services_selectedRow_Service_Status = String.Empty;

        // Der Handler für den TableRowClick-Event
        private void Applications_Services_RowClickHandler(Applications_Services_Entity row)
        {
            applications_services_selectedRowContent = row.name;
            applications_services_selectedRow_Service_Status = row.status;
        }

        private string Applications_Services_GetRowClass(Applications_Services_Entity row)
        {
            return row.name == applications_services_selectedRowContent ? (_isDarkMode ? "selected-row-dark" : "selected-row-light") : String.Empty;
        }

        private async Task Applications_Services_Load()
        {
            try
            {
                JsonArray applications_services_array = JsonNode.Parse(applications_services).AsArray();

                applications_services_mysql_data = new List<Applications_Services_Entity>();

                foreach (var software in applications_services_array)
                {
                    Applications_Services_Entity softwareEntity = new Applications_Services_Entity
                    {
                        display_name = software["display_name"].ToString(),
                        name = software["name"].ToString(),
                        status = software["status"].ToString(),
                        start_type = software["start_type"].ToString(),
                        login_as = software["login_as"].ToString(),
                        path = software["path"].ToString(),
                        description = software["description"].ToString(),
                    };

                    applications_services_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Services_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Applications Services History Table

        private bool applications_services_history_expanded = false;

        private List<Applications_Services_History_Entity> applications_services_history_mysql_data;

        public class Applications_Services_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string display_name { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string start_type { get; set; } = String.Empty;
            public string login_as { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Applications_Services_History_Entity> applications_services_history_groupDefinition = new TableGroupDefinition<Applications_Services_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string applications_services_history_table_view_port = "70vh";
        private string applications_services_history_table_sorted_column;
        private string applications_services_history_table_search_string = "";
        private MudDateRangePicker applications_services_history_table_picker;
        private DateRange applications_services_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Applications_Services_History_Table_Submit_Picker()
        {
            applications_services_history_table_picker.CloseAsync();

            applications_services_history_mysql_data = await Applications_Services_History_Load();
        }

        private bool Applications_Services_History_Table_Filter_Func(Applications_Services_History_Entity row)
        {
            if (string.IsNullOrEmpty(applications_services_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.display_name.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.name.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.status.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.start_type.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.login_as.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.path.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
            row.description.Contains(applications_services_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_services_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Services_History_RowClickHandler(Applications_Services_History_Entity row)
        {
            applications_services_history_selectedRowContent = row.name;
        }

        private string Applications_Services_History_GetRowClass(Applications_Services_History_Entity row)
        {
            return row.name == applications_services_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Applications_Services_History_Entity>> Applications_Services_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM applications_services_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Applications_Services_History_Entity> result = new List<Applications_Services_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", applications_services_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", applications_services_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Applications_Services_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Applications_Services_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray applications_services_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in applications_services_history_array)
                            {
                                try
                                {
                                    Applications_Services_History_Entity softwareEntity = new Applications_Services_History_Entity
                                    {
                                        date = reader["date"].ToString(),
                                        display_name = software["display_name"].ToString(),
                                        name = software["name"].ToString(),
                                        status = software["status"].ToString(),
                                        start_type = software["start_type"].ToString(),
                                        login_as = software["login_as"].ToString(),
                                        path = software["path"].ToString(),
                                        description = software["description"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Applications_Services_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Services_History_Load", "MySQL_Query", ex.Message);
                return new List<Applications_Services_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                loading_overlay = false;
            }
        }

        #endregion

        #region Applications Drivers Table

        public List<Applications_Drivers_Entity> applications_drivers_mysql_data;

        public class Applications_Drivers_Entity
        {
            public string display_name { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string type { get; set; } = String.Empty;
            public string start_type { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
        }

        private string applications_drivers_table_view_port = "70vh";
        private string applications_drivers_table_sorted_column;
        private string applications_drivers_table_search_string = "";

        private bool Applications_Drivers_Table_Filter_Func(Applications_Drivers_Entity row)
        {
            if (string.IsNullOrEmpty(applications_drivers_table_search_string))
                return true;

            //Search logic for each column
            return row.display_name.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.type.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.start_type.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(applications_drivers_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_drivers_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Drivers_RowClickHandler(Applications_Drivers_Entity row)
        {
            applications_drivers_selectedRowContent = row.name;
        }

        private string Applications_Drivers_GetRowClass(Applications_Drivers_Entity row)
        {
            return row.name == applications_drivers_selectedRowContent ? "selected-row" : "";
        }

        private async Task Applications_Drivers_Load()
        {
            try
            {
                JsonArray applications_drivers_array = JsonNode.Parse(applications_drivers).AsArray();

                applications_drivers_mysql_data = new List<Applications_Drivers_Entity>();

                foreach (var software in applications_drivers_array)
                {
                    Applications_Drivers_Entity softwareEntity = new Applications_Drivers_Entity
                    {
                        display_name = software["display_name"].ToString(),
                        name = software["name"].ToString(),
                        description = software["description"].ToString(),
                        status = software["status"].ToString(),
                        type = software["type"].ToString(),
                        start_type = software["start_type"].ToString(),
                        path = software["path"].ToString(),
                        version = software["version"].ToString(),
                    };

                    applications_drivers_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Drivers_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Applications Drivers History Table

        private bool applications_drivers_history_expanded = false;

        private List<Applications_Drivers_History_Entity> applications_drivers_history_mysql_data;

        public class Applications_Drivers_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string display_name { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
            public string type { get; set; } = String.Empty;
            public string start_type { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Applications_Drivers_History_Entity> applications_drivers_history_groupDefinition = new TableGroupDefinition<Applications_Drivers_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string applications_drivers_history_table_view_port = "70vh";
        private string applications_drivers_history_table_sorted_column;
        private string applications_drivers_history_table_search_string = "";
        private MudDateRangePicker applications_drivers_history_table_picker;
        private DateRange applications_drivers_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Applications_Drivers_History_Table_Submit_Picker()
        {
            applications_drivers_history_table_picker.CloseAsync();

            applications_drivers_history_mysql_data = await Applications_Drivers_History_Load();
        }

        private bool Applications_Drivers_History_Table_Filter_Func(Applications_Drivers_History_Entity row)
        {
            if (string.IsNullOrEmpty(applications_drivers_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.display_name.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.type.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.start_type.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(applications_drivers_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string applications_drivers_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Applications_Drivers_History_RowClickHandler(Applications_Drivers_History_Entity row)
        {
            applications_drivers_history_selectedRowContent = row.name;
        }

        private string Applications_Drivers_History_GetRowClass(Applications_Drivers_History_Entity row)
        {
            return row.name == applications_drivers_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Applications_Drivers_History_Entity>> Applications_Drivers_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM applications_drivers_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Applications_Drivers_History_Entity> result = new List<Applications_Drivers_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", applications_drivers_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", applications_drivers_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Applications_Drivers_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Applications_Drivers_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray applications_drivers_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in applications_drivers_history_array)
                            {
                                try
                                {
                                    Applications_Drivers_History_Entity softwareEntity = new Applications_Drivers_History_Entity
                                    {
                                        date = reader["date"].ToString(),
                                        display_name = software["display_name"].ToString(),
                                        name = software["name"].ToString(),
                                        description = software["description"].ToString(),
                                        status = software["status"].ToString(),
                                        type = software["type"].ToString(),
                                        start_type = software["start_type"].ToString(),
                                        path = software["path"].ToString(),
                                        version = software["version"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Applications_Drivers_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Applications_Drivers_History_Load", "MySQL_Query", ex.Message);
                return new List<Applications_Drivers_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Cronjobs Installed Table

        public List<Cronjobs_Entity> cronjobs_mysql_data;

        public class Cronjobs_Entity
        {
            public string name { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
            public string installation_date { get; set; } = String.Empty;
            public string installation_path { get; set; } = String.Empty;
            public string vendor { get; set; } = String.Empty;
            public string uninstallation_string { get; set; } = String.Empty;
        }

        private string cronjobs_table_view_port = "70vh";
        private string cronjobs_table_sorted_column;
        private string cronjobs_table_search_string = "";

        private bool Cronjobs_Table_Filter_Func(Cronjobs_Entity row)
        {
            if (string.IsNullOrEmpty(cronjobs_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_date.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_path.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.vendor.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.uninstallation_string.Contains(cronjobs_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string cronjobs_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Cronjobs_RowClickHandler(Cronjobs_Entity row)
        {
            cronjobs_selectedRowContent = row.name;
        }

        private string Cronjobs_GetRowClass(Cronjobs_Entity row)
        {
            return row.name == cronjobs_selectedRowContent ? "selected-row" : "";
        }

        private async Task Cronjobs_Load()
        {
            try
            {
                JsonArray cronjobs_array = JsonNode.Parse(cronjobs).AsArray();

                cronjobs_mysql_data = new List<Cronjobs_Entity>();

                foreach (var software in cronjobs_array)
                {
                    Cronjobs_Entity softwareEntity = new Cronjobs_Entity
                    {
                        name = software["name"].ToString(),
                        version = software["version"].ToString(),
                        installation_date = software["installed_date"].ToString(),
                        installation_path = software["installation_path"].ToString(),
                        vendor = software["vendor"].ToString(),
                        uninstallation_string = software["uninstallation_string"].ToString()
                    };

                    cronjobs_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Cronjobs_Load", "Result", ex.Message);
            }
        }

        #endregion

        #region Cronjobs Installed Table History

        private bool cronjobs_history_expanded = false;

        private List<Cronjobs_History_Entity> cronjobs_history_mysql_data;

        public class Cronjobs_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string version { get; set; } = String.Empty;
            public string installation_date { get; set; } = String.Empty;
            public string installation_path { get; set; } = String.Empty;
            public string vendor { get; set; } = String.Empty;
            public string uninstallation_string { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Cronjobs_History_Entity> cronjobs_groupDefinition = new TableGroupDefinition<Cronjobs_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string cronjobs_history_table_view_port = "70vh";
        private string cronjobs_history_table_sorted_column;
        private string cronjobs_history_table_search_string = "";
        private MudDateRangePicker cronjobs_history_table_picker;
        private DateRange cronjobs_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Cronjobs_History_Table_Submit_Picker()
        {
            cronjobs_history_table_picker.CloseAsync();

            cronjobs_history_mysql_data = await Cronjobs_History_Load();
        }

        private bool Cronjobs_History_Table_Filter_Func(Cronjobs_History_Entity row)
        {
            if (string.IsNullOrEmpty(cronjobs_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.version.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_date.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.installation_path.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.vendor.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.uninstallation_string.Contains(cronjobs_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string cronjobs_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Cronjobs_History_RowClickHandler(Cronjobs_History_Entity row)
        {
            cronjobs_history_selectedRowContent = row.name;
        }

        private string Cronjobs_History_GetRowClass(Cronjobs_History_Entity row)
        {
            return row.name == cronjobs_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Cronjobs_History_Entity>> Cronjobs_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM cronjobs_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Cronjobs_History_Entity> result = new List<Cronjobs_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", cronjobs_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", cronjobs_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Cronjobs_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Cronjobs_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray cronjobs_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in cronjobs_history_array)
                            {
                                try
                                {
                                    Cronjobs_History_Entity softwareEntity = new Cronjobs_History_Entity
                                    {
                                        name = software["name"].ToString(),
                                        date = reader["date"].ToString(),
                                        version = software["version"].ToString(),
                                        installation_date = software["installed_date"].ToString(),
                                        installation_path = software["installation_path"].ToString(),
                                        vendor = software["vendor"].ToString(),
                                        uninstallation_string = software["uninstallation_string"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Cronjobs_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Cronjobs_History_Load", "MySQL_Query", ex.Message);
                return new List<Cronjobs_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Antivirus Products

        private string antivirus_products_string = String.Empty;

        public List<Antivirus_Products_Entity> antivirus_products_mysql_data; //Datasource for table

        public class Antivirus_Products_Entity
        {
            public string display_name { get; set; } = String.Empty;
            public string instance_guid { get; set; } = String.Empty;
            public string path_to_signed_product_exe { get; set; } = String.Empty;
            public string path_to_signed_reporting_exe { get; set; } = String.Empty;
            public string product_state { get; set; } = String.Empty;
            public string timestamp { get; set; } = String.Empty;
        }

        private string antivirus_products_table_sorted_column;
        private string antivirus_products_table_search_string = String.Empty;

        private bool Antivirus_Products_Table_Filter_Func(Antivirus_Products_Entity row)
        {
            if (string.IsNullOrEmpty(antivirus_products_table_search_string))
                return true;

            //Search logic for each column
            return row.display_name.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.instance_guid.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path_to_signed_product_exe.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path_to_signed_reporting_exe.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.product_state.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.timestamp.Contains(antivirus_products_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string antivirus_products_selectedRowContent = ""; // Saving content of selected row

        // Executes on row click
        private void Antivirus_Products_RowClickHandler(Antivirus_Products_Entity row)
        {
            antivirus_products_selectedRowContent = row.instance_guid;
        }

        private string Antivirus_Products_GetRowClass(Antivirus_Products_Entity row)
        {
            return row.instance_guid == antivirus_products_selectedRowContent ? "selected-row" : "";
        }

        private async Task Antivirus_Products_Load()
        {
            try
            {
                JsonArray antivirus_products_array = JsonNode.Parse(antivirus_products_string).AsArray();

                antivirus_products_mysql_data = new List<Antivirus_Products_Entity>();

                foreach (var _object in antivirus_products_array)
                {
                    Antivirus_Products_Entity entity = new Antivirus_Products_Entity
                    {
                        display_name = _object["display_name"].ToString(),
                        instance_guid = _object["instance_guid"].ToString(),
                        path_to_signed_product_exe = _object["path_to_signed_product_exe"].ToString(),
                        path_to_signed_reporting_exe = _object["path_to_signed_reporting_exe"].ToString(),
                        product_state = _object["product_state"].ToString(),
                        timestamp = _object["timestamp"].ToString(),
                    };

                    antivirus_products_mysql_data.Add(entity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Antivirus_Products_Load", "Result", ex.Message);
            }
        }

        private async Task Export_Antivirus_Products_Table_Dialog()
        {
            await Show_Export_Table_Dialog("antivirus_products");
        }

        #endregion

        #region Antivirus Products History

        private bool antivirus_products_history_expanded = false;

        private List<Antivirus_Products_History_Entity> antivirus_products_history_mysql_data;

        public class Antivirus_Products_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string display_name { get; set; } = String.Empty;
            public string instance_guid { get; set; } = String.Empty;
            public string path_to_signed_product_exe { get; set; } = String.Empty;
            public string path_to_signed_reporting_exe { get; set; } = String.Empty;
            public string product_state { get; set; } = String.Empty;
            public string timestamp { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Antivirus_Products_History_Entity> antivirus_products_history_groupDefinition = new TableGroupDefinition<Antivirus_Products_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string antivirus_products_history_table_view_port = "70vh";
        private string antivirus_products_history_table_sorted_column;
        private string antivirus_products_history_table_search_string = "";
        private MudDateRangePicker antivirus_products_history_table_picker;
        private DateRange antivirus_products_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Antivirus_Products_History_Table_Submit_Picker()
        {
            antivirus_products_history_table_picker.CloseAsync();

            antivirus_products_history_mysql_data = await Antivirus_Products_History_Load();
        }

        private bool Antivirus_Products_History_Table_Filter_Func(Antivirus_Products_History_Entity row)
        {
            if (string.IsNullOrEmpty(antivirus_products_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.display_name.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.instance_guid.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path_to_signed_product_exe.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path_to_signed_reporting_exe.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.product_state.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.timestamp.Contains(antivirus_products_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string antivirus_products_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Antivirus_Products_History_RowClickHandler(Antivirus_Products_History_Entity row)
        {
            antivirus_products_history_selectedRowContent = row.instance_guid;
        }

        private string Antivirus_Products_History_GetRowClass(Antivirus_Products_History_Entity row)
        {
            return row.instance_guid == antivirus_products_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Antivirus_Products_History_Entity>> Antivirus_Products_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_antivirus_products_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Antivirus_Products_History_Entity> result = new List<Antivirus_Products_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", antivirus_products_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", antivirus_products_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Antivirus_Products_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Antivirus_Products_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray antivirus_products_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var software in antivirus_products_history_array)
                            {
                                try
                                {
                                    Antivirus_Products_History_Entity softwareEntity = new Antivirus_Products_History_Entity
                                    {
                                        date = reader["date"].ToString(),
                                        display_name = software["display_name"].ToString(),
                                        instance_guid = software["instance_guid"].ToString(),
                                        path_to_signed_product_exe = software["path_to_signed_product_exe"].ToString(),
                                        path_to_signed_reporting_exe = software["path_to_signed_reporting_exe"].ToString(),
                                        product_state = software["product_state"].ToString(),
                                        timestamp = software["timestamp"].ToString(),
                                    };

                                    result.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Antivirus_Products_History_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Antivirus_Products_History_Load", "MySQL_Query", ex.Message);
                return new List<Antivirus_Products_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_Antivirus_Products_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("antivirus_products_history");
        }

        #endregion

        private string group_name = null;
        private string group_name_displayed = String.Empty;

        private bool move_devices_dialog_open = false;

        private async Task Move_Devices_Dialog()
        {
            if (move_devices_dialog_open)
                return;

            await Get_Tenant_Location_Group_ID();

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("tenant_id", tenant_id);
            parameters.Add("location_id", location_id);
            parameters.Add("group_id", group_id);

            if (group_name == "all")
                parameters.Add("grouped", false);
            else
                parameters.Add("grouped", true);

            move_devices_dialog_open = true;

            var result = await DialogService.Show<Dialogs.Move_Devices_Dialog>(string.Empty, parameters, options).Result;

            move_devices_dialog_open = false;

            if (result.Canceled)
            {
                await Get_Clients_OverviewAsync();
                return;
            }

            Logging.Handler.Debug("/devices -> Move_Devices_Dialog", "Result", result.Data.ToString() ?? String.Empty);

            if (String.IsNullOrEmpty(result.Data.ToString()) == false && result.Data.ToString() != "error")
            {
                await Get_Clients_OverviewAsync();
            }
        }

        private bool move_device_dialog_open = false;

        private async Task Move_Device_Dialog()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("tenant_id", notes_tenant_id);
            parameters.Add("location_id", notes_location_id);
            parameters.Add("device_id", notes_device_id);

            move_device_dialog_open = false;

            var result = await DialogService.Show<Dialogs.Move_Device_Dialog>(string.Empty, parameters, options).Result;

            move_device_dialog_open = true;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/devices -> Move_Device_Dialog", "Result", result.Data.ToString() ?? String.Empty);

            if (String.IsNullOrEmpty(result.Data.ToString()) == false && result.Data.ToString() != "error")
            {
                await Get_Clients_OverviewAsync();
            }
        }

        // Get tenant & location & group id from database using guid
        private async Task Get_Tenant_Location_Group_ID()
        {
            string tenant_guid = await localStorage.GetItemAsync<string>("tenant_guid");
            string location_guid = await localStorage.GetItemAsync<string>("location_guid");
            group_name = await localStorage.GetItemAsync<string>("group_name");

            string query = "SELECT * FROM `tenants` WHERE guid = @tenant_guid;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@tenant_guid", tenant_guid);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            tenant_id = reader["id"].ToString() ?? String.Empty;
                        }
                    }
                }

                query = "SELECT * FROM `locations` WHERE guid = @location_guid;";

                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@location_guid", location_guid);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            location_id = reader["id"].ToString() ?? String.Empty;
                        }
                    }
                }

                if (group_name != "all")
                {
                    query = "SELECT * FROM `groups` WHERE name = @group_name;";

                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@group_name", group_name);

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                group_id = reader["id"].ToString() ?? String.Empty;
                            }
                        }
                    }
                }
                else
                    group_id = group_name;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Tenant_Location_Group_ID", "general_error", ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public class MySQL_Entity
        {
            public string device_id { get; set; } = "Empty";
            public string device_name { get; set; } = "Empty";
            public string tenant_name { get; set; } = "Empty";
            public string tenant_id { get; set; } = "Empty";
            public string location_name { get; set; } = "Empty";
            public string location_id { get; set; } = "Empty";
            public string group_name { get; set; } = "Empty";
            public string agent_version { get; set; } = "Empty";
            public string last_access { get; set; } = "Empty";
            public string ip_address { get; set; } = "Empty";
            public string operating_system { get; set; } = "Empty";
            public string domain { get; set; } = "Empty";
            public string antivirus_solution { get; set; } = "Empty";
            public string firewall_status { get; set; } = "Empty";
            public string platform { get; set; } = "Empty";
            public bool uptime_monitoring_enabled { get; set; } = false;
            public string last_active_user { get; set; } = "Empty";
        }

        public List<MySQL_Entity> mysql_data;

        private async Task Get_Clients_OverviewAsync()
        {
            string tenant_name = await localStorage.GetItemAsync<string>("tenant_name");
            string group_name = await localStorage.GetItemAsync<string>("group_name");
            string location_name = await localStorage.GetItemAsync<string>("location_name");
            string query = null;

            mysql_data = new List<MySQL_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command;

                if (tenant_name == "all")
                {
                    this.group_name_displayed = Localizer["all_devices"];
                    query = "SELECT * FROM devices WHERE authorized = '1';";
                    command = new MySqlCommand(query, conn);
                }
                else if (location_name == "all")
                {
                    this.group_name_displayed = tenant_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND tenant_name = @tenant_name;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                }
                else if (group_name == "all")
                {
                    this.group_name_displayed = tenant_name + "/" + location_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND location_name = @location_name AND tenant_name = @tenant_name;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@location_name", location_name);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                }
                else
                {
                    this.group_name_displayed = tenant_name + "/" + location_name + "/" + group_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND group_name = @group_name AND location_name = @location_name AND tenant_name = @tenant_name;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@group_name", group_name);
                    command.Parameters.AddWithValue("@location_name", location_name);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                }

                Logging.Handler.Debug("/devices -> Get_Clients_OverviewAsync", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            MySQL_Entity entity = new MySQL_Entity
                            {
                                device_id = reader["id"].ToString() ?? String.Empty,
                                device_name = reader["device_name"].ToString() ?? String.Empty,
                                tenant_name = reader["tenant_name"].ToString() ?? String.Empty,
                                tenant_id = reader["tenant_id"].ToString() ?? String.Empty,
                                location_name = reader["location_name"].ToString() ?? String.Empty,
                                location_id = reader["location_id"].ToString() ?? String.Empty,
                                group_name = reader["group_name"].ToString() ?? String.Empty,
                                agent_version = reader["agent_version"].ToString() ?? String.Empty,
                                last_access = reader["last_access"].ToString() ?? String.Empty,
                                ip_address = reader["ip_address_internal"].ToString() + " & " + reader["ip_address_external"].ToString(),
                                operating_system = reader["operating_system"].ToString() ?? String.Empty,
                                domain = reader["domain"].ToString() ?? String.Empty,
                                antivirus_solution = reader["antivirus_solution"].ToString() ?? String.Empty,
                                firewall_status = reader["firewall_status"].ToString() ?? String.Empty,
                                platform = reader["platform"].ToString() ?? String.Empty,
                                uptime_monitoring_enabled = (reader["uptime_monitoring_enabled"]?.ToString() == "1"),
                                last_active_user = reader["last_active_user"].ToString() ?? String.Empty,
                            };

                            mysql_data.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Clients_OverviewAsync", "MySQL_Query", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private async Task SearchDeviceByName()
        {
            if (String.IsNullOrEmpty(device_table_search_string))
            {
                await Get_Clients_OverviewAsync();
                return;
            }

            string tenant_name = await localStorage.GetItemAsync<string>("tenant_name");
            string group_name = await localStorage.GetItemAsync<string>("group_name");
            string location_name = await localStorage.GetItemAsync<string>("location_name");

            string query = null;
            mysql_data = new List<MySQL_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();
                MySqlCommand command;

                string deviceNameFilter = "%" + device_table_search_string + "%";

                if (tenant_name == "all")
                {
                    this.group_name_displayed = Localizer["all_devices"];
                    query = "SELECT * FROM devices WHERE authorized = '1' AND device_name LIKE @deviceName;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@deviceName", deviceNameFilter);
                }
                else if (location_name == "all")
                {
                    this.group_name_displayed = tenant_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND tenant_name = @tenant_name AND device_name LIKE @deviceName;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                    command.Parameters.AddWithValue("@deviceName", deviceNameFilter);
                }
                else if (group_name == "all")
                {
                    this.group_name_displayed = tenant_name + "/" + location_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND location_name = @location_name AND tenant_name = @tenant_name AND device_name LIKE @deviceName;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@location_name", location_name);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                    command.Parameters.AddWithValue("@deviceName", deviceNameFilter);
                }
                else
                {
                    this.group_name_displayed = tenant_name + "/" + location_name + "/" + group_name;
                    query = "SELECT * FROM devices WHERE authorized = '1' AND group_name = @group_name AND location_name = @location_name AND tenant_name = @tenant_name AND device_name LIKE @deviceName;";
                    command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@group_name", group_name);
                    command.Parameters.AddWithValue("@location_name", location_name);
                    command.Parameters.AddWithValue("@tenant_name", tenant_name);
                    command.Parameters.AddWithValue("@deviceName", deviceNameFilter);
                }

                Logging.Handler.Debug("/devices -> SearchDeviceByName", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            MySQL_Entity entity = new MySQL_Entity
                            {
                                device_id = reader["id"].ToString() ?? String.Empty,
                                device_name = reader["device_name"].ToString() ?? String.Empty,
                                tenant_name = reader["tenant_name"].ToString() ?? String.Empty,
                                tenant_id = reader["tenant_id"].ToString() ?? String.Empty,
                                location_name = reader["location_name"].ToString() ?? String.Empty,
                                location_id = reader["location_id"].ToString() ?? String.Empty,
                                group_name = reader["group_name"].ToString() ?? String.Empty,
                                agent_version = reader["agent_version"].ToString() ?? String.Empty,
                                last_access = reader["last_access"].ToString() ?? String.Empty,
                                ip_address = reader["ip_address_internal"].ToString() + " & " + reader["ip_address_external"].ToString(),
                                operating_system = reader["operating_system"].ToString() ?? String.Empty,
                                domain = reader["domain"].ToString() ?? String.Empty,
                                antivirus_solution = reader["antivirus_solution"].ToString() ?? String.Empty,
                                firewall_status = reader["firewall_status"].ToString() ?? String.Empty,
                                platform = reader["platform"].ToString() ?? String.Empty,
                                uptime_monitoring_enabled = (reader["uptime_monitoring_enabled"]?.ToString() == "1"),
                                last_active_user = reader["last_active_user"].ToString() ?? String.Empty,
                            };

                            mysql_data.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> SearchDeviceByName", "MySQL_Query", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private async Task Get_Device_Information_Details(string tenant_name, string location_name, string device_id)
        {
            string query = String.Empty;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            MySqlCommand command = new MySqlCommand(query, conn);

            try
            {
                await conn.OpenAsync();

                query = "SELECT * FROM devices WHERE authorized = '1' AND id = @device_id AND location_name = @location_name AND tenant_name = @tenant_name;";
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@tenant_name", tenant_name);
                command.Parameters.AddWithValue("@location_name", location_name);
                command.Parameters.AddWithValue("@device_id", device_id);

                Logging.Handler.Debug("/devices -> Get_Clients_OverviewAsync", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            agent_version = reader["agent_version"].ToString() ?? String.Empty;
                            last_access = reader["last_access"].ToString() ?? String.Empty;
                            operating_system = reader["operating_system"].ToString() ?? String.Empty;
                            domain = reader["domain"].ToString() ?? String.Empty;
                            architecture = reader["architecture"].ToString() ?? String.Empty;
                            platform = reader["platform"].ToString() ?? String.Empty;
                            antivirus_solution = reader["antivirus_solution"].ToString() ?? String.Empty;
                            firewall_status = reader["firewall_status"].ToString() ?? String.Empty;
                            last_boot = reader["last_boot"].ToString() ?? String.Empty;
                            timezone = reader["timezone"].ToString() ?? String.Empty;
                            cpu = reader["cpu"].ToString() ?? String.Empty;
                            cpu_usage = reader["cpu_usage"].ToString() ?? String.Empty;
                            mainboard = reader["mainboard"].ToString() ?? String.Empty;
                            gpu = reader["gpu"].ToString() ?? String.Empty;
                            ram = reader["ram"].ToString() ?? String.Empty;
                            ram_usage = reader["ram_usage"].ToString() ?? String.Empty;
                            tpm = reader["tpm"].ToString() ?? String.Empty;
                            environment_variables = reader["environment_variables"].ToString() ?? String.Empty;
                            ip_address_internal = reader["ip_address_internal"].ToString() ?? String.Empty;
                            ip_address_external = reader["ip_address_external"].ToString() ?? String.Empty;
                            network_adapters = reader["network_adapters"].ToString() ?? String.Empty;
                            disks = reader["disks"].ToString() ?? String.Empty;
                            cpu_information_string = reader["cpu_information"].ToString() ?? String.Empty;
                            ram_information_string = reader["ram_information"].ToString() ?? String.Empty;
                            applications_installed = reader["applications_installed"].ToString() ?? String.Empty;
                            cronjobs = reader["cronjobs"].ToString() ?? String.Empty;
                            applications_logon = reader["applications_logon"].ToString() ?? String.Empty;
                            applications_scheduled_tasks = reader["applications_scheduled_tasks"].ToString() ?? String.Empty;
                            applications_services = reader["applications_services"].ToString() ?? String.Empty;
                            applications_drivers = reader["applications_drivers"].ToString() ?? String.Empty;
                            task_manager_string = reader["processes"].ToString() ?? String.Empty;
                            notes_string = await Base64.Handler.Decode(reader["notes"].ToString()) ?? String.Empty;
                            notes_old_string = await Base64.Handler.Decode(reader["notes"].ToString()) ?? String.Empty;
                            antivirus_products_string = reader["antivirus_products"].ToString() ?? String.Empty;
                            antivirus_information_json = reader["antivirus_information"].ToString() ?? String.Empty;
                            antivirus_solution = reader["antivirus_solution"].ToString() ?? String.Empty;
                            last_active_user = reader["last_active_user"].ToString() ?? String.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Device_Information_Details", "MySQL_Query", ex.ToString());
            }
            finally
            {
                conn.Close();
            }
        }


        #region Device Information General History

        private bool device_information_general_history_expanded = false;

        private List<Device_Information_General_History_Entity> device_information_general_history_mysql_data;

        public class Device_Information_General_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string agent_version { get; set; } = String.Empty;
            public string operating_system { get; set; } = String.Empty;
            public string domain { get; set; } = String.Empty;
            public string architecture { get; set; } = String.Empty;
            public string antivirus_solution { get; set; } = String.Empty;
            public string firewall_status { get; set; } = String.Empty;
            public string last_boot { get; set; } = String.Empty;
            public string timezone { get; set; } = String.Empty;
            public string cpu { get; set; } = String.Empty;
            public string mainboard { get; set; } = String.Empty;
            public string gpu { get; set; } = String.Empty;
            public string ram { get; set; } = String.Empty;
            public string tpm { get; set; } = String.Empty;
            public string environment_variables { get; set; } = String.Empty;
            public string ip_address_internal { get; set; } = String.Empty;
            public string ip_address_external { get; set; } = String.Empty;
            public string network_adapters { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_General_History_Entity> device_information_general_history_groupDefinition = new TableGroupDefinition<Device_Information_General_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_general_history_table_view_port = "70vh";
        private string device_information_general_history_table_sorted_column;
        private string device_information_general_history_table_search_string = "";
        private string device_information_general_history_table_rows = "50";
        private MudDateRangePicker device_information_general_history_table_picker;
        private DateRange device_information_general_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_General_History_Table_Submit_Picker()
        {
            device_information_general_history_table_picker.CloseAsync();

            device_information_general_history_mysql_data = await Device_Information_General_History_Load();
        }

        private bool Device_Information_General_History_Table_Filter_Func(Device_Information_General_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_general_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.agent_version.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.operating_system.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.domain.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.architecture.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.antivirus_solution.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.firewall_status.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.last_boot.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.timezone.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cpu.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.mainboard.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.gpu.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ram.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.tpm.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.environment_variables.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ip_address_internal.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ip_address_external.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.network_adapters.Contains(device_information_general_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string device_information_general_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Device_Information_General_History_RowClickHandler(Device_Information_General_History_Entity row)
        {
            device_information_general_history_selectedRowContent = row.date;
        }

        private string Device_Information_General_History_GetRowClass(Device_Information_General_History_Entity row)
        {
            return row.date == device_information_general_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Device_Information_General_History_Entity>> Device_Information_General_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_general_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Device_Information_General_History_Entity> result = new List<Device_Information_General_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_general_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_general_history_table_dateRange.End.Value);


                Logging.Handler.Debug("/devices -> Device_Information_General_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Device_Information_General_History_Load", "MySQL_Result", reader["json"].ToString());

                            try
                            {
                                JsonObject device_information_general_history_object = JsonNode.Parse(reader["json"].ToString()).AsObject();

                                Device_Information_General_History_Entity softwareEntity = new Device_Information_General_History_Entity
                                {
                                    date = reader["date"].ToString(),
                                    ip_address_internal = reader["ip_address_internal"].ToString(),
                                    ip_address_external = reader["ip_address_external"].ToString(),
                                    network_adapters = reader["network_adapters"].ToString(),
                                    agent_version = device_information_general_history_object["agent_version"].ToString(),
                                    operating_system = device_information_general_history_object["operating_system"].ToString(),
                                    domain = device_information_general_history_object["domain"].ToString(),
                                    architecture = device_information_general_history_object["architecture"].ToString(),
                                    antivirus_solution = device_information_general_history_object["antivirus_solution"].ToString(),
                                    firewall_status = device_information_general_history_object["firewall_status"].ToString(),
                                    last_boot = device_information_general_history_object["last_boot"].ToString(),
                                    timezone = device_information_general_history_object["timezone"].ToString(),
                                    cpu = device_information_general_history_object["cpu"].ToString(),
                                    mainboard = device_information_general_history_object["mainboard"].ToString(),
                                    gpu = device_information_general_history_object["gpu"].ToString(),
                                    ram = device_information_general_history_object["ram"].ToString(),
                                    tpm = device_information_general_history_object["tpm"].ToString(),
                                    //environment_variables = device_information_general_history_object["environment_variables"].ToString(),
                                };

                                result.Add(softwareEntity);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/devices -> Device_Information_General_History_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Device_Information_General_History_Load", "MySQL_Query", ex.ToString());
                return new List<Device_Information_General_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Drives

        private List<Device_Information_Disks_Entity> device_information_disks_list = new List<Device_Information_Disks_Entity> { };

        public class Device_Information_Disks_Entity
        {
            public string letter { get; set; } = String.Empty;
            public string label { get; set; } = String.Empty;
            public string model { get; set; } = String.Empty;
            public string firmware_revision { get; set; } = String.Empty;
            public string serial_number { get; set; } = String.Empty;
            public string interface_type { get; set; } = String.Empty;
            public string drive_type { get; set; } = String.Empty;
            public string drive_format { get; set; } = String.Empty;
            public string drive_ready { get; set; } = String.Empty;
            public string capacity { get; set; } = String.Empty;
            public string usage { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
        }

        public async Task Drives_Information_Load()
        {
            try
            {
                device_information_disks_list.Clear();

                JsonArray device_information_disks__array = JsonNode.Parse(disks).AsArray();

                foreach (var disks in device_information_disks__array)
                {
                    Logging.Handler.Debug("/devices -> Get_Device_Information_Details", "MySQL_Result", disks.ToString());

                    Device_Information_Disks_Entity disksEntity = new Device_Information_Disks_Entity
                    {
                        letter = disks["letter"].ToString(),
                        label = disks["label"].ToString(),
                        model = disks["model"].ToString(),
                        firmware_revision = disks["firmware_revision"].ToString(),
                        serial_number = disks["serial_number"].ToString(),
                        interface_type = disks["interface_type"].ToString(),
                        drive_type = disks["drive_type"].ToString(),
                        drive_format = disks["drive_format"].ToString(),
                        drive_ready = disks["drive_ready"].ToString(),
                        capacity = disks["capacity"].ToString(),
                        usage = disks["usage"].ToString(),
                        status = disks["status"].ToString(),
                    };

                    device_information_disks_list.Add(disksEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Device_Information_Details", "Result", ex.ToString());
            }
        }

        #endregion

        #region Device Information Disks History

        private bool device_information_disks_history_expanded = false;

        private List<Device_Information_Disks_History_Entity> device_information_disks_history_mysql_data;

        public class Device_Information_Disks_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string letter { get; set; } = String.Empty;
            public string label { get; set; } = String.Empty;
            public string model { get; set; } = String.Empty;
            public string firmware_revision { get; set; } = String.Empty;
            public string serial_number { get; set; } = String.Empty;
            public string interface_type { get; set; } = String.Empty;
            public string drive_type { get; set; } = String.Empty;
            public string drive_format { get; set; } = String.Empty;
            public string drive_ready { get; set; } = String.Empty;
            public string capacity { get; set; } = String.Empty;
            public string usage { get; set; } = String.Empty;
            public string status { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_Disks_History_Entity> device_information_disks_history_groupDefinition = new TableGroupDefinition<Device_Information_Disks_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_disks_history_table_view_port = "70vh";
        private string device_information_disks_history_table_sorted_column;
        private string device_information_disks_history_table_search_string = "";
        private MudDateRangePicker device_information_disks_history_table_picker;
        private DateRange device_information_disks_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Disks_History_Table_Submit_Picker()
        {
            device_information_disks_history_table_picker.CloseAsync();

            device_information_disks_history_mysql_data = await Device_Information_Disks_History_Load();
        }

        private bool Device_Information_Disks_History_Table_Filter_Func(Device_Information_Disks_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_disks_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.letter.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.label.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.model.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.firmware_revision.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.serial_number.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.interface_type.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.drive_type.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.drive_format.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.drive_ready.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.capacity.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.usage.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.status.Contains(device_information_disks_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string device_information_disks_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Device_Information_Disks_History_RowClickHandler(Device_Information_Disks_History_Entity row)
        {
            device_information_disks_history_selectedRowContent = row.letter;
        }

        private string Device_Information_Disks_History_GetRowClass(Device_Information_Disks_History_Entity row)
        {
            return row.letter == device_information_disks_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Device_Information_Disks_History_Entity>> Device_Information_Disks_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_disks_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Device_Information_Disks_History_Entity> result = new List<Device_Information_Disks_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_disks_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_disks_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Device_Information_Disks_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Device_Information_Disks_History_Load", "MySQL_Result", reader["json"].ToString());

                            JsonArray device_information_disks_history_array = JsonNode.Parse(reader["json"].ToString()).AsArray();

                            foreach (var disks in device_information_disks_history_array)
                            {
                                try
                                {
                                    Device_Information_Disks_History_Entity disksEntity = new Device_Information_Disks_History_Entity
                                    {
                                        date = reader["date"].ToString(),
                                        letter = disks["letter"].ToString(),
                                        label = disks["label"].ToString(),
                                        model = disks["model"].ToString(),
                                        firmware_revision = disks["firmware_revision"].ToString(),
                                        serial_number = disks["serial_number"].ToString(),
                                        interface_type = disks["interface_type"].ToString(),
                                        drive_type = disks["drive_type"].ToString(),
                                        drive_format = disks["drive_format"].ToString(),
                                        drive_ready = disks["drive_ready"].ToString(),
                                        capacity = disks["capacity"].ToString(),
                                        usage = disks["usage"].ToString(),
                                        status = disks["status"].ToString(),
                                    };

                                    result.Add(disksEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Device_Information_Disks_History_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Device_Information_Disks_History_Load", "MySQL_Query", ex.Message);
                return new List<Device_Information_Disks_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_Disks_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("disks_history");
        }

        #endregion

        #region Performance Monitoring Ressources

        ChartOptions drive_chart_options = new ChartOptions();
        ChartOptions cpu_chart_options = new ChartOptions();
        ChartOptions ram_chart_options = new ChartOptions();
        ChartOptions network_chart_options = new ChartOptions();

        public void Update_Chart_Options()
        {
            //Select chart colors
            if (_isDarkMode)
            {
                string[] drive_colors = { "#f7f7f7", "#00bbff" }; // Dark gray for free, bright blue for occupied
                string[] cpu_colors = { "#00bbff", "#00bbff" };   // Bright blue for both
                string[] ram_colors = { "#b600ff", "#00bbff" };   // Bright purple for free, bright blue for occupied
                string[] network_colors = { "#ff6e00", "#00bbff" }; // Bright orange for free, bright blue for

                drive_chart_options.ChartPalette = drive_colors;
                cpu_chart_options.ChartPalette = cpu_colors;
                ram_chart_options.ChartPalette = ram_colors;
                network_chart_options.ChartPalette = network_colors;
            }
            else
            {
                string[] drive_colors = { "#303030", "#00bbff" };
                string[] cpu_colors = { "#00bbff", "#00bbff" };
                string[] ram_colors = { "#b600ff", "#00bbff" };
                string[] network_colors = { "#ff6e00", "#00bbff" };

                drive_chart_options.ChartPalette = drive_colors;
                cpu_chart_options.ChartPalette = cpu_colors;
                ram_chart_options.ChartPalette = ram_colors;
                network_chart_options.ChartPalette = network_colors;
            }
        }

        #endregion

        #region Task Manager

        public string task_manager_string = String.Empty;

        public List<Task_Manager_Entity> task_manager_mysql_data;

        public class Task_Manager_Entity
        {
            public string name { get; set; } = String.Empty;
            public string pid { get; set; } = String.Empty;
            public string parent_name { get; set; } = String.Empty;
            public string parent_pid { get; set; } = String.Empty;
            public string cpu { get; set; } = String.Empty;
            public string ram { get; set; } = String.Empty;
            public string user { get; set; } = String.Empty;
            public string created { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string cmd { get; set; } = String.Empty;
            public string handles { get; set; } = String.Empty;
            public string threads { get; set; } = String.Empty;
            public string read_operations { get; set; } = String.Empty;
            public string read_transfer { get; set; } = String.Empty;
            public string write_operations { get; set; } = String.Empty;
            public string write_transfer { get; set; } = String.Empty;
        }

        private string task_manager_table_view_port = "70vh";
        private string task_manager_table_sorted_column;
        private string task_manager_table_search_string = "";

        private bool Task_Manager_Table_Filter_Func(Task_Manager_Entity row)
        {
            if (string.IsNullOrEmpty(task_manager_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.pid.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.parent_name.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.parent_pid.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cpu.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ram.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.created.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cmd.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.handles.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.threads.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.read_operations.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.read_transfer.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.write_operations.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.write_transfer.Contains(task_manager_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string task_manager_selectedRowContent = String.Empty; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Task_Manager_RowClickHandler(Task_Manager_Entity row)
        {
            task_manager_selectedRowContent = row.pid;
        }

        private string Task_Manager_GetRowClass(Task_Manager_Entity row)
        {
            return row.pid == task_manager_selectedRowContent ? (_isDarkMode ? "selected-row-dark" : "selected-row-light") : String.Empty;
        }

        private async Task Task_Manager_Load()
        {
            try
            {
                JsonArray task_manager_array = JsonNode.Parse(task_manager_string).AsArray();

                task_manager_mysql_data = new List<Task_Manager_Entity>();

                foreach (var process in task_manager_array)
                {
                    Task_Manager_Entity softwareEntity = new Task_Manager_Entity
                    {
                        name = process["name"]?.ToString() ?? "N/A",
                        pid = process["pid"]?.ToString() ?? "N/A",
                        parent_name = process["parent_name"]?.ToString() ?? "N/A",
                        parent_pid = process["parent_pid"]?.ToString() ?? "N/A",
                        cpu = process["cpu"]?.ToString() ?? "N/A",
                        ram = process["ram"]?.ToString() ?? "N/A",
                        user = process["user"]?.ToString() ?? "N/A",
                        created = process["created"]?.ToString() ?? "N/A",
                        path = process["path"]?.ToString() ?? "N/A",
                        cmd = process["cmd"]?.ToString() ?? "N/A",
                        handles = process["handles"]?.ToString() ?? "N/A",
                        threads = process["threads"]?.ToString() ?? "N/A",
                        read_operations = process["read_operations"]?.ToString() ?? "N/A",
                        read_transfer = process["read_transfer"]?.ToString() ?? "N/A",
                        write_operations = process["write_operations"]?.ToString() ?? "N/A",
                        write_transfer = process["write_transfer"]?.ToString() ?? "N/A",
                    };

                    task_manager_mysql_data.Add(softwareEntity);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Task_Manager_Load", "Result", ex.ToString());
            }
        }

        #endregion

        #region Task Manager History

        private bool device_information_task_manager_history_expanded = false;

        public List<Task_Manager_History_Entity> task_manager_history_mysql_data; //Datasource for table

        public class Task_Manager_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string pid { get; set; } = String.Empty;
            public string parent_name { get; set; } = String.Empty;
            public string parent_pid { get; set; } = String.Empty;
            public string cpu { get; set; } = String.Empty;
            public string ram { get; set; } = String.Empty;
            public string user { get; set; } = String.Empty;
            public string created { get; set; } = String.Empty;
            public string path { get; set; } = String.Empty;
            public string cmd { get; set; } = String.Empty;
            public string handles { get; set; } = String.Empty;
            public string threads { get; set; } = String.Empty;
            public string read_operations { get; set; } = String.Empty;
            public string read_transfer { get; set; } = String.Empty;
            public string write_operations { get; set; } = String.Empty;
            public string write_transfer { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Task_Manager_History_Entity> task_manager_history_groupDefinition = new TableGroupDefinition<Task_Manager_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string task_manager_history_table_sorted_column;
        private string task_manager_history_table_search_string = String.Empty;
        private MudDateRangePicker device_information_task_manager_history_table_picker;
        private DateRange device_information_task_manager_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Task_Manager_History_Table_Submit_Picker()
        {
            device_information_task_manager_history_table_picker.CloseAsync();

            task_manager_history_mysql_data = await Task_Manager_History_Load();
        }

        private bool Task_Manager_History_Table_Filter_Func(Task_Manager_History_Entity row)
        {
            if (string.IsNullOrEmpty(task_manager_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.name.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.pid.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.parent_name.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.parent_pid.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cpu.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.ram.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.user.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.created.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.path.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.cmd.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.handles.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.threads.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.read_operations.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.read_transfer.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.write_operations.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.write_transfer.Contains(task_manager_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string task_manager_history_selectedRowContent = ""; // Saving content of selected row

        // Executes on row click
        private void Task_Manager_History_RowClickHandler(Task_Manager_History_Entity row)
        {
            task_manager_history_selectedRowContent = row.date;
        }

        private string Task_Manager_History_GetRowClass(Task_Manager_History_Entity row)
        {
            return row.date == task_manager_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Task_Manager_History_Entity>> Task_Manager_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_task_manager_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Task_Manager_History_Entity> result = new List<Task_Manager_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_task_manager_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_task_manager_history_table_dateRange.End.Value);

                Logging.Handler.Debug("Task_Manager_History", "MySQL_Prepared_Query", query); //Output prepared query

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("Task_Manager_History", "MySQL_Result", reader["json"].ToString()); //Output the JSON

                            JsonArray json_array = JsonNode.Parse(reader["json"].ToString()).AsArray(); //Transform json to array

                            foreach (var _object in json_array)
                            {
                                try
                                {
                                    Task_Manager_History_Entity entity = new Task_Manager_History_Entity //Create the entity
                                    {
                                        date = reader["date"].ToString(),
                                        name = _object["name"].ToString(),
                                        pid = _object["pid"].ToString(),
                                        parent_name = _object["parent_name"].ToString(),
                                        parent_pid = _object["parent_pid"].ToString(),
                                        cpu = _object["cpu"].ToString(),
                                        ram = _object["ram"].ToString(),
                                        user = _object["user"].ToString(),
                                        created = _object["created"].ToString(),
                                        path = _object["path"].ToString(),
                                        cmd = _object["cmd"].ToString(),
                                        handles = _object["handles"].ToString(),
                                        threads = _object["threads"].ToString(),
                                        read_operations = _object["read_operations"].ToString(),
                                        read_transfer = _object["read_transfer"].ToString(),
                                        write_operations = _object["write_operations"].ToString(),
                                        write_transfer = _object["write_transfer"].ToString(),
                                    };

                                    result.Add(entity); // Add the entity to the list
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("Task_Manager_History", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }

                return result; //Return the list
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Task_Manager_History", "MySQL_Query", ex.Message);
                return new List<Task_Manager_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private async Task Export_Task_Manager_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("task_manager_history");
        }

        #endregion

        #region Notes

        public string notes_string = String.Empty;
        public string notes_old_string = String.Empty;
        public bool notes_disabled = true;
        public string notes_tenant_name = String.Empty;
        public string notes_location_name = String.Empty;
        public string notes_device_name = String.Empty;
        public string notes_device_id = String.Empty;
        public string notes_location_id = String.Empty;
        public string notes_tenant_id = String.Empty;
        private bool notes_expanded = false;
        private async Task Notes_Edit_Form()
        {
            if (notes_disabled)
                notes_disabled = false;
            else
                notes_disabled = true;
        }

        private async Task Notes_Save()
        {
            Logging.Handler.Debug("/devices -> Notes_Save", "Info", $"{notes_tenant_name} {notes_location_name} {notes_device_name} notes: {notes_string}");

            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                notes_old_string = notes_string;

                await conn.OpenAsync();

                string execute_query = "UPDATE devices SET notes = @notes WHERE id = @device_id; INSERT INTO `device_information_notes_history` (`device_id`, `date`, `author`, `note`) VALUES (@device_id, @date, @author, @notes_old);";

                MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                cmd.Parameters.AddWithValue("@device_id", notes_device_id);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@notes", await Base64.Handler.Encode(notes_string));
                cmd.Parameters.AddWithValue("@notes_old", await Base64.Handler.Encode(notes_old_string));
                cmd.Parameters.AddWithValue("@author", netlock_username);

                cmd.ExecuteNonQuery();

                await Notes_Edit_Form();

                this.Snackbar.Add("Saved.", Severity.Success);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Notes_Save", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        #endregion

        #region Device Information Notes History

        private bool device_information_notes_history_expanded = false;

        private List<Device_Information_Notes_History_Entity> device_information_notes_history_mysql_data;

        public class Device_Information_Notes_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string note { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_Notes_History_Entity> device_information_notes_history_groupDefinition = new TableGroupDefinition<Device_Information_Notes_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_notes_history_table_view_port = "70vh";
        private string device_information_notes_history_table_sorted_column;
        private string device_information_notes_history_table_search_string = "";
        private MudDateRangePicker device_information_notes_history_table_picker;
        private DateRange device_information_notes_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Notes_History_Table_Submit_Picker()
        {
            await device_information_notes_history_table_picker.CloseAsync();

            device_information_notes_history_mysql_data = await Device_Information_Notes_History_Load();
        }

        private bool Device_Information_Notes_History_Table_Filter_Func(Device_Information_Notes_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_notes_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_notes_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.note.Contains(device_information_notes_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string device_information_notes_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Device_Information_Notes_History_RowClickHandler(Device_Information_Notes_History_Entity row)
        {
            device_information_notes_history_selectedRowContent = row.date;
        }

        private string Device_Information_Notes_History_GetRowClass(Device_Information_Notes_History_Entity row)
        {
            return row.date == device_information_notes_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Device_Information_Notes_History_Entity>> Device_Information_Notes_History_Load()
        {
            loading_overlay = true;

            string query = "SELECT * FROM device_information_notes_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Device_Information_Notes_History_Entity> result = new List<Device_Information_Notes_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_notes_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_notes_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Device_Information_Notes_History_Load", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Device_Information_Notes_History_Load", "MySQL_Result", reader["note"].ToString());

                            try
                            {
                                Device_Information_Notes_History_Entity softwareEntity = new Device_Information_Notes_History_Entity
                                {
                                    date = reader["date"].ToString(),
                                    author = reader["author"].ToString(),
                                    note = await Base64.Handler.Decode(reader["note"].ToString()),
                                };

                                result.Add(softwareEntity);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/devices -> Device_Information_Notes_History_Load", "MySQL_Query (corrupt json entry)", ex.ToString());
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Device_Information_Notes_History_Load", "MySQL_Query", ex.ToString());
                return new List<Device_Information_Notes_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        #endregion

        #region Support History

        public string support_history_string = String.Empty;

        public List<Support_History_Entity> support_history_mysql_data;

        public class Support_History_Entity
        {
            public string id { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string username { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
        }

        private string support_history_table_view_port = "70vh";
        private string support_history_table_sorted_column;
        private string support_history_table_search_string = "";
        private MudDateRangePicker device_information_support_history_table_picker;
        private DateRange device_information_support_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Support_History_Table_Submit_Picker()
        {
            device_information_support_history_table_picker.CloseAsync();

            support_history_mysql_data = await Get_Device_Support_History(true);
        }

        private bool Support_History_Table_Filter_Func(Support_History_Entity row)
        {
            if (string.IsNullOrEmpty(support_history_table_search_string))
                return true;

            //Search logic for each column
            return row.id.Contains(support_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.username.Contains(support_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.date.Contains(support_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(support_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string support_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Support_History_RowClickHandler(Support_History_Entity row)
        {
            support_history_selectedRowContent = row.id;
        }

        private string Support_History_GetRowClass(Support_History_Entity row)
        {
            return row.id == support_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task<List<Support_History_Entity>> Get_Device_Support_History(bool loading_overlay)
        {
            if (loading_overlay)
                loading_overlay = true;

            string query = "SELECT * FROM support_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                List<Support_History_Entity> result = new List<Support_History_Entity>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", notes_device_id);
                command.Parameters.AddWithValue("@start_date", device_information_support_history_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_support_history_table_dateRange.End.Value);

                Logging.Handler.Debug("/devices -> Get_Device_Support_History", "MySQL_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("/devices -> Get_Device_Support_History", "MySQL_Result", reader["username"].ToString() + " " + reader["date"].ToString() + " " + reader["description"].ToString());

                            try
                            {
                                Support_History_Entity entity = new Support_History_Entity
                                {
                                    id = reader["id"].ToString() ?? String.Empty,
                                    username = reader["username"].ToString() ?? String.Empty,
                                    date = reader["date"].ToString() ?? String.Empty,
                                    description = reader["description"].ToString() ?? String.Empty,
                                };

                                result.Add(entity);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/devices -> Get_Device_Support_History", "MySQL_Query (corrupt json entry)", ex.Message);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Device_Support_History", "MySQL_Query", ex.Message);
                return new List<Support_History_Entity>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                if (loading_overlay)
                    loading_overlay = true;
            }
        }

        #endregion

        #region Events

        private string events_type_string = String.Empty;

        public List<Events_Table> events_mysql_data; //Datasource for table

        public class Events_Table
        {
            public string id { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string severity { get; set; } = String.Empty;
            public string reported_by { get; set; } = String.Empty;
            public string _event { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string read { get; set; } = String.Empty;
            public string type { get; set; } = String.Empty;
        }

        private string events_table_sorted_column;
        private string events_table_search_string = String.Empty;
        private MudDateRangePicker device_information_events_table_picker;
        private DateRange device_information_events_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));
        private string device_information_events_severity_string = String.Empty;

        private async Task Device_Information_Events_Table_Submit()
        {
            device_information_events_table_picker.CloseAsync();

            events_mysql_data = await Events_Load(notes_device_id, true);
        }

        private bool Events_Table_Filter_Func(Events_Table row)
        {
            if (string.IsNullOrEmpty(events_table_search_string))
                return true;

            //Search logic for each column
            return row.id.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.date.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.severity.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.reported_by.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row._event.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.read.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.type.Contains(events_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string events_selectedRowContent = ""; // Saving content of selected row

        // Executes on row click
        private async Task Events_RowClickHandler(Events_Table row)
        {
            Logging.Handler.Debug("/dashboard -> Events_RowClickHandler", "row.id", row.id); //Output the selected row

            events_selectedRowContent = row.id;

            await Event_Details_Dialog(row.date, row.id, row.severity, row.reported_by, row._event, row.description);

            // Mark log as read
            await Classes.MySQL.Handler.Execute_Command("UPDATE `events` SET `read` = 1 WHERE `id` = " + Convert.ToInt32(row.id) + ";");

            // Remove row from table
            events_mysql_data = events_mysql_data.ToList();
            events_mysql_data.Remove(row);
        }

        private string Events_GetRowClass(Events_Table row)
        {
            return row.id == events_selectedRowContent ? "selected-row" : "";
        }

        int events_load_counter = 0;

        private async Task<List<Events_Table>> Events_Load(string device_id, bool bypass_events_load_counter)
        {
            if (events_load_counter != 0 && bypass_events_load_counter == false)
            {
                events_load_counter++;
                return new List<Events_Table>();
            }

            loading_overlay = true;

            string severity_condition = String.Empty;
            string type_condition = String.Empty;
            string read_condition = String.Empty;
            string tenant_condition = String.Empty;
            string location_condition = String.Empty;

            // Mapping severity string to integer
            if (device_information_events_severity_string == Localizer["low"])
                severity_condition = "AND severity = 0";
            else if (device_information_events_severity_string == Localizer["moderate"])
                severity_condition = "AND severity = 1";
            else if (device_information_events_severity_string == Localizer["high"])
                severity_condition = "AND severity = 2";
            else if (device_information_events_severity_string == Localizer["critical"])
                severity_condition = "AND severity = 3";

            // Mapping type string to integer
            if (events_type_string == Localizer["antivirus"])
                type_condition = "AND type = 0";
            else if (events_type_string == Localizer["job"])
                type_condition = "AND type = 1";
            else if (events_type_string == Localizer["sensor"])
                type_condition = "AND type = 2";

            // Construct the query
            string query = $@"
            SELECT *
            FROM events
            WHERE device_id = @device_id
            AND date >= @start_date
            AND date <= @end_date
            {severity_condition}
            {type_condition}
            ORDER BY date DESC;
        ";

            using MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            try
            {
                List<Events_Table> result = new List<Events_Table>();

                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", device_id);
                command.Parameters.AddWithValue("@start_date", device_information_events_table_dateRange.Start.Value);
                command.Parameters.AddWithValue("@end_date", device_information_events_table_dateRange.End.Value);


                Logging.Handler.Debug("Events", "MySQL_Prepared_Query", query); //Output prepared query

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            //Logging.Handler.Debug("Events", "MySQL_Result", reader["id"].ToString()); //Output the JSON

                            Events_Table entity = new Events_Table //Create the entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                severity = reader["severity"].ToString() ?? String.Empty,
                                reported_by = reader["reported_by"].ToString() ?? String.Empty,
                                _event = reader["_event"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                read = reader["read"].ToString() ?? String.Empty,
                                type = reader["type"].ToString() ?? String.Empty,
                            };

                            result.Add(entity); // Add the entity to the list
                        }
                    }
                }

                return result; //Return the list
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Events", "MySQL_Query", ex.ToString());
                return new List<Events_Table>(); // Return an empty list or handle the exception as needed
            }
            finally
            {
                conn.Close();
                loading_overlay = false;
            }
        }

        private bool event_details_dialog_open = false;

        private async Task Event_Details_Dialog(string date, string event_id, string severity, string reported_by, string _event, string description)
        {
            if (event_details_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Large,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("date", date);
            parameters.Add("event_id", event_id);
            parameters.Add("severity", severity);
            parameters.Add("reported_by", reported_by);
            parameters.Add("_event", _event);
            parameters.Add("description", description);

            event_details_dialog_open = true;

            var result = await this.DialogService.Show<Pages.Devices.Dialogs.Event_Details_Dialog>(string.Empty, parameters, options).Result;

            event_details_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/dashboard -> Event_Details_Dialog", "Result", result.Data.ToString());
        }

        private async Task Export_Events_Table_Dialog()
        {
            await Show_Export_Table_Dialog("events");
        }

        #endregion

        #region Antivirus Information

        string antivirus_information_json = String.Empty;

        string antivirus_information_amengineversion = String.Empty;
        string antivirus_information_amproductversion = String.Empty;
        bool antivirus_information_amserviceenabled = false;
        string antivirus_information_amserviceenabled_display = String.Empty;
        string antivirus_information_amserviceversion = String.Empty;
        bool antivirus_information_antispywareenabled = false;
        string antivirus_information_antispywareenabled_display = String.Empty;
        string antivirus_information_antispywaresignaturelastupdated = String.Empty;
        string antivirus_information_antispywaresignatureversion = String.Empty;
        bool antivirus_information_antivirusenabled = false;
        string antivirus_information_antivirusenabled_display = String.Empty;
        string antivirus_information_antivirussignaturelastupdated = String.Empty;
        string antivirus_information_antivirussignatureversion = String.Empty;
        bool antivirus_information_behaviormonitorenabled = false;
        string antivirus_information_behaviormonitorenabled_display = String.Empty;
        bool antivirus_information_ioavprotectionenabled = false;
        string antivirus_information_ioavprotectionenabled_display = String.Empty;
        bool antivirus_information_istamperprotected = false;
        string antivirus_information_istamperprotected_display = String.Empty;
        bool antivirus_information_nisenabled = false;
        string antivirus_information_nisenabled_display = String.Empty;
        string antivirus_information_nisengineversion = String.Empty;
        string antivirus_information_nissignaturelastupdated = String.Empty;
        string antivirus_information_nissignatureversion = String.Empty;
        bool antivirus_information_onaccessprotectionenabled = false;
        string antivirus_information_onaccessprotectionenabled_display = String.Empty;
        bool antivirus_information_realtimetprotectionenabled = false;
        string antivirus_information_realtimetprotectionenabled_display = String.Empty;

        private async Task Get_Antivirus_Information()
        {
            try
            {
                string antivirus_information_antispywaresignaturelastupdated_temp = String.Empty;
                string antivirus_information_antivirussignaturelastupdated_temp = String.Empty;
                string antivirus_information_nissignaturelastupdated_temp = String.Empty;


                // Deserialisierung des gesamten JSON-Strings
                using (JsonDocument document = JsonDocument.Parse(antivirus_information_json))
                {
                    antivirus_information_amengineversion = document.RootElement.GetProperty("amengineversion").ToString();
                    antivirus_information_amproductversion = document.RootElement.GetProperty("amproductversion").ToString();
                    antivirus_information_amserviceenabled = document.RootElement.GetProperty("amserviceenabled").GetBoolean();
                    antivirus_information_amserviceversion = document.RootElement.GetProperty("amserviceversion").ToString();
                    antivirus_information_antispywareenabled = document.RootElement.GetProperty("antispywareenabled").GetBoolean();
                    antivirus_information_antispywaresignaturelastupdated_temp = document.RootElement.GetProperty("antispywaresignaturelastupdated").ToString();
                    antivirus_information_antispywaresignatureversion = document.RootElement.GetProperty("antispywaresignatureversion").ToString();
                    antivirus_information_antivirusenabled = document.RootElement.GetProperty("antivirusenabled").GetBoolean();
                    antivirus_information_antivirussignaturelastupdated_temp = document.RootElement.GetProperty("antivirussignaturelastupdated").ToString();
                    antivirus_information_antivirussignatureversion = document.RootElement.GetProperty("antivirussignatureversion").ToString();
                    antivirus_information_behaviormonitorenabled = document.RootElement.GetProperty("behaviormonitorenabled").GetBoolean();
                    antivirus_information_ioavprotectionenabled = document.RootElement.GetProperty("ioavprotectionenabled").GetBoolean();
                    antivirus_information_istamperprotected = document.RootElement.GetProperty("istamperprotected").GetBoolean();
                    antivirus_information_nisenabled = document.RootElement.GetProperty("nisenabled").GetBoolean();
                    antivirus_information_nisengineversion = document.RootElement.GetProperty("nisengineversion").ToString();
                    antivirus_information_nissignaturelastupdated_temp = document.RootElement.GetProperty("nissignaturelastupdated").ToString();
                    antivirus_information_nissignatureversion = document.RootElement.GetProperty("nissignatureversion").ToString();
                    antivirus_information_onaccessprotectionenabled = document.RootElement.GetProperty("onaccessprotectionenabled").GetBoolean();
                    antivirus_information_realtimetprotectionenabled = document.RootElement.GetProperty("realtimetprotectionenabled").GetBoolean();
                }

                // Try parsing the antispyware signature last updated date
                try
                {
                    antivirus_information_antispywaresignaturelastupdated =
                        DateTime.ParseExact(antivirus_information_antispywaresignaturelastupdated_temp.Substring(0, 14),
                                            "yyyyMMddHHmmss",
                                            null,
                                            System.Globalization.DateTimeStyles.AssumeUniversal)
                        .ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    antivirus_information_antispywaresignaturelastupdated = antivirus_information_antispywaresignaturelastupdated_temp;
                }

                // Try parsing the antivirus signature last updated date
                try
                {
                    antivirus_information_antivirussignaturelastupdated =
                        DateTime.ParseExact(antivirus_information_antivirussignaturelastupdated_temp.Substring(0, 14),
                                            "yyyyMMddHHmmss",
                                            null,
                                            System.Globalization.DateTimeStyles.AssumeUniversal)
                        .ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    antivirus_information_antivirussignaturelastupdated = antivirus_information_antivirussignaturelastupdated_temp;
                }

                // Try parsing the NIS signature last updated date
                try
                {
                    antivirus_information_nissignaturelastupdated =
                        DateTime.ParseExact(antivirus_information_nissignaturelastupdated_temp.Substring(0, 14),
                                            "yyyyMMddHHmmss",
                                            null,
                                            System.Globalization.DateTimeStyles.AssumeUniversal)
                        .ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    antivirus_information_nissignaturelastupdated = antivirus_information_nissignaturelastupdated_temp;
                }

                //Logging.Handler.Debug("/devices -> Get_Antivirus_Information", "antispywareenabled", antivirus_information_antispywareenabled);


                //computable to human
                if (antivirus_information_amserviceenabled)
                    antivirus_information_amserviceenabled_display = Localizer["enabled"];
                else
                    antivirus_information_amserviceenabled_display = Localizer["disabled"];

                if (antivirus_information_antispywareenabled)
                    antivirus_information_antispywareenabled_display = Localizer["enabled"];
                else
                    antivirus_information_antispywareenabled_display = Localizer["disabled"];

                if (antivirus_information_antivirusenabled)
                    antivirus_information_antivirusenabled_display = Localizer["enabled"];
                else
                    antivirus_information_antivirusenabled_display = Localizer["disabled"];

                if (antivirus_information_behaviormonitorenabled)
                    antivirus_information_behaviormonitorenabled_display = Localizer["enabled"];
                else
                    antivirus_information_behaviormonitorenabled_display = Localizer["disabled"];

                if (antivirus_information_ioavprotectionenabled)
                    antivirus_information_ioavprotectionenabled_display = Localizer["enabled"];
                else
                    antivirus_information_ioavprotectionenabled_display = Localizer["disabled"];

                if (antivirus_information_istamperprotected)
                    antivirus_information_istamperprotected_display = Localizer["enabled"];
                else
                    antivirus_information_istamperprotected_display = Localizer["disabled"];

                if (antivirus_information_istamperprotected)
                    antivirus_information_istamperprotected_display = Localizer["enabled"];
                else
                    antivirus_information_istamperprotected_display = Localizer["disabled"];

                if (antivirus_information_nisenabled)
                    antivirus_information_nisenabled_display = Localizer["enabled"];
                else
                    antivirus_information_nisenabled_display = Localizer["disabled"];

                if (antivirus_information_onaccessprotectionenabled)
                    antivirus_information_onaccessprotectionenabled_display = Localizer["enabled"];
                else
                    antivirus_information_onaccessprotectionenabled_display = Localizer["disabled"];

                if (antivirus_information_realtimetprotectionenabled)
                    antivirus_information_realtimetprotectionenabled_display = Localizer["enabled"];
                else
                    antivirus_information_realtimetprotectionenabled_display = Localizer["disabled"];
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Antivirus_Information", "result", ex.ToString());
            }
        }

        #endregion

        #region Remote

        public class Remote_Admin_Identity
        {
            public string token { get; set; }
        }

        public class Remote_Target_Device
        {
            public string device_id { get; set; }
            public string device_name { get; set; }
            public string location_guid { get; set; }
            public string tenant_guid { get; set; }
        }

        public class Remote_Command
        {
            public int type { get; set; }
            public bool wait_response { get; set; }
            public string command { get; set; }
        }

        public class Remote_Root_Object
        {
            public Remote_Admin_Identity admin_identity { get; set; }
            public Remote_Target_Device target_device { get; set; }
            public Remote_Command command { get; set; }
        }

        // Remote Server
        private HubConnection remote_server_client;
        private System.Threading.Timer remote_server_clientCheckTimer;
        private bool remote_server_client_setup = false;
        private string remote_admin_identity = String.Empty;

        public async Task Remote_Setup_SignalR()
        {
            try
            {
                if (remote_server_client_setup)
                    return;

                Remote_Admin_Identity identity = new Remote_Admin_Identity
                {
                    token = token
                };

                // Create the object that contains the device_identity object
                var jsonObject = new { admin_identity = identity };

                // Serialize the object to a JSON string
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });

                remote_admin_identity = json;

                remote_server_client = new HubConnectionBuilder()
                    .WithUrl(Configuration.Remote_Server.Connection_String + "/commandHub", options =>
                    {
                        options.Headers.Add("Admin-Identity", Uri.EscapeDataString(remote_admin_identity));
                    })
                    .Build();

                // Software - Service Action
                remote_server_client.On<string>("ReceiveClientResponseServiceAction", async (command) =>
                {
                    Logging.Handler.Debug("/dashboard -> Remote_Setup_SignalR", "ReceiveClientResponseServiceAction", command);

                    // Use InvokeAsync to reflect changes on UI immediately
                    await InvokeAsync(() =>
                    {
                        Remote_Result_Dialog(command + System.Environment.NewLine + System.Environment.NewLine + Localizer["device_info_may_be_outdated"].ToString());

                        StateHasChanged();
                    });
                });

                // Task Manager - Terminate Process
                remote_server_client.On<string>("ReceiveClientResponseTaskManagerAction", async (command) =>
                {
                    Logging.Handler.Debug("/dashboard -> Remote_Setup_SignalR", "ReceiveClientResponseTaskManagerAction", command);

                    // Use InvokeAsync to reflect changes on UI immediately
                    await InvokeAsync(() =>
                    {
                        Remote_Result_Dialog(command + System.Environment.NewLine + System.Environment.NewLine + Localizer["device_info_may_be_outdated"].ToString());

                        StateHasChanged();
                    });
                });

                // Start the connection
                await remote_server_client.StartAsync();

                remote_server_client_setup = true;

                Logging.Handler.Debug("/dashboard -> Remote_Setup_SignalR", "Connected to the remote server.", remote_server_client_setup.ToString());

                //this.Snackbar.Add(Localizer["connected_with_netlock_remote_server"].ToString(), Severity.Info);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/dashboard -> Remote_Setup_SignalR", "General error", ex.ToString());
            }
        }

        private bool remote_result_dialog_open = false;

        private async Task Remote_Result_Dialog(string _result)
        {
            if (remote_result_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
                BackgroundClass = "dialog-blurring",
            };

            remote_result_dialog_open = true;

            DialogParameters parameters = new DialogParameters();
            parameters.Add("result", _result);

            remote_result_dialog_open = false;

            await this.DialogService.Show<Pages.Devices.Dialogs.Remote_File_Browser.Result_Dialog>(string.Empty, parameters, options).Result;
        }


        #endregion

        #region Remote Shell

        private bool remote_shell_dialog_open = false;

        private async Task Remote_Shell_Dialog()
        {
            if (remote_shell_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraLarge,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("device_id", notes_device_id);
            parameters.Add("device_name", notes_device_name);
            parameters.Add("tenant_guid", tenant_guid);
            parameters.Add("location_guid", location_guid);

            remote_shell_dialog_open = true;

            var result = await DialogService.Show<Pages.Devices.Dialogs.Remote_Shell.Remote_Shell_Dialog>(string.Empty, parameters, options).Result;

            remote_shell_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/devices -> Remote_Shell_Dialog", "Result", result.Data.ToString());
        }

        #endregion

        #region Remote Shell History

        private List<Device_Information_Remote_Shell_History_Entity> device_information_remote_shell_history_mysql_data;

        public class Device_Information_Remote_Shell_History_Entity
        {
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string command { get; set; } = String.Empty;
            public string result { get; set; } = String.Empty;
        }

        private TableGroupDefinition<Device_Information_Remote_Shell_History_Entity> device_information_remote_shell_history_groupDefinition = new TableGroupDefinition<Device_Information_Remote_Shell_History_Entity>
        {
            GroupName = date,
            Indentation = false,
            Expandable = true,
            IsInitiallyExpanded = false,
            Selector = (e) => e.date// Hier sollte die Eigenschaft sein, nach der gruppiert werden soll
        };

        private string device_information_remote_shell_history_table_view_port = "70vh";
        private string device_information_remote_shell_history_table_sorted_column;
        private string device_information_remote_shell_history_table_search_string = "";
        private MudDateRangePicker device_information_remote_shell_history_table_picker;
        private DateRange device_information_remote_shell_history_table_dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(1));

        private async Task Device_Information_Remote_Shell_History_Table_Submit_Picker()
        {
            device_information_remote_shell_history_table_picker.CloseAsync();

            await Device_Information_Remote_Shell_History_Load();
        }

        private bool Device_Information_Remote_Shell_History_Table_Filter_Func(Device_Information_Remote_Shell_History_Entity row)
        {
            if (string.IsNullOrEmpty(device_information_remote_shell_history_table_search_string))
                return true;

            //Search logic for each column
            return row.date.Contains(device_information_remote_shell_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.author.Contains(device_information_remote_shell_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.command.Contains(device_information_remote_shell_history_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.result.Contains(device_information_remote_shell_history_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string device_information_remote_shell_history_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private async void Device_Information_Remote_Shell_History_RowClickHandler(Device_Information_Remote_Shell_History_Entity row)
        {
            device_information_remote_shell_history_selectedRowContent = row.date;

            await Remote_Shell_History_Details_Dialog(row.date, row.author, row.command, row.result);
        }

        private string Device_Information_Remote_Shell_History_GetRowClass(Device_Information_Remote_Shell_History_Entity row)
        {
            return row.date == device_information_remote_shell_history_selectedRowContent ? "selected-row" : "";
        }

        private async Task Device_Information_Remote_Shell_History_Load()
        {
            device_information_remote_shell_history_mysql_data = new List<Device_Information_Remote_Shell_History_Entity>();
            loading_overlay = true;

            string query = @"SELECT * FROM device_information_remote_shell_history WHERE device_id = @device_id AND date >= @start_date AND date <= @end_date ORDER BY date DESC;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                using (MySqlCommand command = new MySqlCommand(query, conn))
                {
                    // Add parameters and ensure they are not null
                    command.Parameters.AddWithValue("@device_id", notes_device_id);
                    command.Parameters.AddWithValue("@start_date", device_information_remote_shell_history_table_dateRange.Start.HasValue ? device_information_remote_shell_history_table_dateRange.Start.Value : DateTime.Now.AddDays(-7));
                    command.Parameters.AddWithValue("@end_date", device_information_remote_shell_history_table_dateRange.Start.HasValue ? device_information_remote_shell_history_table_dateRange.End.Value : DateTime.MaxValue);

                    Logging.Handler.Debug("/devices -> Device_Information_Remote_Shell_History_Load", "MySQL_Query", query);

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                Logging.Handler.Debug("/devices -> Device_Information_Remote_Shell_History_Load", "MySQL_Result", reader["result"].ToString());

                                try
                                {
                                    Device_Information_Remote_Shell_History_Entity softwareEntity = new Device_Information_Remote_Shell_History_Entity
                                    {
                                        date = reader["date"].ToString(),
                                        author = reader["author"].ToString(),
                                        command = await Base64.Handler.Decode(reader["command"].ToString()),
                                        result = reader["result"].ToString(),
                                    };

                                    device_information_remote_shell_history_mysql_data.Add(softwareEntity);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Handler.Error("/devices -> Device_Information_Remote_Shell_History_Load", "MySQL_Query (corrupt json entry)", ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            catch (FormatException fe)
            {
                Logging.Handler.Error("/devices -> Device_Information_Remote_Shell_History_Load", "Format Error", fe.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Device_Information_Remote_Shell_History_Load", "MySQL_Query", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
                StateHasChanged();
                loading_overlay = false;
            }
        }

        private bool remote_shell_history_dialog_open = false;

        private async Task Remote_Shell_History_Details_Dialog(string date, string author, string command, string _result)
        {
            if (remote_shell_history_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraLarge,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("date", date);
            parameters.Add("author", author);
            parameters.Add("command", command);
            parameters.Add("result", _result);

            remote_shell_history_dialog_open = true;

            var result = await this.DialogService.Show<Pages.Devices.Dialogs.Remote_Shell.Remote_Shell_History_Details_Dialog>(string.Empty, parameters, options).Result;

            remote_shell_history_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/dashboard -> Remote_Shell_History_Details_Dialog", "Result", result.Data.ToString());
        }

        #endregion

        #region Remote File Browser

        private bool remote_file_browser_dialog_open = false;

        private async Task Remote_File_Browser_Dialog()
        {
            if (remote_file_browser_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraLarge,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("device_id", notes_device_id);
            parameters.Add("device_name", notes_device_name);
            parameters.Add("tenant_guid", tenant_guid);
            parameters.Add("location_guid", location_guid);
            parameters.Add("platform", platform);

            remote_file_browser_dialog_open = true;

            var result = await DialogService.Show<Pages.Devices.Dialogs.Remote_File_Browser.File_Browser_Dialog>(string.Empty, parameters, options).Result;

            remote_file_browser_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/devices -> Remote_Shell_Dialog", "Result", result.Data.ToString());
        }

        #endregion

        #region Remote Services

        private async Task Remote_Service_Action(string action, string name)
        {
            try
            {
                if (!remote_server_client_setup)
                    await Remote_Setup_SignalR();

                // Build the command json (action, name)
                var commandObject = new
                {
                    action = action,
                    name = name
                };

                // Serialize the command object to JSON
                string jsonCommand = JsonSerializer.Serialize(commandObject);

                // Create the object
                var adminIdentity = new Remote_Admin_Identity
                {
                    token = token
                };

                var targetDevice = new Remote_Target_Device
                {
                    device_id = notes_device_id,
                    device_name = notes_device_name,
                    tenant_guid = tenant_guid,
                    location_guid = location_guid
                };

                var command = new Remote_Command
                {
                    type = 2, // Service Action
                    wait_response = true,
                    command = jsonCommand
                };

                var rootObject = new Remote_Root_Object
                {
                    admin_identity = adminIdentity,
                    target_device = targetDevice,
                    command = command
                };

                // Serialization of the object
                string json = JsonSerializer.Serialize(rootObject, new JsonSerializerOptions { WriteIndented = true });

                // Send the command to the remote server
                await remote_server_client.InvokeAsync("MessageReceivedFromWebconsole", json);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Remote_Uninstall_Application", "General error", ex.ToString());
            }

        }

        #endregion

        #region Task Manager

        private async Task Remote_Task_Manager_Action(string pid)
        {
            try
            {
                if (!remote_server_client_setup)
                    await Remote_Setup_SignalR();

                // Create the object
                var adminIdentity = new Remote_Admin_Identity
                {
                    token = token
                };

                var targetDevice = new Remote_Target_Device
                {
                    device_id = notes_device_id,
                    device_name = notes_device_name,
                    tenant_guid = tenant_guid,
                    location_guid = location_guid
                };

                var command = new Remote_Command
                {
                    type = 3, // Task Manager
                    wait_response = true,
                    command = pid
                };

                var rootObject = new Remote_Root_Object
                {
                    admin_identity = adminIdentity,
                    target_device = targetDevice,
                    command = command
                };

                // Serialization of the object
                string json = JsonSerializer.Serialize(rootObject, new JsonSerializerOptions { WriteIndented = true });

                // Send the command to the remote server
                await remote_server_client.InvokeAsync("MessageReceivedFromWebconsole", json);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Remote_Uninstall_Application", "General error", ex.ToString());
            }

        }

        #endregion

        #region Remote Control

        private bool remote_control_dialog_open = false;

        private async Task Remote_Control_Dialog()
        {
            if (remote_control_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = false,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                BackgroundClass = "dialog-blurring",
                BackdropClick = false,
                CloseOnEscapeKey = false,
                FullScreen = false,
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("device_id", notes_device_id);
            parameters.Add("device_name", notes_device_name);
            parameters.Add("tenant_guid", tenant_guid);
            parameters.Add("location_guid", location_guid);

            remote_control_dialog_open = true;

            await DialogService.Show<Pages.Devices.Dialogs.Remote_Control.Remote_Control_Dialog>(string.Empty, parameters, options).Result;

            remote_control_dialog_open = false;
        }

        #endregion


        #region Data_Export

        private bool show_export_table_dialog_open = false;

        private async Task Show_Export_Table_Dialog(string type)
        {
            if (show_export_table_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            show_export_table_dialog_open = true;

            var result = await this.DialogService.Show<Shared.Export_Data_Dialog>(string.Empty, new DialogParameters(), options).Result;

            show_export_table_dialog_open = false;

            if (result != null && result.Data != null)
            {
                if (result.Data.ToString() == "JSON")
                    await Export_Data_Json(type);
                else if (result.Data.ToString() == "HTML")
                    await Export_Data_HTML(type);
            }
        }

        private async Task Trigger_Export_Device_Table_Dialog()
        {
            await Show_Export_Table_Dialog("devices");
        }

        private async Task Trigger_Export_Device_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("devices_history");
        }

        private async Task Trigger_Export_Installed_Application_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_installed");
        }

        private async Task Trigger_Export_Cronjobs_Table_Dialog()
        {
            await Show_Export_Table_Dialog("cronjobs");
        }

        private async Task Trigger_Export_Cronjobs_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("cronjobs_history");
        }

        private async Task Trigger_Export_Application_Logon_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_logon");
        }

        private async Task Trigger_Export_Application_Logon_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_logon_history");
        }

        private async Task Trigger_Export_Application_Scheduled_Tasks_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_scheduled_tasks");
        }

        private async Task Trigger_Export_Application_Scheduled_Tasks_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_scheduled_tasks_history");
        }

        private async Task Trigger_Export_Applications_Services_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_services");
        }

        private async Task Trigger_Export_Application_Services_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_services_history");
        }

        private async Task Trigger_Export_Applications_Drivers_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_drivers");
        }

        private async Task Trigger_Export_Application_Drivers_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_drivers_history");
        }

        private async Task Trigger_Export_Task_Manager_Table_Dialog()
        {
            await Show_Export_Table_Dialog("task_manager");
        }

        private async Task Trigger_Export_Support_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("support_history");
        }

        private async Task Trigger_Export_Notes_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("note_history");
        }

        private async Task Trigger_Export_Disks_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("disk_history");
        }

        private async Task Trigger_Export_Installed_Application_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("applications_installed_history");
        }

        private async Task Trigger_Export_Remote_Shell_History_Table_Dialog()
        {
            await Show_Export_Table_Dialog("remote_shell_history");
        }

        private async Task Export_Data_Json(string type)
        {
            try
            {
                string jsonContent = String.Empty;

                // Erstellen eines JSON-Strings aus den MudTable-Einträgen
                if (type == "devices")
                    jsonContent = JsonSerializer.Serialize(mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_installed")
                    jsonContent = JsonSerializer.Serialize(software_installed_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_logon")
                    jsonContent = JsonSerializer.Serialize(applications_logon, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_scheduled_tasks")
                    jsonContent = JsonSerializer.Serialize(applications_scheduled_tasks, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_services")
                    jsonContent = JsonSerializer.Serialize(applications_services, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "task_manager")
                    jsonContent = JsonSerializer.Serialize(task_manager_string, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "support_history")
                    jsonContent = JsonSerializer.Serialize(support_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "devices_history")
                    jsonContent = JsonSerializer.Serialize(support_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "note_history")
                    jsonContent = JsonSerializer.Serialize(device_information_notes_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "disk_history")
                    jsonContent = JsonSerializer.Serialize(device_information_disks_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_installed_history")
                    jsonContent = JsonSerializer.Serialize(applications_installed_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_logon_history")
                    jsonContent = JsonSerializer.Serialize(applications_logon_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_scheduled_tasks_history")
                    jsonContent = JsonSerializer.Serialize(applications_scheduled_tasks_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_services_history")
                    jsonContent = JsonSerializer.Serialize(applications_services_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "applications_drivers_history")
                    jsonContent = JsonSerializer.Serialize(applications_drivers_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "cpu_history")
                    jsonContent = JsonSerializer.Serialize(device_information_cpu_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "ram_history")
                    jsonContent = JsonSerializer.Serialize(ram_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "network_adapters_history")
                    jsonContent = JsonSerializer.Serialize(device_information_network_adapters_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "disks_history")
                    jsonContent = JsonSerializer.Serialize(device_information_disks_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "task_manager_history")
                    jsonContent = JsonSerializer.Serialize(task_manager_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "events")
                    jsonContent = JsonSerializer.Serialize(events_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "antivirus_products_history")
                    jsonContent = JsonSerializer.Serialize(antivirus_products_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });
                else if (type == "remote_shell_history")
                    jsonContent = JsonSerializer.Serialize(device_information_remote_shell_history_mysql_data, new JsonSerializerOptions { WriteIndented = true });

                // Aufruf der JavaScript-Funktion für den Export als .txt
                await JSRuntime.InvokeVoidAsync("exportToTxt", $"{type}.json", jsonContent);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("System_Logs", "Export_Data_Json", ex.Message);
            }
        }

        public async Task Export_Data_HTML(string type)
        {
            try
            {
                StringBuilder htmlBuilder = new StringBuilder();

                if (type == "devices")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_installed")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in software_installed_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in software_installed_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_logon")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in application_logon_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in application_logon_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_scheduled_tasks")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_scheduled_tasks_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_scheduled_tasks_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_services")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_services_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_services_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "task_manager")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in task_manager_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in task_manager_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "support_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in support_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in support_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "devices_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_general_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_general_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "note_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_notes_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_notes_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "disk_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_disks_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_disks_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_installed_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_installed_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_installed_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_logon_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_logon_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_logon_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_scheduled_tasks_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_scheduled_tasks_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_scheduled_tasks_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_services_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_services_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_services_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "applications_drivers_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in applications_drivers_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in applications_drivers_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "cpu_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_cpu_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_cpu_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "ram_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in ram_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in ram_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "network_adapters_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_network_adapters_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_network_adapters_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "disks_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_disks_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in device_information_disks_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "task_manager_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in task_manager_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in task_manager_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "events")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in events_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in events_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "antivirus_products_history")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in antivirus_products_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in antivirus_products_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }
                else if (type == "remote_shell_history")
                {
                    // Build the table header based on the properties of the data class                htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in device_information_remote_shell_history_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Build the table entries based on the data
                    foreach (var entry in device_information_remote_shell_history_mysql_data)
                    {
                        htmlBuilder.Append("<tr>");
                        foreach (var property in entry.GetType().GetProperties())
                        {
                            htmlBuilder.Append($"<td>{property.GetValue(entry)}</td>");
                        }
                        htmlBuilder.Append("</tr>");
                    }
                }


                htmlBuilder.Append("</table>");

                string htmlContent = htmlBuilder.ToString();

                // Hier wird JavaScript-Interop verwendet, um den HTML-Inhalt herunterzuladen
                await JSRuntime.InvokeVoidAsync("exportToTxt", $"{type}.html", htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("System_Logs", "Export_Data_HTML", ex.Message);
            }
        }
        #endregion
    }
}