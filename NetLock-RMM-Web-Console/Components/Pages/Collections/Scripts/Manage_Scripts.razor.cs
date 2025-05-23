using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using MudBlazor;
using MySqlConnector;
using OfficeOpenXml;

namespace NetLock_RMM_Web_Console.Components.Pages.Collections.Scripts
{
    public partial class Manage_Scripts
    {

        #region Permissions System

        private string permissions_json = String.Empty;
        private string permissions_tenants_json = String.Empty;
        public static List<string> permissions_tenants_list = new List<string> { };

        private bool permissions_collections_enabled = false;
        private bool permissions_collections_scripts_enabled = false;
        private bool permissions_collections_scripts_add = false;
        private bool permissions_collections_scripts_edit = false;
        private bool permissions_collections_scripts_delete = false;

        public class Permissions_Tenants_Activation_State
        {
            public string id { get; set; } = String.Empty;
        }

        private async Task Get_Permissions()
        {
            //Extract user info from users session storage
            var sessionStorage = new ProtectedSessionStorage(JSRuntime, DataProtectionProvider);
            var username = await sessionStorage.GetAsync<string>("username");
            var password = await sessionStorage.GetAsync<string>("password");

            Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "username", username.Value ?? String.Empty);

            //if user info empty, force logout
            if (String.IsNullOrEmpty(username.Value) || String.IsNullOrEmpty(password.Value))
            {
                Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "sessions storage data", "empty, force logout");

                NavigationManager.NavigateTo("/logout", true);
                return;
            }

            //Check if user info is valid, if not, force logout
            if (!await Classes.Authentication.User.Verify_User(username.Value ?? String.Empty, password.Value ?? String.Empty))
            {
                Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "verify user", "incorrect data, force logout");

