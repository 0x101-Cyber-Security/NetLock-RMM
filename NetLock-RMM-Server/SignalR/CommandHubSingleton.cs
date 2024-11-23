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
            _clientConnections.TryAdd(clientId, identity);
        }

        public bool RemoveClientConnection(string clientId)
        {
            return _clientConnections.TryRemove(clientId, out _);
        }

        // Add admin command
        public void AddAdminCommand(string responseId, string command)
        {
            _adminCommands.TryAdd(responseId, command);
        }

        // Remove admin command
        public bool RemoveAdminCommand(string responseId)
        {
            return _adminCommands.TryRemove(responseId, out _);
        }

        public void Initialize(IHubContext<CommandHub> hubContext)
        {
            HubContext = hubContext;
        }

        // Get admin identity from command
        public string GetAdminIdentity(string commandId)
        {
            if (_adminCommands.TryGetValue(commandId, out string identity))
            {
                return identity;
            }

            return string.Empty;
        }

    }
}
