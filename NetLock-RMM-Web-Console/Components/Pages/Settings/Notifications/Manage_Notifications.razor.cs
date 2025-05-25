using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using MudBlazor;
using MySqlConnector;

namespace NetLock_RMM_Web_Console.Components.Pages.Settings.Notifications
{
    public partial class Manage_Notifications
    {            
        #region Permissions System

        private string permissions_json = String.Empty;

        private bool permissions_settings_enabled = false;
        private bool permissions_settings_notifications_enabled = false;

        private bool permissions_settings_notifications_mail_enabled = false;
        private bool permissions_settings_notifications_mail_add = false;
        private bool permissions_settings_notifications_mail_smtp = false;
        private bool permissions_settings_notifications_mail_test = false;
        private bool permissions_settings_notifications_mail_edit = false;
        private bool permissions_settings_notifications_mail_delete = false;

        private bool permissions_settings_notifications_microsoft_teams_enabled = false;
        private bool permissions_settings_notifications_microsoft_teams_add = false;
        private bool permissions_settings_notifications_microsoft_teams_test = false;
        private bool permissions_settings_notifications_microsoft_teams_edit = false;
        private bool permissions_settings_notifications_microsoft_teams_delete = false;

        private bool permissions_settings_notifications_telegram_enabled = false;
        private bool permissions_settings_notifications_telegram_add = false;
        private bool permissions_settings_notifications_telegram_test = false;
        private bool permissions_settings_notifications_telegram_edit = false;
        private bool permissions_settings_notifications_telegram_delete = false;

        private bool permissions_settings_notifications_ntfysh_enabled = false;
        private bool permissions_settings_notifications_ntfysh_add = false;
        private bool permissions_settings_notifications_ntfysh_test = false;
        private bool permissions_settings_notifications_ntfysh_edit = false;
        private bool permissions_settings_notifications_ntfysh_delete = false;

