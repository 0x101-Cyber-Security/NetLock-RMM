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

        // ConcurrentDictionary instances
        public readonly ConcurrentDictionary<string, string> _clientConnections;
        public readonly ConcurrentDictionary<string, string> _adminCommands;
        public readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _responseTasks;

        // Private constructor to prevent external instantiation
        private CommandHubSingleton()
        {
            _clientConnections = new ConcurrentDictionary<string, string>();
            _adminCommands = new ConcurrentDictionary<string, string>();
            _responseTasks = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        }

        // Example method to interact with dictionaries
        public void AddClientConnection(string clientId, string identity)
        {
            try
            {
                _clientConnections.TryAdd(clientId, identity);
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
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "RemoveClientConnection", ex.ToString());
            }
        }

        // Add admin command to dictionary
        public void AddAdminCommand(string responseId, string command)
        {
            try
            {
                _adminCommands.TryAdd(responseId, command);
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
                return _adminCommands.TryRemove(responseId, out _);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("CommandHubSingleton", "RemoveAdminCommand", ex.ToString());
                return false;
            }
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