                NavigationManager.NavigateTo("/logout", true);
                return;
            }

            //Get permissions
            string query = "SELECT * FROM `accounts` WHERE username = @username;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@username", username.Value);

                Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions_json = reader["permissions"].ToString() ?? String.Empty;
                            permissions_tenants_json = reader["tenants"].ToString() ?? String.Empty;
                        }
                    }
                }

                Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "permissions_json", permissions_json);

                //Extract permissions
                if (!String.IsNullOrEmpty(permissions_json))
                {
                    using (JsonDocument document = JsonDocument.Parse(permissions_json))
                    {
                        //collections_enabled
                        try
                        {
                            JsonElement element = document.RootElement.GetProperty("collections_enabled");
                            permissions_collections_enabled = element.GetBoolean();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/manage_scripts -> Permissions_Load", "permissions_json (collections_enabled)", ex.Message);
                        }

                        //collections_scripts_enabled
                        try
                        {
                            JsonElement element = document.RootElement.GetProperty("collections_scripts_enabled");
                            permissions_collections_scripts_enabled = element.GetBoolean();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/manage_scripts -> Permissions_Load", "permissions_json (collections_scripts_enabled)", ex.Message);
                        }

                        //collections_scripts_add
                        try
                        {
                            JsonElement element = document.RootElement.GetProperty("collections_scripts_add");
                            permissions_collections_scripts_add = element.GetBoolean();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/manage_scripts -> Permissions_Load", "permissions_json (collections_scripts_add)", ex.Message);
                        }

                        //collections_scripts_edit
                        try
                        {
                            JsonElement element = document.RootElement.GetProperty("collections_scripts_edit");
                            permissions_collections_scripts_edit = element.GetBoolean();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/manage_scripts -> Permissions_Load", "permissions_json (collections_scripts_edit)", ex.Message);
                        }

                        //collections_scripts_delete
                        try
                        {
                            JsonElement element = document.RootElement.GetProperty("collections_scripts_delete");
                            permissions_collections_scripts_delete = element.GetBoolean();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/manage_scripts -> Permissions_Load", "permissions_json (collections_scripts_delete)", ex.Message);
                        }
                    }
                }
                else if (permissions_json == "[]")
                {
                    Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "permissions_json", "Empty, logout user");
                    NavigationManager.NavigateTo("/logout", true);
                }
                else
                {
                    Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "permissions_json", "Empty, logout user");
                    NavigationManager.NavigateTo("/logout", true);
                }

                //Extract tenants from json
                permissions_tenants_list.Clear();
                if (!String.IsNullOrEmpty(permissions_tenants_json))
                {
                    //Set the activation state for the tenants
                    try
                    {
                        List<Permissions_Tenants_Activation_State> tenants_activation_state_list = JsonSerializer.Deserialize<List<Permissions_Tenants_Activation_State>>(permissions_tenants_json);

                        foreach (var tenant in tenants_activation_state_list)
                        {
                            Logging.Handler.Debug("/manage_scripts -> Permissions_Load", "foreach tenant", tenant.id);

                            permissions_tenants_list.Add(tenant.id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("/manage_scripts -> Permissions_Load (permissions_tenants_json deserialize)", "Result", ex.Message);
                    }
                }
                else
                {
                    Logging.Handler.Debug("/manage_scripts -> Permissions_Load (permissions_tenants_json deserialize)", "Result", "Empty");
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_scripts -> Permissions_Load", "general_error (force logout)", ex.Message);
                NavigationManager.NavigateTo("/logout", true);
            }
            finally
            {
                conn.Close();
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
            // Get the current user from the authentication state
            var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;

            // Check if user is authenticated
            if (user?.Identity is not { IsAuthenticated: true })
            {
                NavigationManager.NavigateTo("/login", true);
                return;
            }

            await Get_Permissions();
            //Check permissions
            if (!permissions_collections_enabled || !permissions_collections_scripts_enabled)
            {
                NavigationManager.NavigateTo("/logout", true);
                return;
            }

            _isDarkMode = await JSRuntime.InvokeAsync<bool>("isDarkMode");

            await Scripts_Load();

            StateHasChanged();
        }

        private async Task Scripts_Load()
        {
            scripts_mysql_data = new List<Scripts_Entity>();

            string query = "SELECT * FROM scripts;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM scripts;", conn);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Scripts_Entity entity = new Scripts_Entity //Create entity
                            {
                                id = reader["id"].ToString() ?? String.Empty,
                                name = reader["name"].ToString() ?? String.Empty,
                                description = reader["description"].ToString() ?? String.Empty,
                                author = reader["author"].ToString() ?? String.Empty,
                                date = reader["date"].ToString() ?? String.Empty,
                                shell = reader["shell"].ToString() ?? String.Empty,
                                platform = reader["platform"].ToString() ?? String.Empty,
                                script_json = reader["json"].ToString() ?? String.Empty,
                            };

                            scripts_mysql_data.Add(entity);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_scripts -> Scripts_Load", "Result", ex.Message);
            }
            finally
            {
                conn.Close();
                StateHasChanged();
            }
        }

        public List<Scripts_Entity> scripts_mysql_data;

        public class Scripts_Entity
        {
            public string id { get; set; } = String.Empty;
            public string name { get; set; } = String.Empty;
            public string description { get; set; } = String.Empty;
            public string author { get; set; } = String.Empty;
            public string date { get; set; } = String.Empty;
            public string platform { get; set; } = String.Empty;
            public string shell { get; set; } = String.Empty;
            public string script_json { get; set; } = String.Empty;
        }

        private string scripts_table_view_port = "70vh";
        private string scripts_table_sorted_column;
        private string scripts_table_search_string = "";

        private bool Scripts_Table_Filter_Func(Scripts_Entity row)
        {
            if (string.IsNullOrEmpty(scripts_table_search_string))
                return true;

            //Search logic for each column
            return row.name.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.description.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.author.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.date.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.shell.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.platform.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase) ||
                row.script_json.Contains(scripts_table_search_string, StringComparison.OrdinalIgnoreCase);
        }

        private string scripts_selectedRowContent_id = String.Empty; // Hier wird der Inhalt der ausgewählten Zeile gespeichert
        private string scripts_selectedRowContent_json = String.Empty; // Hier wird der Inhalt der ausgewählten Zeile gespeichert

        // Der Handler für den TableRowClick-Event
        private void Scripts_RowClickHandler(Scripts_Entity row)
        {
            scripts_selectedRowContent_id = row.id;
            scripts_selectedRowContent_json = row.script_json;
        }

        private async void Scripts_RowDblClickHandler(Scripts_Entity row)
        {
            scripts_selectedRowContent_id = row.id;

            await Edit_Script_Dialog(row.script_json);
        }

        private string Scripts_GetRowClass(Scripts_Entity row)
        {
            return row.id == scripts_selectedRowContent_id ? (_isDarkMode ? "selected-row-dark" : "selected-row-light") : String.Empty;
        }

        private bool Scripts_Get_Row_Selected()
        {
            if (String.IsNullOrEmpty(scripts_selectedRowContent_id) == false)
                return false;
            else
                return true;
        }

        private bool add_script_dialog_open = false;

        private async Task Add_Script_Dialog()
        {
            if (add_script_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                //FullScreen = true,
                MaxWidth = MaxWidth.ExtraLarge,
                BackgroundClass = "dialog-blurring",
            };

            add_script_dialog_open = true;

            var result = await this.DialogService.Show<Pages.Collections.Scripts.Dialogs.Add_Script_Dialog>(string.Empty, new DialogParameters(), options).Result;

            add_script_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/manage_scripts -> Add_Script_Dialog", "Result", result.Data.ToString());

            if (String.IsNullOrEmpty(result.Data.ToString()) == false && result.Data.ToString() != "error")
            {
                await Scripts_Load();
            }
        }

        private bool edit_script_dialog_open = false;

        private async Task Edit_Script_Dialog(string json)
        {
            if (edit_script_dialog_open)
                return;

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                //FullScreen = true,
                MaxWidth = MaxWidth.ExtraLarge,
                BackgroundClass = "dialog-blurring",
            };

            DialogParameters parameters = new DialogParameters();
            parameters.Add("json", json);

            edit_script_dialog_open = true;

            var result = await this.DialogService.Show<Pages.Collections.Scripts.Dialogs.Edit_Script_Dialog>(string.Empty, parameters, options).Result;

            edit_script_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/manage_scripts -> Edit_Script_Dialog", "Result", result.Data.ToString());

            if (String.IsNullOrEmpty(result.Data.ToString()) == false && result.Data.ToString() != "error")
            {
                scripts_selectedRowContent_id = String.Empty;
                scripts_selectedRowContent_json = String.Empty;

                await Scripts_Load();
            }
        }

        private bool delete_script_dialog_open = false;

        private async Task Delete_Script_Dialog(string id)
        {
            if (delete_script_dialog_open)
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

            delete_script_dialog_open = true;

            var result = await this.DialogService.Show<Pages.Collections.Scripts.Dialogs.Delete_Script_Dialog>(string.Empty, parameters, options).Result;

            delete_script_dialog_open = false;

            if (result.Canceled)
                return;

            Logging.Handler.Debug("/manage_scripts -> Delete_Script_Dialog", "Result", result.Data.ToString());

            if (String.IsNullOrEmpty(result.Data.ToString()) == false && result.Data.ToString() != "error")
            {
                scripts_selectedRowContent_id = String.Empty;
                scripts_selectedRowContent_json = String.Empty;

                await Scripts_Load();
            }
        }

        private bool community_scripts_dialog_open = false;

        private async Task Community_Scripts_Dialog()
        {
            try
            {
                if (community_scripts_dialog_open)
                    return;

                var options = new DialogOptions
                {
                    CloseButton = true,
                    FullWidth = true,
                    MaxWidth = MaxWidth.ExtraLarge,
                    BackgroundClass = "dialog-blurring",
                };

                DialogParameters parameters = new DialogParameters();

                community_scripts_dialog_open = true;

                await this.DialogService.Show<Pages.Collections.Scripts.Community_Scripts.Community_Scripts_Dialog>(string.Empty, parameters, options).Result;

                await Scripts_Load();

                community_scripts_dialog_open = false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_scripts -> Community_Scripts_Dialog", "Community_Scripts_Dialog", ex.ToString());
            }
        }

        #region Data_Export
        private async Task Trigger_Export_Table_Dialog()
        {
            await Export_Table_Dialog("scripts");
        }

        private async Task Export_Table_Dialog(string type)
        {
            var options = new DialogOptions
            {

                MaxWidth = MaxWidth.Small,
                BackgroundClass = "dialog-blurring",

            };

            var result = await this.DialogService.Show<Shared.Export_Data_Dialog>(string.Empty, new DialogParameters(), options).Result;

            if (result != null && result.Data != null)
            {
                if (result.Data.ToString() == "JSON")
                    await Export_Data_Json(type);
                else if (result.Data.ToString() == "Spreadsheet (.xlsx)")
                    await Export_Data_Spreadsheet(type);
                else if (result.Data.ToString() == "HTML")
                    await Export_Data_HTML(type);
            }
        }

        private async Task Export_Data_Json(string type)
        {
            try
            {
                string jsonContent = String.Empty;

                // Erstellen eines JSON-Strings aus den MudTable-Einträgen
                if (type == "scripts")
                    jsonContent = JsonSerializer.Serialize(scripts_mysql_data, new JsonSerializerOptions { WriteIndented = true });

                // Aufruf der JavaScript-Funktion für den Export als .txt
                await JSRuntime.InvokeVoidAsync("exportToTxt", $"{type}.json", jsonContent);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_scripts -> Export_Data_Json", "", ex.Message);
            }
        }

        public async Task Export_Data_HTML(string type)
        {
            try
            {
                StringBuilder htmlBuilder = new StringBuilder();

                if (type == "scripts")
                {
                    // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                    htmlBuilder.Append("<table border='1'><tr>");
                    foreach (var property in scripts_mysql_data.First().GetType().GetProperties())
                    {
                        htmlBuilder.Append($"<th>{property.Name}</th>");
                    }
                    htmlBuilder.Append("</tr>");

                    // Baue die Tabelleneinträge basierend auf den Daten
                    foreach (var entry in scripts_mysql_data)
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
                Logging.Handler.Error("/manage_scripts -> Export_Data_HTML", "", ex.Message);
            }
        }

        private async Task Export_Data_Spreadsheet(string type)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    if (type == "scripts")
                    {
                        if (scripts_mysql_data.Count > 0)
                        {
                            int headerRow = 1;

                            // Baue den Tabellenkopf basierend auf den Eigenschaften der Datenklasse
                            int columnIndex = 1;
                            foreach (var property in scripts_mysql_data.First().GetType().GetProperties())
                            {
                                worksheet.Cells[headerRow, columnIndex].Value = property.Name;
                                columnIndex++;
                            }

                            int dataRow = headerRow + 1;

                            // Baue die Tabelleneinträge basierend auf den Daten
                            foreach (var entry in scripts_mysql_data)
                            {
                                columnIndex = 1;
                                foreach (var property in entry.GetType().GetProperties())
                                {
                                    worksheet.Cells[dataRow, columnIndex].Value = property.GetValue(entry);
                                    columnIndex++;
                                }

                                dataRow++;
                            }
                        }
                    }

                    var stream = new MemoryStream(package.GetAsByteArray());

                    // Hier wird JavaScript-Interop verwendet, um die Datei herunterzuladen
                    await JSRuntime.InvokeVoidAsync("saveAsSpreadSheet", $"{type}.xlsx", Convert.ToBase64String(stream.ToArray()));
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_scripts -> Export_Data_Spreadsheet", "", ex.Message);
            }
        }
        #endregion
    }
}