        private async Task<bool> Permissions()
        {
            try
            {
                bool logout = false;

                // Get the current user from the authentication state
                var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;

                // Check if user is authenticated
                if (user?.Identity is not { IsAuthenticated: true })
                    logout = true;

                string netlock_username = user.FindFirst(ClaimTypes.Email)?.Value;

                permissions_settings_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_enabled");
                permissions_settings_notifications_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_enabled");
                permissions_settings_notifications_mail_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_enabled");
                permissions_settings_notifications_mail_add = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_add");
                permissions_settings_notifications_mail_smtp = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_smtp");
                permissions_settings_notifications_mail_test = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_test");
                permissions_settings_notifications_mail_edit = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_edit");
                permissions_settings_notifications_mail_delete = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_mail_delete");
                permissions_settings_notifications_microsoft_teams_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_microsoft_teams_enabled");
                permissions_settings_notifications_microsoft_teams_add = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_microsoft_teams_add");
                permissions_settings_notifications_microsoft_teams_test = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_microsoft_teams_test");
                permissions_settings_notifications_microsoft_teams_edit = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_microsoft_teams_edit");
                permissions_settings_notifications_microsoft_teams_delete = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_microsoft_teams_delete");
                permissions_settings_notifications_telegram_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_telegram_enabled");
                permissions_settings_notifications_telegram_add = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_telegram_add");
                permissions_settings_notifications_telegram_test = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_telegram_test");
                permissions_settings_notifications_telegram_edit = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_telegram_edit");
                permissions_settings_notifications_telegram_delete = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_telegram_delete");
                permissions_settings_notifications_ntfysh_enabled = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_ntfysh_enabled");
                permissions_settings_notifications_ntfysh_add = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_ntfysh_add");
                permissions_settings_notifications_ntfysh_test = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_ntfysh_test");
                permissions_settings_notifications_ntfysh_edit = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_ntfysh_edit");
                permissions_settings_notifications_ntfysh_delete = await Classes.Authentication.Permissions.Verify_Permission(netlock_username, "settings_notifications_ntfysh_delete");

                if (!permissions_settings_enabled || !permissions_settings_notifications_enabled)
                    logout = true;

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

        private bool _isDarkMode;

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
            if (!await Permissions())
                return;

            _isDarkMode = await JSRuntime.InvokeAsync<bool>("isDarkMode");

            settings_notifications_panel_index = Convert.ToInt32(await localStorage.GetItemAsync<string>("settings_notifications_panel_index"));
            mail_notifications_mysql_data = await Get_Mail_Notifications_Overview();
            microsoft_teams_notifications_mysql_data = await Get_Microsoft_Teams_Notifications_Overview();
            telegram_notifications_mysql_data = await Get_Telegram_Notifications_Overview();
            ntfy_sh_notifications_mysql_data = await Get_Ntfy_sh_Notifications_Overview();

            StateHasChanged();
        }

        int settings_notifications_panel_index = 0;

        private async Task Save_Panel_Index()
        {
            await localStorage.SetItemAsync<string>("settings_notifications_panel_index ", settings_notifications_panel_index.ToString());
        }

        #region Mail

        public List<Mail_Notifications_Entity> mail_notifications_mysql_data;

        public class Mail_Notifications_Entity
        {
            public string id { get; set; } = String.Empty;
            public string mail_address { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string severity { get; set; } = String.Empty;
            public string tenants { get; set; } = String.Empty;
            public string tenants_json { get; set; } = String.Empty;
            public bool uptime_monitoring_enabled { get; set; } = false;
        }

        private string mail_notifications_table_view_port = "70vh";
        private string mail_notifications_table_sorted_column;
        private string mail_notifications_table_search_string = "";

        private bool Mail_Notifications_Table_Filter_Func(Mail_Notifications_Entity row)
        {
            if (string.IsNullOrEmpty(mail_notifications_table_search_string))
                return true;

            //Search logic for each column
            return row.mail_address.Contains(mail_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.date.Contains(mail_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.author.Contains(mail_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.tenants.Contains(mail_notifications_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string mail_notifications_selectedRowContent = String.Empty;
        private string mail_notifications_selectedRowContent_mail = String.Empty;

        private void Mail_Notifications_RowClickHandler(Mail_Notifications_Entity row)
        {
            mail_notifications_selectedRowContent = row.id;
        }

        private async void Mail_Notifications_RowDblClickHandler(Mail_Notifications_Entity row)
        {
            await Edit_Mail_Notification_Dialog(row.id, row.mail_address, row.description, row.severity, row.tenants_json, row.uptime_monitoring_enabled);
        }

        private string Mail_Notifications_GetRowClass(Mail_Notifications_Entity row)
        {
            return row.id == mail_notifications_selectedRowContent ? "selected-row" : "";
        }

        private bool add_mail_notification_dialog_open = false;

        private async Task Add_Mail_Notification_Dialog()
        {
            if (add_mail_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            //parameters.Add("parameter", parameter);

            add_mail_notification_dialog_open = true;

            var result = await this.DialogService.Show<Settings.Notifications.E_Mail.Add_Mail_Notification_Dialog>(string.Empty, parameters, options).Result;

            add_mail_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Mail_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                mail_notifications_mysql_data = await Get_Mail_Notifications_Overview();
        }

        private bool edit_mail_notification_dialog_open = false;

        private async Task Edit_Mail_Notification_Dialog(string id, string mail_address, string description, string severity, string tenants_json, bool uptime_monitoring_enabled)
        {
            if (edit_mail_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };
            
            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);
            parameters.Add("mail_address", mail_address);
            parameters.Add("description", description);
            parameters.Add("severity", severity);
            parameters.Add("tenants_json", tenants_json);
            parameters.Add("uptime_monitoring_enabled", uptime_monitoring_enabled);

            edit_mail_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.E_Mail.Edit_Mail_Notification_Dialog>(string.Empty, parameters, options).Result;

            edit_mail_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Mail_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                mail_notifications_mysql_data = await Get_Mail_Notifications_Overview();
        }

        private bool delete_mail_notification_dialog_open = false;

        private async Task Delete_Mail_Notification_Dialog(string id)
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);

            delete_mail_notification_dialog_open = true;

            var result = await this.DialogService.Show<Settings.Notifications.E_Mail.Delete_Mail_Notification_Dialog>(string.Empty, parameters, options).Result;

            delete_mail_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Mail_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                mail_notifications_mysql_data = await Get_Mail_Notifications_Overview();
        }

        private bool smtp_settings_dialog_open = false;

        private async Task SMTP_Settings_Dialog()
        {
            if (smtp_settings_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            //parameters.Add("parameter", parameter);

            smtp_settings_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.E_Mail.Smtp_Settings_Dialog>(string.Empty, parameters, options).Result;

            smtp_settings_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Mail_Notification_Dialog", "Result", result.Data.ToString());
        }

        private async Task<List<Mail_Notifications_Entity>> Get_Mail_Notifications_Overview()
        {
            Logging.Handler.Debug("/Manage_Notifications -> Get_Mail_Notifications_Overview", "Start", "true");

            List<Mail_Notifications_Entity> result = new List<Mail_Notifications_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM mail_notifications;", conn);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string tenants_string = "";

                            try
                            {
                                var tenants_list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(reader["tenants"].ToString());

                                foreach (var tenant in tenants_list)
                                    if (tenant.ContainsKey("id"))
                                    {
                                        tenants_string = tenants_string + await Classes.MySQL.Handler.Get_Tenant_Name_By_Id(Convert.ToInt32(tenant["id"])) + ", ";
                                    }

                                tenants_string = tenants_string.Remove(tenants_string.Length - 2);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("Manage_Notifications -> Get_Mail_Notifications_Overview", "Extract tenants to table", ex.ToString());
                            }

                            Mail_Notifications_Entity entity = new Mail_Notifications_Entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                mail_address = reader["mail_address"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                author = reader["author"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                severity = reader["severity"].ToString() ?? String.Empty,
                                tenants = tenants_string,
                                tenants_json = reader["tenants"].ToString() ?? String.Empty,
                                uptime_monitoring_enabled = Convert.ToBoolean(reader["uptime_monitoring_enabled"]),
                            };

                            result.Add(entity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Get_Mail_Notifications_Overview", ex.Message);
            }
            finally
            {
                conn.Close();
                StateHasChanged();
            }

            return result;
        }

        private async Task Send_Mail(string mail_address)
        {
            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            string result = await Classes.Helper.Notifications.Smtp.Send_Mail(mail_address, "NetLock RMM - Test Alert", "Test.");

            if (result == "success")
            {
                this.Snackbar.Add(Localizer["successfully_sent"], Severity.Success);
            }
            else
            {
                this.Snackbar.Add(Localizer["failed_sending"] + result, Severity.Error);
            }
        }

        #endregion

        #region MS_Teams

        public List<Microsoft_Teams_Notifications_Entity> microsoft_teams_notifications_mysql_data;

        public class Microsoft_Teams_Notifications_Entity
        {
            public string id { get; set; } = String.Empty;
            public string connector_name { get; set; } = String.Empty;
            public string connector_url { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string severity { get; set; } = String.Empty;
            public string tenants { get; set; } = String.Empty;
            public string tenants_json { get; set; } = String.Empty;
            public bool uptime_monitoring_enabled { get; set; } = false;
        }

        private string microsoft_teams_notifications_table_view_port = "70vh";
        private string microsoft_teams_notifications_table_sorted_column;
        private string microsoft_teams_notifications_table_search_string = "";

        private bool Microsoft_Teams_Notifications_Table_Filter_Func(Microsoft_Teams_Notifications_Entity row)
        {
            if (string.IsNullOrEmpty(microsoft_teams_notifications_table_search_string))
                return true;

            //Search logic for each column
            return row.connector_name.Contains(microsoft_teams_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.date.Contains(microsoft_teams_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.author.Contains(microsoft_teams_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.tenants.Contains(microsoft_teams_notifications_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string microsoft_teams_notifications_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Microsoft_Teams_Notifications_RowClickHandler(Microsoft_Teams_Notifications_Entity row)
        {
            microsoft_teams_notifications_selectedRowContent = row.connector_name;
        }

        private async void Microsoft_Teams_Notifications_RowDblClickHandler(Microsoft_Teams_Notifications_Entity row)
        {
            await Edit_Microsoft_Teams_Notification_Dialog(row.id, row.connector_name, row.connector_url, row.description, row.severity, row.tenants_json, row.uptime_monitoring_enabled);
        }

        private string Microsoft_Teams_Notifications_GetRowClass(Microsoft_Teams_Notifications_Entity row)
        {
            return row.connector_name == microsoft_teams_notifications_selectedRowContent ? "selected-row" : "";
        }

        private bool add_microsoft_teams_notification_dialog_open = false;

        private async Task Add_Microsoft_Teams_Notification_Dialog()
        {
            if (add_microsoft_teams_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            //parameters.Add("parameter", parameter);

            add_microsoft_teams_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Microsoft_Teams.Add_Microsoft_Teams_Notification_Dialog>(string.Empty, parameters, options).Result;

            add_microsoft_teams_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Microsoft_Teams_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                microsoft_teams_notifications_mysql_data = await Get_Microsoft_Teams_Notifications_Overview();
        }

        private bool edit_microsoft_teams_notification_dialog_open = false;

        private async Task Edit_Microsoft_Teams_Notification_Dialog(string id, string connector_name, string connector_url, string description, string severity, string tenants_json, bool uptime_monitoring_enabled)
        {
            if (edit_microsoft_teams_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);
            parameters.Add("connector_name", connector_name);
            parameters.Add("connector_url", connector_url);
            parameters.Add("description", description);
            parameters.Add("severity", severity);
            parameters.Add("tenants_json", tenants_json);
            parameters.Add("uptime_monitoring_enabled", uptime_monitoring_enabled);

            edit_microsoft_teams_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Microsoft_Teams.Edit_Microsoft_Teams_Notification_Dialog>(string.Empty, parameters, options).Result;

            edit_microsoft_teams_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Edit_Microsoft_Teams_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                microsoft_teams_notifications_mysql_data = await Get_Microsoft_Teams_Notifications_Overview();
        }

        private bool delete_microsoft_teams_notification_dialog = false;

        private async Task Delete_Microsoft_Teams_Notification_Dialog(string id)
        {
            if (delete_microsoft_teams_notification_dialog)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);

            delete_microsoft_teams_notification_dialog = true;

            var result = await this.DialogService.Show<Settings.Notifications.Microsoft_Teams.Delete_Microsoft_Teams_Notification_Dialog>(string.Empty, parameters, options).Result;

            delete_microsoft_teams_notification_dialog = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Delete_Microsoft_Teams_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                microsoft_teams_notifications_mysql_data = await Get_Microsoft_Teams_Notifications_Overview();
        }

        private async Task<List<Microsoft_Teams_Notifications_Entity>> Get_Microsoft_Teams_Notifications_Overview()
        {
            List<Microsoft_Teams_Notifications_Entity> result = new List<Microsoft_Teams_Notifications_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM microsoft_teams_notifications;", conn);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string tenants_string = "";

                            try
                            {
                                var tenants_list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(reader["tenants"].ToString());

                                foreach (var tenant in tenants_list)
                                    if (tenant.ContainsKey("id"))
                                    {
                                        tenants_string = tenants_string + await Classes.MySQL.Handler.Get_Tenant_Name_By_Id(Convert.ToInt32(tenant["id"])) + ", ";
                                    }

                                tenants_string = tenants_string.Remove(tenants_string.Length - 2);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/Manage_Notifications -> Get_Microsoft_Teams_Notifications_Overview", "Extract tenants to table", ex.ToString());
                            }

                            Microsoft_Teams_Notifications_Entity entity = new Microsoft_Teams_Notifications_Entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                connector_name = reader["connector_name"].ToString() ?? String.Empty,
                                connector_url = reader["connector_url"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                author = reader["author"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                severity = reader["severity"].ToString() ?? String.Empty,
                                tenants = tenants_string,
                                tenants_json = reader["tenants"].ToString() ?? String.Empty,
                                uptime_monitoring_enabled = Convert.ToBoolean(reader["uptime_monitoring_enabled"]),
                            };

                            result.Add(entity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Get_Mail_Notifications_Overview", ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return result;
        }

        private async Task Send_Microsoft_Teams_Message(string id, string connector_name)
        {
            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            string result = await Classes.Helper.Notifications.Microsoft_Teams.Send_Message(id, "NetLock RMM Alert: Test.<br>Connector Name: " + connector_name);

            if (result == "success")
            {
                this.Snackbar.Add(Localizer["successfully_sent"], Severity.Success);
            }
            else
            {
                this.Snackbar.Add(Localizer["failed_sending"] + result, Severity.Error);
            }
        }

        #endregion

        #region Telegram

        public List<Telegram_Notifications_Entity> telegram_notifications_mysql_data;

        public class Telegram_Notifications_Entity
        {
            public string id { get; set; } = String.Empty;
            public string bot_name { get; set; } = String.Empty;
            public string bot_token { get; set; } = String.Empty;
            public string chat_id { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string severity { get; set; } = String.Empty;
            public string tenants { get; set; } = String.Empty;
            public string tenants_json { get; set; } = String.Empty;
            public bool uptime_monitoring_enabled { get; set; } = false;
        }

        private string telegram_notifications_table_view_port = "70vh";
        private string telegram_notifications_table_sorted_column;
        private string telegram_notifications_table_search_string = "";

        private bool Telegram_Notifications_Table_Filter_Func(Telegram_Notifications_Entity row)
        {
            if (string.IsNullOrEmpty(telegram_notifications_table_search_string))
                return true;

            //Search logic for each column
            return row.bot_name.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.bot_token.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.chat_id.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.date.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.author.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.tenants.Contains(telegram_notifications_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string telegram_notifications_selectedRowContent = String.Empty;

        private void Telegram_Notifications_RowClickHandler(Telegram_Notifications_Entity row)
        {
            telegram_notifications_selectedRowContent = row.bot_name;
        }

        private async void Telegram_Notifications_RowDblClickHandler(Telegram_Notifications_Entity row)
        {
            await Edit_Telegram_Notification_Dialog(row.id, row.bot_name, row.bot_token, row.chat_id, row.description, row.severity, row.tenants_json, row.uptime_monitoring_enabled);
        }

        private string Telegram_Notifications_GetRowClass(Telegram_Notifications_Entity row)
        {
            return row.bot_name == telegram_notifications_selectedRowContent ? "selected-row" : "";
        }

        private bool add_telegram_notification_dialog_open = false;

        private async Task Add_Telegram_Notification_Dialog()
        {
            if (add_telegram_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            //parameters.Add("parameter", parameter);

            add_telegram_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Telegram.Add_Telegram_Notification_Dialog>(string.Empty, parameters, options).Result;

            add_telegram_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Telegram_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                telegram_notifications_mysql_data = await Get_Telegram_Notifications_Overview();
        }

        private bool edit_telegram_notification_dialog_open = false;

        private async Task Edit_Telegram_Notification_Dialog(string id, string bot_name, string bot_token, string chat_id, string description, string severity, string tenants_json, bool uptime_monitoring_enabled)
        {
            if (edit_telegram_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);
            parameters.Add("bot_name", bot_name);
            parameters.Add("bot_token", bot_token);
            parameters.Add("chat_id", chat_id);
            parameters.Add("description", description);
            parameters.Add("severity", severity);
            parameters.Add("tenants_json", tenants_json);
            parameters.Add("uptime_monitoring_enabled", uptime_monitoring_enabled);

            edit_telegram_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Telegram.Edit_Telegram_Notification_Dialog>(string.Empty, parameters, options).Result;

            edit_telegram_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Edit_Telegram_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                telegram_notifications_mysql_data = await Get_Telegram_Notifications_Overview();
        }

        private bool delete_telegram_notification_dialog_open = false;

        private async Task Delete_Telegram_Notification_Dialog(string id)
        {
            if (delete_telegram_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);

            delete_telegram_notification_dialog_open = true;

            var result = await this.DialogService.Show<Settings.Notifications.Telegram.Delete_Telegram_Notification_Dialog>(string.Empty, parameters, options).Result;

            delete_telegram_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Delete_Telegram_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                telegram_notifications_mysql_data = await Get_Telegram_Notifications_Overview();
        }

        private async Task<List<Telegram_Notifications_Entity>> Get_Telegram_Notifications_Overview()
        {
            List<Telegram_Notifications_Entity> result = new List<Telegram_Notifications_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM telegram_notifications;", conn);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string tenants_string = "";

                            try
                            {
                                var tenants_list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(reader["tenants"].ToString());

                                foreach (var tenant in tenants_list)
                                    if (tenant.ContainsKey("id"))
                                    {
                                        tenants_string = tenants_string + await Classes.MySQL.Handler.Get_Tenant_Name_By_Id(Convert.ToInt32(tenant["id"])) + ", ";
                                    }

                                tenants_string = tenants_string.Remove(tenants_string.Length - 2);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/Manage_Notifications -> Get_Telegram_Notifications_Overview", "Extract tenants to table", ex.ToString());
                            }

                            Telegram_Notifications_Entity entity = new Telegram_Notifications_Entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                bot_name = reader["bot_name"].ToString() ?? String.Empty,
                                bot_token = reader["bot_token"].ToString() ?? String.Empty,
                                chat_id = reader["chat_id"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                author = reader["author"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                severity = reader["severity"].ToString() ?? "",
                                tenants = tenants_string,
                                tenants_json = reader["tenants"].ToString() ?? "",
                                uptime_monitoring_enabled = Convert.ToBoolean(reader["uptime_monitoring_enabled"]),
                            };

                            result.Add(entity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Get_Telegram_Notifications_Overview", ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return result;
        }

        private async Task Send_Telegram_Message(string id, string bot_name)
        {
            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            string result = await Classes.Helper.Notifications.Telegram.Send_Message(id, "NetLock RMM Alert: Test.\nBot Name: " + bot_name);

            if (result == "success")
            {
                this.Snackbar.Add(Localizer["successfully_sent"], Severity.Success);
            }
            else
            {
                this.Snackbar.Add(Localizer["failed_sending"] + result, Severity.Error);
            }
        }

        #endregion

        #region Ntfysh

        public List<Ntfy_sh_Notifications_Entity> ntfy_sh_notifications_mysql_data;

        public class Ntfy_sh_Notifications_Entity
        {
            public string id { get; set; } = String.Empty;
            public string topic_name { get; set; } = String.Empty;
            public string topic_url { get; set; } = String.Empty;
            public string access_token { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string severity { get; set; } = String.Empty;
            public string tenants { get; set; } = String.Empty;
            public string tenants_json { get; set; } = String.Empty;
            public bool uptime_monitoring_enabled { get; set; } = false;
        }

        private string ntfy_sh_notifications_table_view_port = "70vh";
        private string ntfy_sh_notifications_table_sorted_column;
        private string ntfy_sh_notifications_table_search_string = "";

        private bool Ntfy_sh_Notifications_Table_Filter_Func(Ntfy_sh_Notifications_Entity row)
        {
            if (string.IsNullOrEmpty(ntfy_sh_notifications_table_search_string))
                return true;

            //Search logic for each column
            return row.topic_name.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.topic_url.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.access_token.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.date.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.author.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                   row.tenants.Contains(ntfy_sh_notifications_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string ntfy_sh_notifications_selectedRowContent = ""; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Ntfy_sh_Notifications_RowClickHandler(Ntfy_sh_Notifications_Entity row)
        {
            ntfy_sh_notifications_selectedRowContent = row.topic_name;
        }

        private async void Ntfy_sh_Notifications_RowDblClickHandler(Ntfy_sh_Notifications_Entity row)
        {
            await Edit_Ntfy_sh_Notification_Dialog(row.id, row.topic_name, row.topic_url, row.access_token, row.description, row.severity, row.tenants_json, row.uptime_monitoring_enabled);
        }

        private string Ntfy_sh_Notifications_GetRowClass(Ntfy_sh_Notifications_Entity row)
        {
            return row.topic_name == ntfy_sh_notifications_selectedRowContent ? "selected-row" : "";
        }

        private bool add_ntfy_sh_notification_dialog_open = false;

        private async Task Add_Ntfy_sh_Notification_Dialog()
        {
            if (add_ntfy_sh_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            //parameters.Add("parameter", parameter);

            add_ntfy_sh_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Ntfy_sh.Add_Ntfy_sh_Notification_Dialog>(string.Empty, parameters, options).Result;

            add_ntfy_sh_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Add_Ntfy_sh_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                ntfy_sh_notifications_mysql_data = await Get_Ntfy_sh_Notifications_Overview();
        }

        private bool edit_ntfy_sh_notification_dialog_open = false;

        private async Task Edit_Ntfy_sh_Notification_Dialog(string id, string topic_name, string topic_url, string access_token, string description, string severity, string tenants_json, bool uptime_monitoring_enabled)
        {
            if (edit_ntfy_sh_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);
            parameters.Add("topic_name", topic_name);
            parameters.Add("topic_url", topic_url);
            parameters.Add("access_token", access_token);
            parameters.Add("description", description);
            parameters.Add("severity", severity);
            parameters.Add("tenants_json", tenants_json);
            parameters.Add("uptime_monitoring_enabled", uptime_monitoring_enabled);

            edit_ntfy_sh_notification_dialog_open = true;

            var result = await DialogService.Show<Settings.Notifications.Ntfy_sh.Edit_Ntfy_sh_Notification_Dialog>(string.Empty, parameters, options).Result;

            edit_ntfy_sh_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Edit_Ntfy_sh_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                ntfy_sh_notifications_mysql_data = await Get_Ntfy_sh_Notifications_Overview();
        }

        private bool delete_ntfy_sh_notification_dialog_open = false;

        private async Task Delete_Ntfy_sh_Notification_Dialog(string id)
        {
            if (delete_ntfy_sh_notification_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("id", id);

            delete_ntfy_sh_notification_dialog_open = true;

            var result = await this.DialogService.Show<Settings.Notifications.Ntfy_sh.Delete_Ntfy_sh_Notification_Dialog>(string.Empty, parameters, options).Result;

            delete_ntfy_sh_notification_dialog_open = false;

            if (result.Canceled)
                return;
            else if (result.Data == null || result.Data == "error")
                return;

            Logging.Handler.Debug("/Manage_Notifications -> Delete_Ntfy_sh_Notification_Dialog", "Result", result.Data.ToString());

            if (result.Data == "success")
                ntfy_sh_notifications_mysql_data = await Get_Ntfy_sh_Notifications_Overview();
        }

        private async Task<List<Ntfy_sh_Notifications_Entity>> Get_Ntfy_sh_Notifications_Overview()
        {
            List<Ntfy_sh_Notifications_Entity> result = new List<Ntfy_sh_Notifications_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM ntfy_sh_notifications;", conn);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string tenants_string = "";

                            try
                            {
                                var tenants_list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(reader["tenants"].ToString());

                                foreach (var tenant in tenants_list)
                                    if (tenant.ContainsKey("id"))
                                    {
                                        tenants_string = tenants_string + await Classes.MySQL.Handler.Get_Tenant_Name_By_Id(Convert.ToInt32(tenant["id"])) + ", ";
                                    }

                                tenants_string = tenants_string.Remove(tenants_string.Length - 2);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/Manage_Notifications -> Get_Ntfy_sh_Notifications_Overview", "Extract tenants to table", ex.ToString());
                            }

                            Ntfy_sh_Notifications_Entity entity = new Ntfy_sh_Notifications_Entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                topic_name = reader["topic_name"].ToString() ?? String.Empty,
                                topic_url = reader["topic_url"].ToString() ?? String.Empty,
                                access_token = reader["access_token"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                author = reader["author"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                severity = reader["severity"].ToString() ?? String.Empty,
                                tenants = tenants_string,
                                tenants_json = reader["tenants"].ToString() ?? String.Empty,
                                uptime_monitoring_enabled = Convert.ToBoolean(reader["uptime_monitoring_enabled"]),
                            };

                            result.Add(entity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Get_Ntfy_sh_Notifications_Overview", ex.ToString());
            }
            finally
            {
                conn.Close();
            }

            return result;
        }

        private async Task Send_Ntfy_sh_Message(string id, string topic_name)
        {
            this.Snackbar.Configuration.ShowCloseIcon = true;
            this.Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

            string result = await Classes.Helper.Notifications.Ntfy_sh.Send_Message(id, "NetLock RMM Alert: Test.\nTopic Name: " + topic_name);

            if (result == "success")
            {
                this.Snackbar.Add(Localizer["successfully_sent"], Severity.Success);
            }
            else
            {
                this.Snackbar.Add(Localizer["failed_sending"] + result, Severity.Error);
            }
        }
        #endregion
    }
}