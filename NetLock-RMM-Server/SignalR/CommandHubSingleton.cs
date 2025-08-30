using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace NetLock_RMM_Server.SignalR
{
    public class CommandHubSingleton
    {
        private static readonly Lazy<CommandHubSingleton> _instance = new Lazy<CommandHubSingleton>(() => new CommandHubSingleton());

        // Public property to access the Singleton instance
        public static CommandHubSingleton Instance => _instance.Value;

        public IHubContext<CommandHub> HubContext { get; private set; }

        // ConcurrentDictionary instances with Timestamp tracking für bessere Ressourcenverwaltung
        public readonly ConcurrentDictionary<string, string> _clientConnections;
        public readonly ConcurrentDictionary<string, string> _adminCommands;
        public readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _responseTasks;
        
        // Zusätzliche Dictionaries für Connection Tracking
        private readonly ConcurrentDictionary<string, DateTime> _connectionTimestamps;
        private readonly ConcurrentDictionary<string, int> _failedAttempts;
        
        // Konstanten für Ressourcenmanagement
        private const int MAX_COMMAND_AGE_MINUTES = 5;
        private const int MAX_INACTIVE_CONNECTION_MINUTES = 10;
        private const int MAX_FAILED_ATTEMPTS = 3;

        // Private constructor to prevent external instantiation
        private CommandHubSingleton()
        {
            _clientConnections = new ConcurrentDictionary<string, string>();
            _adminCommands = new ConcurrentDictionary<string, string>();
            _responseTasks = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
            _connectionTimestamps = new ConcurrentDictionary<string, DateTime>();
            _failedAttempts = new ConcurrentDictionary<string, int>();
            
            // Starte Task für periodische Ressourcenbereinigung
            StartCleanupTask();
        }
        
        private async void StartCleanupTask()
        {
            while (true)
            {
                try
                {
                    // Warte 5 Minuten zwischen den Bereinigungen
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    
                    // Alte Admin-Commands bereinigen
                    foreach (var command in _adminCommands)
                    {
                        if (_connectionTimestamps.TryGetValue(command.Key, out DateTime timestamp))
                        {
                            if ((DateTime.UtcNow - timestamp).TotalMinutes > MAX_COMMAND_AGE_MINUTES)
                            {
                                RemoveAdminCommand(command.Key);
                                Logging.Handler.Debug("CommandHubSingleton", "CleanupTask", $"Removed stale admin command: {command.Key}");
                            }
                        }
                    }
                    
                    // Verwaiste Response-Tasks bereinigen
                    foreach (var task in _responseTasks)
                    {
                        if (_connectionTimestamps.TryGetValue(task.Key, out DateTime timestamp))
                        {
                            if ((DateTime.UtcNow - timestamp).TotalMinutes > MAX_COMMAND_AGE_MINUTES)
                            {
                                if (_responseTasks.TryRemove(task.Key, out var taskSource))
                                {
                                    taskSource.TrySetCanceled();
                                    Logging.Handler.Debug("CommandHubSingleton", "CleanupTask", $"Canceled stale response task: {task.Key}");
                                }
                            }
                        }
                    }
                    
                    int removedConnections = 0;
                    
                    // Alte Verbindungsstatistiken bereinigen
                    foreach (var failedAttempt in _failedAttempts)
                    {
                        if (!_clientConnections.ContainsKey(failedAttempt.Key))
                        {
                            _failedAttempts.TryRemove(failedAttempt.Key, out _);
                        }
                    }
                    
                    // Statistische Daten ausgeben
                    Logging.Handler.Debug("CommandHubSingleton", "ConnectionStats", 
                        $"Active connections: {_clientConnections.Count}, Admin commands: {_adminCommands.Count}, " +
                        $"Response tasks: {_responseTasks.Count}, Removed connections: {removedConnections}");
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("CommandHubSingleton", "CleanupTask", ex.ToString());
                }
            }
        }

        // Example method to interact with dictionaries
        public void AddClientConnection(string clientId, string identity)
        {
            try
            {
                _clientConnections.TryAdd(clientId, identity);
                _connectionTimestamps[clientId] = DateTime.UtcNow;
                _failedAttempts.TryRemove(clientId, out _); // Zurücksetzen des Fehlerzählers bei erfolgreicher Verbindung
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "AddClientConnection", ex.ToString());
            }
        }

        public void RemoveClientConnection(string clientId)
        {
            try
            {
                _clientConnections.TryRemove(clientId, out _);
                _connectionTimestamps.TryRemove(clientId, out _);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "RemoveClientConnection", ex.ToString());
            }
        }

        // Add admin command to dictionary with timestamp tracking
        public void AddAdminCommand(string responseId, string command)
        {
            try
            {
                _adminCommands.TryAdd(responseId, command);
                _connectionTimestamps[responseId] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "AddAdminCommand", ex.ToString());
            }
        }

        // Remove admin command from dictionary
        public bool RemoveAdminCommand(string responseId)
        {
            try
            {
                bool removed = _adminCommands.TryRemove(responseId, out _);
                _connectionTimestamps.TryRemove(responseId, out _);
                return removed;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "RemoveAdminCommand", ex.ToString());
                return false;
            }
        }
        
        // Zähle fehlgeschlagene Verbindungsversuche
        public int IncrementFailedAttempt(string clientId)
        {
            _failedAttempts.AddOrUpdate(clientId, 1, (key, oldValue) => oldValue + 1);
            return _failedAttempts[clientId];
        }
        
        // Prüfe ob Verbindungslimit erreicht ist
        public bool HasExceededFailedAttempts(string clientId)
        {
            return _failedAttempts.TryGetValue(clientId, out int attempts) && attempts >= MAX_FAILED_ATTEMPTS;
        }

        public void Initialize(IHubContext<CommandHub> hubContext)
        {
            HubContext = hubContext;
        }

        // Get admin identity from command
        public string GetAdminIdentity(string commandId)
        {
            try
            {
                if (_adminCommands.TryGetValue(commandId, out string identity))
                {
                    return identity;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "GetAdminIdentity", ex.ToString());
                return string.Empty;
            }
        }
    }
}
